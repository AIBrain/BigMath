// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedBitConverterFacts.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Linq;
using BigMath.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace BigMath.Tests
{
    [TestFixture]
    public class ExtendedBitConverterFacts
    {
        private const string Int128Value = "0x000102030405060708090A0B0C0D0E0F";
        private const string Int128ValueLittleEndian = "0x0F0E0D0C0B0A09080706050403020100";

        [Test]
        public void Should_convert_bytes_to_int128()
        {
            Int128 expectedBigEndian = Int128.Parse(Int128Value);
            Int128 expectedLittleEndian = Int128.Parse(Int128ValueLittleEndian);

            byte[] bytes = Int128Value.ToBytes();

            bytes.ToInt128(0, false).Should().Be(expectedBigEndian);
            bytes.ToInt128(0, true).Should().Be(expectedLittleEndian);
        }

        [Test]
        public void Should_convert_int128_to_bytes()
        {
            Int128 i = Int128.Parse(Int128Value);
            byte[] expectedBytes = Int128Value.ToBytes();

            byte[] actualBytes = i.ToBytes(false);
            Assert.AreEqual(expectedBytes, actualBytes);

            actualBytes = i.ToBytes(true);
            Assert.AreEqual(expectedBytes.Reverse(), actualBytes);
        }
    }
}
