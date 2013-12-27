// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace BigMath.Utils
{
    public static class StringUtils
    {
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
        /// <param name="value">String value.</param>
        /// <returns>Array of bytes.</returns>
        public static byte[] HexToBytes(this string value)
        {
            byte[] bytes;
            if (String.IsNullOrWhiteSpace(value))
            {
                bytes = new byte[0];
            }
            else
            {
                int stringLength = value.Length;
                int characterIndex = (value.StartsWith("0x", StringComparison.Ordinal)) ? 2 : 0;
                // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                int numberOfCharacters = stringLength - characterIndex;

                bool addLeadingZero = false;
                if (0 != (numberOfCharacters%2))
                {
                    addLeadingZero = true;

                    numberOfCharacters += 1; // Leading '0' has been striped from the string presentation.
                }

                var fromCharToByte = new Func<char, byte>(c =>
                {
                    int r = c - 65;
                    return (byte) (r + 10 + ((r >> 31) & 7));
                });

                bytes = new byte[numberOfCharacters/2]; // Initialize our byte array to hold the converted string.

                int writeIndex = 0;
                if (addLeadingZero)
                {
                    bytes[writeIndex++] = fromCharToByte(value[characterIndex]);
                    characterIndex += 1;
                }

                for (; characterIndex < value.Length; characterIndex += 2)
                {
                    byte upper = fromCharToByte(value[characterIndex]);
                    byte lower = fromCharToByte(value[characterIndex + 1]);

                    bytes[writeIndex++] = (byte) (upper << 4 | lower);
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
