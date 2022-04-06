﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Sleepey.FF8Mod
{
    public static class FF8String
    {
        // Note: there are two identical double-quote characters & Encode will always choose the first one
        private static readonly char[] readableChars = new char[]
        {
            ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '%', '/', ':', '!', '?',
            '…', '+', '-', '=', '*', '&', '"', '"', '(', ')', '·', '.', ',', '~', '”', '“',
            '\'', '#', '$', '`', '_', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K',
            'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a',
            'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q',
            'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        };

        public static string Decode(IEnumerable<byte> bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            var result = string.Empty;

            using (var stream = new MemoryStream(bytes.ToArray()))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var code = (int)reader.ReadByte();

                    // null-terminated
                    if (code == 0) break;

                    /*
                    // special characters & commands
                    switch (code)
                    {
                        case 0x02:
                            result += @"\n";
                            continue;
                        case 0x03:
                            result += "{char " + reader.ReadByte().ToString("x2") + "}";
                            continue;
                        case 0x05:
                            result += "{icon " + reader.ReadByte().ToString("x2") + "}";
                            continue;
                        case 0x06:
                            result += "{col " + reader.ReadByte().ToString("x2") + "}";
                            continue;
                        case 0x0a:
                            result += "{spec " + reader.ReadByte().ToString("x2") + "}";
                            continue;
                        case 0x0b:
                            result += "{cursor " + reader.ReadByte().ToString("x2") + "}";
                            continue;
                        case 0x0c:
                            result += "{spell " + reader.ReadByte().ToString("x2") + "}";
                            continue;
                    }
                    */

                    var index = code - 0x20;
                    if (index < 0 || index >= readableChars.Length)
                    {
                        // unreadable characters are output as hex codes like "{0a}"
                        result += "{" + code.ToString("x2") + "}";
                    }
                    else
                    {
                        result += readableChars[index];
                    }
                }
            }
            return result;
        }

        public static IEnumerable<byte> Encode(string str)
        {
            if (str == null) throw new ArgumentNullException("str");

            var result = new List<byte>();

            var leftBraces = str.Where(c => c == '{').Count();
            var rightBraces = str.Where(c => c == '}').Count();
            if (leftBraces != rightBraces) throw new InvalidDataException("Unopened or unclosed character code braces in string: " + str);

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                // stream through the string, char by char
                writer.Write(str);
                writer.Flush();
                stream.Position = 0;
                while (stream.Position < stream.Length)
                {
                    var nextChar = reader.ReadChar();

                    // handle special character codes
                    if (nextChar == '{')
                    {
                        var codeStr = "";
                        var nextInCode = reader.ReadChar();

                        while (nextInCode != '}')
                        {
                            codeStr += nextInCode;
                            nextInCode = reader.ReadChar();
                        }

                        if (codeStr.Length > 2 || !int.TryParse(codeStr, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int code))
                        {
                            throw new InvalidDataException("Invalid character code '{" + codeStr + "}' in string: " + str);
                        }

                        result.Add((byte)code);
                        continue;
                    }

                    var index = Array.IndexOf(readableChars, nextChar);
                    if (index == -1) throw new InvalidDataException("Unrecognised character '" + nextChar + "' in string: " + str);
                    result.Add((byte)(index + 0x20));
                }

                // null-terminated
                result.Add(0);
                return result;
            }
        }
    }
}
