// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MathUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace BigMath.Utils
{
    /// <summary>
    ///     Math utils.
    /// </summary>
    public static class MathUtils
    {
        private const ulong HiBit = 0x100000000;

        /// <summary>
        ///     Divide with reminder.
        /// </summary>
        /// <param name="dividend">Dividend.</param>
        /// <param name="divisor">Divisor.</param>
        /// <param name="quotient">Quotient.</param>
        /// <param name="remainder">Reminder.</param>
        public static void DivRem(uint[] dividend, uint[] divisor, out uint[] quotient, out uint[] remainder)
        {
            int divisorLen = GetLength(divisor);
            int dividendLen = GetLength(dividend);
            if (divisorLen <= 1)
            {
                ulong rem = 0;
                uint div = divisor[0];
                quotient = new uint[dividendLen];
                remainder = new uint[1];
                for (int i = dividendLen - 1; i >= 0; i--)
                {
                    rem *= HiBit;
                    rem += dividend[i];
                    ulong q = rem/div;
                    rem -= q*div;
                    quotient[i] = (uint) q;
                }
                remainder[0] = (uint) rem;
                return;
            }

            if (dividendLen >= divisorLen)
            {
                int shift = GetNormalizeShift(divisor[divisorLen - 1]);
                var normDividend = new uint[dividendLen + 1];
                var normDivisor = new uint[divisorLen];
                Normalize(dividend, dividendLen, normDividend, shift);
                Normalize(divisor, divisorLen, normDivisor, shift);
                quotient = new uint[(dividendLen - divisorLen) + 1];
                for (int j = dividendLen - divisorLen; j >= 0; j--)
                {
                    ulong dx = (HiBit*normDividend[j + divisorLen]) + normDividend[(j + divisorLen) - 1];
                    ulong qj = dx/normDivisor[divisorLen - 1];
                    dx -= qj*normDivisor[divisorLen - 1];
                    do
                    {
                        if ((qj < HiBit) && ((qj*normDivisor[divisorLen - 2]) <= ((dx*HiBit) + normDividend[(j + divisorLen) - 2])))
                        {
                            break;
                        }

                        qj -= 1L;
                        dx += normDivisor[divisorLen - 1];
                    } while (dx < HiBit);

                    long di = 0;
                    long dj;
                    int index = 0;
                    while (index < divisorLen)
                    {
                        ulong dqj = normDivisor[index]*qj;
                        dj = (normDividend[index + j] - ((uint) dqj)) - di;
                        normDividend[index + j] = (uint) dj;
                        dqj = dqj >> 32;
                        dj = dj >> 32;
                        di = ((long) dqj) - dj;
                        index++;
                    }

                    dj = normDividend[j + divisorLen] - di;
                    normDividend[j + divisorLen] = (uint) dj;
                    quotient[j] = (uint) qj;

                    if (dj < 0)
                    {
                        quotient[j]--;
                        ulong sum = 0;
                        for (index = 0; index < divisorLen; index++)
                        {
                            sum = (normDivisor[index] + normDividend[j + index]) + sum;
                            normDividend[j + index] = (uint) sum;
                            sum = sum >> 32;
                        }
                        sum += normDividend[j + divisorLen];
                        normDividend[j + divisorLen] = (uint) sum;
                    }
                }
                remainder = Unnormalize(normDividend, shift);
                return;
            }

            quotient = new uint[0];
            remainder = dividend;
        }

        /// <summary>
        ///     Bitwise shift array of <see cref="ulong" />.
        /// </summary>
        /// <param name="values">Bits to shift. Lower bits have lower index in array.</param>
        /// <param name="shift">Shift amount in bits. Negative for left shift, positive for right shift.</param>
        /// <returns>Shifted values.</returns>
        public static ulong[] Shift(ulong[] values, int shift)
        {
            if (shift == 0)
            {
                return values;
            }
            return shift < 0 ? ShiftLeft(values, -shift) : ShiftRight(values, shift);
        }

        /// <summary>
        ///     Bitwise right shift.
        /// </summary>
        /// <param name="values">Bits to shift. Lower bits have lower index in array.</param>
        /// <param name="shift">Shift amount in bits.</param>
        /// <returns>Shifted values.</returns>
        public static ulong[] ShiftRight(ulong[] values, int shift)
        {
            if (shift < 0)
            {
                return ShiftLeft(values, -shift);
            }

            const int valueLength = sizeof (ulong)*8;
            int length = values.Length;

            shift = shift%(length*valueLength);

            int shiftOffset = shift/valueLength;
            int bshift = shift%valueLength;

            var shifted = new ulong[length];
            for (int i = 0; i < length; i++)
            {
                int ishift = i - shiftOffset;
                if (ishift < 0)
                {
                    continue;
                }
                shifted[ishift] |= values[i] >> bshift;
                if (bshift > 0 && i + 1 < length)
                {
                    shifted[ishift] |= values[i + 1] << valueLength - bshift;
                }
            }

            return shifted;
        }

        /// <summary>
        ///     Bitwise right shift.
        /// </summary>
        /// <param name="values">Bits to shift. Lower bits have lower index in array.</param>
        /// <param name="shift">Shift amount in bits.</param>
        /// <returns>Shifted values.</returns>
        public static ulong[] ShiftLeft(ulong[] values, int shift)
        {
            if (shift < 0)
            {
                return ShiftRight(values, -shift);
            }

            const int valueLength = sizeof (ulong)*8;
            int length = values.Length;

            shift = shift%(length*valueLength);

            int shiftOffset = shift/valueLength;
            int bshift = shift%valueLength;

            var shifted = new ulong[length];
            for (int i = 0; i < length; i++)
            {
                int ishift = i + shiftOffset;
                if (ishift >= length)
                {
                    continue;
                }
                shifted[ishift] |= values[i] << bshift;
                if (bshift > 0 && i - 1 >= 0)
                {
                    shifted[ishift] |= values[i - 1] >> valueLength - bshift;
                }
            }

            return shifted;
        }

        private static int GetLength(uint[] uints)
        {
            int index = uints.Length - 1;
            while ((index >= 0) && (uints[index] == 0))
            {
                index--;
            }
            index = index < 0 ? 0 : index;
            return index + 1;
        }

        private static int GetNormalizeShift(uint ui)
        {
            int shift = 0;
            if ((ui & 0xffff0000) == 0)
            {
                ui = ui << 16;
                shift += 16;
            }

            if ((ui & 0xff000000) == 0)
            {
                ui = ui << 8;
                shift += 8;
            }

            if ((ui & 0xf0000000) == 0)
            {
                ui = ui << 4;
                shift += 4;
            }

            if ((ui & 0xc0000000) == 0)
            {
                ui = ui << 2;
                shift += 2;
            }

            if ((ui & 0x80000000) == 0)
            {
                shift++;
            }
            return shift;
        }

        private static uint[] Unnormalize(uint[] normalized, int shift)
        {
            int len = GetLength(normalized);
            var unormalized = new uint[len];
            if (shift > 0)
            {
                int rshift = 32 - shift;
                uint r = 0;
                for (int i = len - 1; i >= 0; i--)
                {
                    unormalized[i] = (normalized[i] >> shift) | r;
                    r = normalized[i] << rshift;
                }
            }
            else
            {
                for (int j = 0; j < len; j++)
                {
                    unormalized[j] = normalized[j];
                }
            }
            return unormalized;
        }

        private static void Normalize(uint[] unormalized, int len, uint[] normalized, int shift)
        {
            int i;
            uint n = 0;
            if (shift > 0)
            {
                int rshift = 32 - shift;
                for (i = 0; i < len; i++)
                {
                    normalized[i] = (unormalized[i] << shift) | n;
                    n = unormalized[i] >> rshift;
                }
            }
            else
            {
                i = 0;
                while (i < len)
                {
                    normalized[i] = unormalized[i];
                    i++;
                }
            }

            while (i < normalized.Length)
            {
                normalized[i++] = 0;
            }

            if (n != 0)
            {
                normalized[len] = n;
            }
        }
    }
}
