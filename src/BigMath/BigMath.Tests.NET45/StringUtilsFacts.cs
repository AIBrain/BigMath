// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtilsFacts.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using BigMath.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace BigMath.Tests
{
    [TestFixture]
    public class StringUtilsFacts
    {
        [TestCase("String", "gnirtS")]
        [TestCase("Str ing", "gni rtS")]
        [TestCase(" String", "gnirtS ")]
        [TestCase("String ", " gnirtS")]
        public void Should_reverse_string(string str, string expectedStr)
        {
            str.Reverse().Should().Be(expectedStr);
        }
    }
}
