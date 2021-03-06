﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Text;

    static class StringUtil
    {
        public const char DoubleQuote = '\"';
        public const char Comma = ',';
        public const char LineFeed = '\n';
        public const char CarriageReturn = '\r';
        public const char Tab = '\t';
        public const byte UpperCaseToLowerCaseAsciiOffset = 'a' - 'A';

        static readonly string[] Byte2HexPad = new string[256];
        static readonly string[] Byte2HexNopad = new string[256];

        public static readonly string Newline;

        const int CsvNumberEscapeCharacters = 2 + 5;

        static StringUtil()
        {
            Newline = Environment.NewLine;

            // Generate the lookup table that converts a byte into a 2-digit hexadecimal integer.
            int i;
            for (i = 0; i < 10; i++)
            {
                var buf = new StringBuilder(2);
                buf.Append('0');
                buf.Append(i);
                Byte2HexPad[i] = buf.ToString();
                Byte2HexNopad[i] = (i).ToString();
            }
            for (; i < 16; i++)
            {
                var buf = new StringBuilder(2);
                char c = (char)('A' + i - 10);
                buf.Append('0');
                buf.Append(c);
                Byte2HexPad[i] = buf.ToString();
                Byte2HexNopad[i] = c.ToString(); /* String.valueOf(c);*/
            }
            for (; i < Byte2HexPad.Length; i++)
            {
                var buf = new StringBuilder(2);
                buf.Append(i.ToString("X") /*Integer.toHexString(i)*/);
                string str = buf.ToString();
                Byte2HexPad[i] = str;
                Byte2HexNopad[i] = str;
            }
        }

        public static string[] Split(string value, char delim, int maxParts)
        {
            int end = value.Length;
            var res = new List<string>();

            int start = 0;
            int cpt = 1;
            for (int i = 0; i < end && cpt < maxParts; i++)
            {
                if (value[i] == delim)
                {
                    if (start == i)
                    {
                        res.Add(string.Empty);
                    }
                    else
                    {
                        res.Add(value.Substring(start, i));
                    }
                    start = i + 1;
                    cpt++;
                }
            }

            if (start == 0)
            {
                // If no delimiter was found in the value
                res.Add(value);
            }
            else
            {
                if (start != end)
                {
                    // Add the last element if it's not empty.
                    res.Add(value.Substring(start, end));
                }
                else
                {
                    // Truncate trailing empty elements.
                    for (int i = res.Count - 1; i >= 0; i--)
                    {
                        if (res[i] == "")
                        {
                            res.Remove(res[i]);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return res.ToArray();
        }

        public static string SubstringAfter(this string value, char delim)
        {
            int pos = value.IndexOf(delim);
            return pos >= 0 ? value.Substring(pos + 1) : null;
        }

        public static string ByteToHexStringPadded(int value) => Byte2HexPad[value & 0xff];

        public static string ToHexStringPadded(byte[] src) => ToHexStringPadded(src, 0, src.Length);

        public static string ToHexStringPadded(byte[] src, int offset, int length)
        {
            int end = offset + length;
            var sb = new StringBuilder(length << 1);
            for (int i = offset; i < end; i++)
            {
                sb.Append(ByteToHexStringPadded(src[i]));
            }
            return sb.ToString();
        }

        public static StringBuilder ToHexStringPadded(StringBuilder sb, byte[] src, int offset, int length)
        {
            Contract.Requires((offset + length) <= src.Length);

            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                sb.Append(ByteToHexStringPadded(src[i]));
            }
            return sb;
        }

        public static string ByteToHexString(byte value) => Byte2HexNopad[value & 0xff];

        public static StringBuilder ByteToHexString(StringBuilder buf, byte value) => buf.Append(ByteToHexString(value));

        public static string ToHexString(byte[] src) => ToHexString(src, 0, src.Length);

        public static string ToHexString(byte[] src, int offset, int length) => ToHexString(new StringBuilder(length << 1), src, offset, length).ToString();

        public static StringBuilder ToHexString(StringBuilder dst, byte[] src) => ToHexString(dst, src, 0, src.Length);

        public static StringBuilder ToHexString(StringBuilder dst, byte[] src, int offset, int length)
        {
            Debug.Assert(length >= 0);
            if (length == 0)
            {
                return dst;
            }
            int end = offset + length;
            int endMinusOne = end - 1;
            int i;
            // Skip preceding zeroes.
            for (i = offset; i < endMinusOne; i++)
            {
                if (src[i] != 0)
                {
                    break;
                }
            }

            ByteToHexString(dst, src[i++]);
            int remaining = end - i;
            ToHexStringPadded(dst, src, i, remaining);

            return dst;
        }

        public static string EscapeCsv(string value)
        {
            int length = value.Length;
            if (length == 0)
            {
                return value;
            }
            int last = length - 1;
            bool quoted = IsDoubleQuote(value[0]) && IsDoubleQuote(value[last]) && length != 1;
            bool foundSpecialCharacter = false;
            bool escapedDoubleQuote = false;
            StringBuilder escaped = new StringBuilder(length + CsvNumberEscapeCharacters).Append(DoubleQuote);
            for (int i = 0; i < length; i++)
            {
                char current = value[i];
                switch (current)
                {
                    case DoubleQuote:
                        if (i == 0 || i == last)
                        {
                            if (!quoted)
                            {
                                escaped.Append(DoubleQuote);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            bool isNextCharDoubleQuote = IsDoubleQuote(value[i + 1]);
                            if (!IsDoubleQuote(value[i - 1]) &&
                                (!isNextCharDoubleQuote || i + 1 == last))
                            {
                                escaped.Append(DoubleQuote);
                                escapedDoubleQuote = true;
                            }
                        }
                        break;
                    case LineFeed:
                    case CarriageReturn:
                    case Comma:
                        foundSpecialCharacter = true;
                        break;
                }
                escaped.Append(current);
            }
            return escapedDoubleQuote || foundSpecialCharacter && !quoted ?
                escaped.Append(DoubleQuote).ToString() : value;
        }

        static bool IsDoubleQuote(char c) => c == DoubleQuote;

        public static string SimpleClassName(object o) => o?.GetType().Name ?? "null_object";

        public static string SimpleClassName<T>() => typeof(T).Name;

        public static string SimpleClassName(Type type) => type.Name;
    }
}
