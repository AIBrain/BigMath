// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace BigMath.Utils
{
    public static class StringUtils
    {
        private static readonly byte[] CharToByteLookupTable =
        {
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e,
            0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff
        };

        public static string ToHexaString(this ulong[] values, bool caps, int min)
        {
            var sb = new StringBuilder();
            string x = (caps ? "X" : "x") + 16;

            for (int i = values.Length - 1; i >= 0; i--)
            {
                sb.Append(values[i].ToString(x));
            }

            string value = sb.ToString().TrimStart('0');

            int dif = min - value.Length;
            if (dif > 0)
            {
                value = new string('0', min - value.Length) + value;
            }

            return value;
        }

        /// <summary>
        ///     Converts array of bytes to hexadecimal string.
        /// </summary>
        /// <param name="values">Values.</param>
        /// <param name="caps">Capitalize chars.</param>
        /// <param name="min">Minimum string length. Null if there is no minimum length.</param>
        /// <param name="spaceEveryByte">Space every byte.</param>
        /// <returns>Hexadecimal string representation of the bytes array.</returns>
        public static string ToHexaString(this byte[] values, bool caps = true, int? min = null, bool spaceEveryByte = false)
        {
            var sb = new StringBuilder();
            string x = (caps ? "X" : "x") + 2;

            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString(x));
                if (spaceEveryByte)
                {
                    sb.Append(" ");
                }
            }

            string value = sb.ToString();

            if (!min.HasValue)
            {
                return value;
            }

            value = value.TrimStart('0');
            int dif = min.Value - value.Length;
            if (dif > 0)
            {
                value = new string('0', min.Value - value.Length) + value;
            }
            return value;
        }

        /// <summary>
        ///     Converts string of hex numbers to array of bytes.
        /// </summary>
        /// <param name="hexString">String value.</param>
        /// <returns>Array of bytes.</returns>
        public static byte[] HexToBytes(this string hexString)
        {
            byte[] bytes;
            if (String.IsNullOrWhiteSpace(hexString))
            {
                bytes = new byte[0];
            }
            else
            {
                int stringLength = hexString.Length;
                int characterIndex = (hexString.StartsWith("0x", StringComparison.Ordinal)) ? 2 : 0;
                // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                int numberOfCharacters = stringLength - characterIndex;

                bool addLeadingZero = false;
                if (0 != (numberOfCharacters%2))
                {
                    addLeadingZero = true;

                    numberOfCharacters += 1; // Leading '0' has been striped from the string presentation.
                }

                bytes = new byte[numberOfCharacters/2]; // Initialize our byte array to hold the converted string.

                int writeIndex = 0;
                if (addLeadingZero)
                {
                    bytes[writeIndex++] = CharToByteLookupTable[hexString[characterIndex]];
                    characterIndex += 1;
                }

                while (characterIndex < hexString.Length)
                {
                    int hi = CharToByteLookupTable[hexString[characterIndex++]];
                    int lo = CharToByteLookupTable[hexString[characterIndex++]];

                    bytes[writeIndex++] = (byte) (hi << 4 | lo);
                }
            }

            return bytes;
        }

        public static string Reverse(this string input)
        {
            char[] chars = input.ToCharArray();
            Array.Reverse(chars);
            return new String(chars);
        }
    }
}
