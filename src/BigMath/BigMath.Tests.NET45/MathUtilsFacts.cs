// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MathUtilsFacts.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using BigMath.Utils;
using NUnit.Framework;

namespace BigMath.Tests
{
    [TestFixture]
    public class MathUtilsFacts
    {
        [TestCase(new[] {0U}, new[] {1U}, new[] {0U}, new[] {0U}, TestName = "0/1 = 0(0)")]
        [TestCase(new[] {1U}, new[] {1U}, new[] {1U}, new[] {0U}, TestName = "1/1 = 1(0)")]
        [TestCase(new[] {10U}, new[] {2U}, new[] {5U}, new[] {0U}, TestName = "10/2 = 5(0)")]
        [TestCase(new[] {0U, 0x10U}, new[] {0x2U}, new[] {0U, 0x8U}, new[] {0U}, TestName = "0x10U<<32/2 = 0x8U<<32(0)")]
        public void Should_divide_with_reminder(uint[] dividend, uint[] divisor, uint[] expectedQuotient, uint[] expectedRemainder)
        {
            uint[] actualQuotient;
            uint[] actualRemainder;
            MathUtils.DivRem(dividend, divisor, out actualQuotient, out actualRemainder);
            Assert.AreEqual(actualQuotient, expectedQuotient, "Quotient is not as expected.");
            Assert.AreEqual(actualRemainder, expectedRemainder, "Reminder is not as expected.");
        }

        [Test, TestCaseSource(typeof (MathUtilsTestCases), "ShiftTestCases")]
        public ulong[] Should_shift(ulong[] bits, int shift)
        {
            ulong[] shiftedBits = MathUtils.Shift(bits, shift);
            return shiftedBits;
        }
    }

    public class MathUtilsTestCases
    {
        public static IEnumerable ShiftTestCases
        {
            get
            {
                yield return new TestCaseData(new[] {0x1UL, 0x0UL}, -127).Returns(new[] {0x0UL, 0x1UL << 63}).SetName("1 << 127");
                yield return new TestCaseData(new[] {0x1UL, 0x0UL}, 1).Returns(new[] {0UL, 0UL}).SetName("1 >> 1");
                yield return new TestCaseData(new[] {0x0UL, 0x1UL << 63}, 127).Returns(new[] {1UL, 0UL}).SetName("(1<<127) >> 127");
                yield return new TestCaseData(new[] {0x0UL, 0x1UL << 63}, -1).Returns(new[] {0UL, 0UL}).SetName("(1<<127) << 1");
                yield return
                    new TestCaseData(new[] {0x0UL, 0x0102030405060708UL}, 32).Returns(new[] {0x0506070800000000UL, 0x01020304UL}).SetName("(0x0102030405060708UL<<64) >> 32");
                yield return
                    new TestCaseData(new[] {0xA9AAABACADAEAFA0UL, 0xA1A2A3A4A5A6A7A8UL}, 32).Returns(new[] {0xA5A6A7A8A9AAABACUL, 0xA1A2A3A4UL})
                        .SetName("(0xA1A2A3A4A5A6A7A8A9AAABACADAEAFA0UL) >> 32");
                yield return
                    new TestCaseData(new[] {0x0UL, 0xA9AAABACADAEAFA0UL, 0xA1A2A3A4A5A6A7A8UL}, 32).Returns(new[] {0xADAEAFA000000000UL, 0xA5A6A7A8A9AAABACUL, 0xA1A2A3A4UL})
                        .SetName("(0xA1A2A3A4A5A6A7A8A9AAABACADAEAFA0UL<<64) >> 32");
                yield return
                    new TestCaseData(new[] {0xA9AAABACADAEAFA0UL, 0xA1A2A3A4A5A6A7A8UL, 0x0UL}, -32).Returns(new[] {0xADAEAFA000000000UL, 0xA5A6A7A8A9AAABACUL, 0xA1A2A3A4UL})
                        .SetName("(0xA1A2A3A4A5A6A7A8A9AAABACADAEAFA0UL) << 32");
            }
        }
    }
}
