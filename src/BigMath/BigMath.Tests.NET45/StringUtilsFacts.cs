// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtilsFacts.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigMath.Utils;
using NUnit.Framework;

namespace BigMath.Tests
{
    [TestFixture]
    public class StringUtilsFacts
    {
        [TestCaseSource("HexStringToByteArrayTestCases")]
        public byte[] Should_convert_hex_string_to_array_of_bytes(string str)
        {
            return str.HexToBytes();
        }

        private static IEnumerable HexStringToByteArrayTestCases
        {
            get { return GetHexStringToByteArrayTestCasesData.Select(hexBytese => new TestCaseData(hexBytese.Hex).Returns(hexBytese.Bytes)); }
        }

        private static IEnumerable<HexBytes> GetHexStringToByteArrayTestCasesData
        {
            get
            {
                yield return new HexBytes(string.Empty, new byte[0]);
                yield return new HexBytes("0x0", new byte[] {0});
                yield return new HexBytes("0x00", new byte[] {0});
                yield return new HexBytes("0x0000", new byte[] {0, 0});
                yield return new HexBytes("0x00000", new byte[] {0, 0, 0});
                yield return new HexBytes("0x000000", new byte[] {0, 0, 0});
                yield return new HexBytes("0x010203040506070809", Enumerable.Range(1, 9).Select(i => (byte) i).ToArray());
                yield return new HexBytes("0x0102030405060708090A0B0C0D0E0F", Enumerable.Range(1, 15).Select(i => (byte) i).ToArray());
                yield return new HexBytes("0x0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", Enumerable.Range(1, 31).Select(i => (byte) i).ToArray());
                yield return new HexBytes("0x102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", Enumerable.Range(1, 31).Select(i => (byte) i).ToArray());
                yield return new HexBytes("0xc6aeda78b0", new byte[] {0xC6, 0xAE, 0xDA, 0x78, 0xB0});
                yield return new HexBytes("c6aeda78b0", new byte[] {0xC6, 0xAE, 0xDA, 0x78, 0xB0});
                yield return new HexBytes("0xc6aeda78b", new byte[] {0x0c, 0x6A, 0xED, 0xA7, 0x8B});
                yield return new HexBytes("c6aeda78b", new byte[] {0x0c, 0x6A, 0xED, 0xA7, 0x8B});
                yield return new HexBytes("0xC6AEDA78B", new byte[] {0x0c, 0x6A, 0xED, 0xA7, 0x8B});
                yield return new HexBytes("0xC6aEDA78b", new byte[] {0x0c, 0x6A, 0xED, 0xA7, 0x8B});
                yield return new HexBytes("C6AEDA78B", new byte[] {0x0c, 0x6A, 0xED, 0xA7, 0x8B});
                yield return new HexBytes("c6AEda78B", new byte[] {0x0c, 0x6A, 0xED, 0xA7, 0x8B});
                yield return new HexBytes("0xda", new byte[] {0xDA});
                yield return new HexBytes("0xd0", new byte[] {0xD0});
                yield return new HexBytes("0xDA", new byte[] {0xDA});
                yield return new HexBytes("0x0a", new byte[] {0x0A});
            }
        }

        private class HexBytes
        {
            public HexBytes(string hex, byte[] bytes)
            {
                Hex = hex;
                Bytes = bytes;
            }

            public string Hex { get; set; }
            public byte[] Bytes { get; set; }
        }
    }
}
