// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BigInteger.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

/*
Optimization
	Have proper popcount function for IsPowerOfTwo
	Use unsafe ops to avoid bounds check
	CoreAdd could avoid some resizes by checking for equal sized array that top overflow
	For bitwise operators, hoist the conditionals out of their main loop
	Optimize BitScanBackward
	Use a carry variable to make shift opts do half the number of array ops.
	Schoolbook multiply is O(n^2), use Karatsuba /Toom-3 for large numbers
*/

namespace BigMath
{
    public struct BigInteger : IComparable, IFormattable, IComparable<BigInteger>, IEquatable<BigInteger>
    {
        //LSB on [0]
        private readonly uint[] _data;
        private readonly short _sign;

        private static readonly uint[] ZERO = new uint[1];
        private static readonly uint[] ONE = new uint[1] {1};

        private BigInteger(short sign, uint[] data)
        {
            _sign = sign;
            _data = data;
        }

        public BigInteger(int value)
        {
            if (value == 0)
            {
                _sign = 0;
                _data = ZERO;
            }
            else if (value > 0)
            {
                _sign = 1;
                _data = new[] {(uint) value};
            }
            else
            {
                _sign = -1;
                _data = new uint[1] {(uint) -value};
            }
        }

        [CLSCompliant(false)]
        public BigInteger(uint value)
        {
            if (value == 0)
            {
                _sign = 0;
                _data = ZERO;
            }
            else
            {
                _sign = 1;
                _data = new uint[1] {value};
            }
        }

        public BigInteger(long value)
        {
            if (value == 0)
            {
                _sign = 0;
                _data = ZERO;
            }
            else if (value > 0)
            {
                _sign = 1;
                var low = (uint) value;
                var high = (uint) (value >> 32);

                _data = new uint[high != 0 ? 2 : 1];
                _data[0] = low;
                if (high != 0)
                {
                    _data[1] = high;
                }
            }
            else
            {
                _sign = -1;
                value = -value;
                var low = (uint) value;
                var high = (uint) ((ulong) value >> 32);

                _data = new uint[high != 0 ? 2 : 1];
                _data[0] = low;
                if (high != 0)
                {
                    _data[1] = high;
                }
            }
        }

        [CLSCompliant(false)]
        public BigInteger(ulong value)
        {
            if (value == 0)
            {
                _sign = 0;
                _data = ZERO;
            }
            else
            {
                _sign = 1;
                var low = (uint) value;
                var high = (uint) (value >> 32);

                _data = new uint[high != 0 ? 2 : 1];
                _data[0] = low;
                if (high != 0)
                {
                    _data[1] = high;
                }
            }
        }


        private static bool Negative(byte[] v)
        {
            return ((v[7] & 0x80) != 0);
        }

        private static ushort Exponent(byte[] v)
        {
            return (ushort) ((((ushort) (v[7] & 0x7F)) << 4) | (((ushort) (v[6] & 0xF0)) >> 4));
        }

        private static ulong Mantissa(byte[] v)
        {
            uint i1 = (v[0] | ((uint) v[1] << 8) | ((uint) v[2] << 16) | ((uint) v[3] << 24));
            uint i2 = (v[4] | ((uint) v[5] << 8) | ((uint) (v[6] & 0xF) << 16));

            return i1 | ((ulong) i2 << 32);
        }

        private const int bias = 1075;

        public BigInteger(double value)
        {
            if (double.IsNaN(value) || Double.IsInfinity(value))
            {
                throw new OverflowException();
            }

            byte[] bytes = BitConverter.GetBytes(value);
            ulong mantissa = Mantissa(bytes);
            if (mantissa == 0)
            {
                // 1.0 * 2**exp, we have a power of 2
                int exponent = Exponent(bytes);
                if (exponent == 0)
                {
                    _sign = 0;
                    _data = ZERO;
                    return;
                }

                BigInteger res = Negative(bytes) ? MinusOne : One;
                res = res << (exponent - 0x3ff);
                _sign = res._sign;
                _data = res._data;
            }
            else
            {
                // 1.mantissa * 2**exp
                int exponent = Exponent(bytes);
                mantissa |= 0x10000000000000ul;
                BigInteger res = mantissa;
                res = exponent > bias ? res << (exponent - bias) : res >> (bias - exponent);

                _sign = (short) (Negative(bytes) ? -1 : 1);
                _data = res._data;
            }
        }

        public BigInteger(float value) : this((double) value)
        {
        }

        private const Int32 DecimalScaleFactorMask = 0x00FF0000;
        private const Int32 DecimalSignMask = unchecked((Int32) 0x80000000);

        public BigInteger(decimal value)
        {
            // First truncate to get scale to 0 and extract bits
            int[] bits = Decimal.GetBits(Decimal.Truncate(value));

            int size = 3;
            while (size > 0 && bits[size - 1] == 0)
            {
                size--;
            }

            if (size == 0)
            {
                _sign = 0;
                _data = ZERO;
                return;
            }

            _sign = (short) ((bits[3] & DecimalSignMask) != 0 ? -1 : 1);

            _data = new uint[size];
            _data[0] = (uint) bits[0];
            if (size > 1)
            {
                _data[1] = (uint) bits[1];
            }
            if (size > 2)
            {
                _data[2] = (uint) bits[2];
            }
        }

        [CLSCompliant(false)]
        public BigInteger(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            int len = value.Length;

            if (len == 0 || (len == 1 && value[0] == 0))
            {
                _sign = 0;
                _data = ZERO;
                return;
            }

            if ((value[len - 1] & 0x80) != 0)
            {
                _sign = -1;
            }
            else
            {
                _sign = 1;
            }

            if (_sign == 1)
            {
                while (value[len - 1] == 0)
                {
                    if (--len == 0)
                    {
                        _sign = 0;
                        _data = ZERO;
                        return;
                    }
                }

                int full_words, size;
                full_words = size = len/4;
                if ((len & 0x3) != 0)
                {
                    ++size;
                }

                _data = new uint[size];
                int j = 0;
                for (int i = 0; i < full_words; ++i)
                {
                    _data[i] = value[j++] | (uint) (value[j++] << 8) | (uint) (value[j++] << 16) | (uint) (value[j++] << 24);
                }
                size = len & 0x3;
                if (size > 0)
                {
                    int idx = _data.Length - 1;
                    for (int i = 0; i < size; ++i)
                    {
                        _data[idx] |= (uint) (value[j++] << (i*8));
                    }
                }
            }
            else
            {
                int full_words, size;
                full_words = size = len/4;
                if ((len & 0x3) != 0)
                {
                    ++size;
                }

                _data = new uint[size];

                uint word, borrow = 1;
                ulong sub = 0;
                int j = 0;

                for (int i = 0; i < full_words; ++i)
                {
                    word = value[j++] | (uint) (value[j++] << 8) | (uint) (value[j++] << 16) | (uint) (value[j++] << 24);

                    sub = (ulong) word - borrow;
                    word = (uint) sub;
                    borrow = (uint) (sub >> 32) & 0x1u;
                    _data[i] = ~word;
                }
                size = len & 0x3;

                if (size > 0)
                {
                    word = 0;
                    uint store_mask = 0;
                    for (int i = 0; i < size; ++i)
                    {
                        word |= (uint) (value[j++] << (i*8));
                        store_mask = (store_mask << 8) | 0xFF;
                    }

                    sub = word - borrow;
                    word = (uint) sub;
                    borrow = (uint) (sub >> 32) & 0x1u;

                    _data[_data.Length - 1] = ~word & store_mask;
                }
                if (borrow != 0) //FIXME I believe this can't happen, can someone write a test for it?
                {
                    throw new Exception("non zero final carry");
                }
            }
        }

        public bool IsEven
        {
            get { return _sign == 0 || (_data[0] & 0x1) == 0; }
        }

        public bool IsOne
        {
            get { return _sign == 1 && _data.Length == 1 && _data[0] == 1; }
        }


        //Gem from Hacker's Delight
        //Returns the number of bits set in @x
        private static int PopulationCount(uint x)
        {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            x = (x + (x >> 4)) & 0x0F0F0F0F;
            x = x + (x >> 8);
            x = x + (x >> 16);
            return (int) (x & 0x0000003F);
        }

        public bool IsPowerOfTwo
        {
            get
            {
                bool foundBit = false;
                if (_sign != 1)
                {
                    return false;
                }
                //This function is pop count == 1 for positive numbers
                for (int i = 0; i < _data.Length; ++i)
                {
                    int p = PopulationCount(_data[i]);
                    if (p > 0)
                    {
                        if (p > 1 || foundBit)
                        {
                            return false;
                        }
                        foundBit = true;
                    }
                }
                return foundBit;
            }
        }

        public bool IsZero
        {
            get { return _sign == 0; }
        }

        public int Sign
        {
            get { return _sign; }
        }

        public static BigInteger MinusOne
        {
            get { return new BigInteger(-1, ONE); }
        }

        public static BigInteger One
        {
            get { return new BigInteger(1, ONE); }
        }

        public static BigInteger Zero
        {
            get { return new BigInteger(0, ZERO); }
        }

        public static explicit operator int(BigInteger value)
        {
            if (value._sign == 0)
            {
                return 0;
            }
            if (value._data.Length > 1)
            {
                throw new OverflowException();
            }
            uint data = value._data[0];

            if (value._sign == 1)
            {
                if (data > int.MaxValue)
                {
                    throw new OverflowException();
                }
                return (int) data;
            }
            if (value._sign == -1)
            {
                if (data > 0x80000000u)
                {
                    throw new OverflowException();
                }
                return -(int) data;
            }

            return 0;
        }

        [CLSCompliant(false)]
        public static explicit operator uint(BigInteger value)
        {
            if (value._sign == 0)
            {
                return 0;
            }
            if (value._data.Length > 1 || value._sign == -1)
            {
                throw new OverflowException();
            }
            return value._data[0];
        }

        public static explicit operator short(BigInteger value)
        {
            var val = (int) value;
            if (val < short.MinValue || val > short.MaxValue)
            {
                throw new OverflowException();
            }
            return (short) val;
        }

        [CLSCompliant(false)]
        public static explicit operator ushort(BigInteger value)
        {
            var val = (uint) value;
            if (val > ushort.MaxValue)
            {
                throw new OverflowException();
            }
            return (ushort) val;
        }

        public static explicit operator byte(BigInteger value)
        {
            var val = (uint) value;
            if (val > byte.MaxValue)
            {
                throw new OverflowException();
            }
            return (byte) val;
        }

        [CLSCompliant(false)]
        public static explicit operator sbyte(BigInteger value)
        {
            var val = (int) value;
            if (val < sbyte.MinValue || val > sbyte.MaxValue)
            {
                throw new OverflowException();
            }
            return (sbyte) val;
        }


        public static explicit operator long(BigInteger value)
        {
            if (value._sign == 0)
            {
                return 0;
            }

            if (value._data.Length > 2)
            {
                throw new OverflowException();
            }

            uint low = value._data[0];

            if (value._data.Length == 1)
            {
                if (value._sign == 1)
                {
                    return low;
                }
                long res = low;
                return -res;
            }

            uint high = value._data[1];

            if (value._sign == 1)
            {
                if (high >= 0x80000000u)
                {
                    throw new OverflowException();
                }
                return (((long) high) << 32) | low;
            }

            /*
			We cannot represent negative numbers smaller than long.MinValue.
			Those values are encoded into what look negative numbers, so negating
			them produces a positive value, that's why it's safe to check for that
			condition.

			long.MinValue works fine since it's bigint encoding looks like a negative
			number, but since long.MinValue == -long.MinValue, we're good.
			*/

            long result = - ((((long) high) << 32) | low);
            if (result > 0)
            {
                throw new OverflowException();
            }
            return result;
        }

        [CLSCompliant(false)]
        public static explicit operator ulong(BigInteger value)
        {
            if (value._sign == 0)
            {
                return 0;
            }
            if (value._data.Length > 2 || value._sign == -1)
            {
                throw new OverflowException();
            }

            uint low = value._data[0];
            if (value._data.Length == 1)
            {
                return low;
            }

            uint high = value._data[1];
            return (((ulong) high) << 32) | low;
        }

        public static explicit operator double(BigInteger value)
        {
            //FIXME
            try
            {
                return double.Parse(value.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (OverflowException)
            {
                return value._sign == -1 ? double.NegativeInfinity : double.PositiveInfinity;
            }
        }

        public static explicit operator float(BigInteger value)
        {
            //FIXME
            try
            {
                return float.Parse(value.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (OverflowException)
            {
                return value._sign == -1 ? float.NegativeInfinity : float.PositiveInfinity;
            }
        }

        public static explicit operator decimal(BigInteger value)
        {
            if (value._sign == 0)
            {
                return Decimal.Zero;
            }

            uint[] data = value._data;
            if (data.Length > 3)
            {
                throw new OverflowException();
            }

            int lo = 0, mi = 0, hi = 0;
            if (data.Length > 2)
            {
                hi = (Int32) data[2];
            }
            if (data.Length > 1)
            {
                mi = (Int32) data[1];
            }
            if (data.Length > 0)
            {
                lo = (Int32) data[0];
            }

            return new Decimal(lo, mi, hi, value._sign < 0, 0);
        }

        public static implicit operator BigInteger(int value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(uint value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(short value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ushort value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(byte value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(sbyte value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(long value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ulong value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(double value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(float value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(decimal value)
        {
            return new BigInteger(value);
        }

        public static BigInteger operator +(BigInteger left, BigInteger right)
        {
            if (left._sign == 0)
            {
                return right;
            }
            if (right._sign == 0)
            {
                return left;
            }

            if (left._sign == right._sign)
            {
                return new BigInteger(left._sign, CoreAdd(left._data, right._data));
            }

            int r = CoreCompare(left._data, right._data);

            if (r == 0)
            {
                return new BigInteger(0, ZERO);
            }

            if (r > 0) //left > right
            {
                return new BigInteger(left._sign, CoreSub(left._data, right._data));
            }

            return new BigInteger(right._sign, CoreSub(right._data, left._data));
        }

        public static BigInteger operator -(BigInteger left, BigInteger right)
        {
            if (right._sign == 0)
            {
                return left;
            }
            if (left._sign == 0)
            {
                return new BigInteger((short) -right._sign, right._data);
            }

            if (left._sign == right._sign)
            {
                int r = CoreCompare(left._data, right._data);

                if (r == 0)
                {
                    return new BigInteger(0, ZERO);
                }

                if (r > 0) //left > right
                {
                    return new BigInteger(left._sign, CoreSub(left._data, right._data));
                }

                return new BigInteger((short) -right._sign, CoreSub(right._data, left._data));
            }

            return new BigInteger(left._sign, CoreAdd(left._data, right._data));
        }

        public static BigInteger operator *(BigInteger left, BigInteger right)
        {
            if (left._sign == 0 || right._sign == 0)
            {
                return new BigInteger(0, ZERO);
            }

            if (left._data[0] == 1 && left._data.Length == 1)
            {
                if (left._sign == 1)
                {
                    return right;
                }
                return new BigInteger((short) -right._sign, right._data);
            }

            if (right._data[0] == 1 && right._data.Length == 1)
            {
                if (right._sign == 1)
                {
                    return left;
                }
                return new BigInteger((short) -left._sign, left._data);
            }

            uint[] a = left._data;
            uint[] b = right._data;

            var res = new uint[a.Length + b.Length];

            for (int i = 0; i < a.Length; ++i)
            {
                uint ai = a[i];
                int k = i;

                ulong carry = 0;
                for (int j = 0; j < b.Length; ++j)
                {
                    carry = carry + ((ulong) ai)*b[j] + res[k];
                    res[k++] = (uint) carry;
                    carry >>= 32;
                }

                while (carry != 0)
                {
                    carry += res[k];
                    res[k++] = (uint) carry;
                    carry >>= 32;
                }
            }

            int m;
            for (m = res.Length - 1; m >= 0 && res[m] == 0; --m)
            {
                ;
            }
            if (m < res.Length - 1)
            {
                res = Resize(res, m + 1);
            }

            return new BigInteger((short) (left._sign*right._sign), res);
        }

        public static BigInteger operator /(BigInteger dividend, BigInteger divisor)
        {
            if (divisor._sign == 0)
            {
                throw new DivideByZeroException();
            }

            if (dividend._sign == 0)
            {
                return dividend;
            }

            uint[] quotient;
            uint[] remainder_value;

            DivModUnsigned(dividend._data, divisor._data, out quotient, out remainder_value);

            int i;
            for (i = quotient.Length - 1; i >= 0 && quotient[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }
            if (i < quotient.Length - 1)
            {
                quotient = Resize(quotient, i + 1);
            }

            return new BigInteger((short) (dividend._sign*divisor._sign), quotient);
        }

        public static BigInteger operator %(BigInteger dividend, BigInteger divisor)
        {
            if (divisor._sign == 0)
            {
                throw new DivideByZeroException();
            }

            if (dividend._sign == 0)
            {
                return dividend;
            }

            uint[] quotient;
            uint[] remainder_value;

            DivModUnsigned(dividend._data, divisor._data, out quotient, out remainder_value);

            int i;
            for (i = remainder_value.Length - 1; i >= 0 && remainder_value[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }

            if (i < remainder_value.Length - 1)
            {
                remainder_value = Resize(remainder_value, i + 1);
            }
            return new BigInteger(dividend._sign, remainder_value);
        }

        public static BigInteger operator -(BigInteger value)
        {
            if (value._sign == 0)
            {
                return value;
            }
            return new BigInteger((short) -value._sign, value._data);
        }

        public static BigInteger operator +(BigInteger value)
        {
            return value;
        }

        public static BigInteger operator ++(BigInteger value)
        {
            if (value._sign == 0)
            {
                return One;
            }

            short sign = value._sign;
            uint[] data = value._data;
            if (data.Length == 1)
            {
                if (sign == -1 && data[0] == 1)
                {
                    return new BigInteger(0, ZERO);
                }
                if (sign == 0)
                {
                    return new BigInteger(1, ONE);
                }
            }

            if (sign == -1)
            {
                data = CoreSub(data, 1);
            }
            else
            {
                data = CoreAdd(data, 1);
            }

            return new BigInteger(sign, data);
        }

        public static BigInteger operator --(BigInteger value)
        {
            if (value._sign == 0)
            {
                return MinusOne;
            }

            short sign = value._sign;
            uint[] data = value._data;
            if (data.Length == 1)
            {
                if (sign == 1 && data[0] == 1)
                {
                    return new BigInteger(0, ZERO);
                }
                if (sign == 0)
                {
                    return new BigInteger(-1, ONE);
                }
            }

            if (sign == -1)
            {
                data = CoreAdd(data, 1);
            }
            else
            {
                data = CoreSub(data, 1);
            }

            return new BigInteger(sign, data);
        }

        public static BigInteger operator &(BigInteger left, BigInteger right)
        {
            if (left._sign == 0)
            {
                return left;
            }

            if (right._sign == 0)
            {
                return right;
            }

            uint[] a = left._data;
            uint[] b = right._data;
            int ls = left._sign;
            int rs = right._sign;

            bool neg_res = (ls == rs) && (ls == -1);

            var result = new uint[Math.Max(a.Length, b.Length)];

            ulong ac = 1, bc = 1, borrow = 1;

            int i;
            for (i = 0; i < result.Length; ++i)
            {
                uint va = 0;
                if (i < a.Length)
                {
                    va = a[i];
                }
                if (ls == -1)
                {
                    ac = ~va + ac;
                    va = (uint) ac;
                    ac = (uint) (ac >> 32);
                }

                uint vb = 0;
                if (i < b.Length)
                {
                    vb = b[i];
                }
                if (rs == -1)
                {
                    bc = ~vb + bc;
                    vb = (uint) bc;
                    bc = (uint) (bc >> 32);
                }

                uint word = va & vb;

                if (neg_res)
                {
                    borrow = word - borrow;
                    word = ~(uint) borrow;
                    borrow = (uint) (borrow >> 32) & 0x1u;
                }

                result[i] = word;
            }

            for (i = result.Length - 1; i >= 0 && result[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }

            if (i < result.Length - 1)
            {
                result = Resize(result, i + 1);
            }

            return new BigInteger(neg_res ? (short) -1 : (short) 1, result);
        }

        public static BigInteger operator |(BigInteger left, BigInteger right)
        {
            if (left._sign == 0)
            {
                return right;
            }

            if (right._sign == 0)
            {
                return left;
            }

            uint[] a = left._data;
            uint[] b = right._data;
            int ls = left._sign;
            int rs = right._sign;

            bool neg_res = (ls == -1) || (rs == -1);

            var result = new uint[Math.Max(a.Length, b.Length)];

            ulong ac = 1, bc = 1, borrow = 1;

            int i;
            for (i = 0; i < result.Length; ++i)
            {
                uint va = 0;
                if (i < a.Length)
                {
                    va = a[i];
                }
                if (ls == -1)
                {
                    ac = ~va + ac;
                    va = (uint) ac;
                    ac = (uint) (ac >> 32);
                }

                uint vb = 0;
                if (i < b.Length)
                {
                    vb = b[i];
                }
                if (rs == -1)
                {
                    bc = ~vb + bc;
                    vb = (uint) bc;
                    bc = (uint) (bc >> 32);
                }

                uint word = va | vb;

                if (neg_res)
                {
                    borrow = word - borrow;
                    word = ~(uint) borrow;
                    borrow = (uint) (borrow >> 32) & 0x1u;
                }

                result[i] = word;
            }

            for (i = result.Length - 1; i >= 0 && result[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }

            if (i < result.Length - 1)
            {
                result = Resize(result, i + 1);
            }

            return new BigInteger(neg_res ? (short) -1 : (short) 1, result);
        }

        public static BigInteger operator ^(BigInteger left, BigInteger right)
        {
            if (left._sign == 0)
            {
                return right;
            }

            if (right._sign == 0)
            {
                return left;
            }

            uint[] a = left._data;
            uint[] b = right._data;
            int ls = left._sign;
            int rs = right._sign;

            bool neg_res = (ls == -1) ^ (rs == -1);

            var result = new uint[Math.Max(a.Length, b.Length)];

            ulong ac = 1, bc = 1, borrow = 1;

            int i;
            for (i = 0; i < result.Length; ++i)
            {
                uint va = 0;
                if (i < a.Length)
                {
                    va = a[i];
                }
                if (ls == -1)
                {
                    ac = ~va + ac;
                    va = (uint) ac;
                    ac = (uint) (ac >> 32);
                }

                uint vb = 0;
                if (i < b.Length)
                {
                    vb = b[i];
                }
                if (rs == -1)
                {
                    bc = ~vb + bc;
                    vb = (uint) bc;
                    bc = (uint) (bc >> 32);
                }

                uint word = va ^ vb;

                if (neg_res)
                {
                    borrow = word - borrow;
                    word = ~(uint) borrow;
                    borrow = (uint) (borrow >> 32) & 0x1u;
                }

                result[i] = word;
            }

            for (i = result.Length - 1; i >= 0 && result[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }

            if (i < result.Length - 1)
            {
                result = Resize(result, i + 1);
            }

            return new BigInteger(neg_res ? (short) -1 : (short) 1, result);
        }

        public static BigInteger operator ~(BigInteger value)
        {
            if (value._sign == 0)
            {
                return new BigInteger(-1, ONE);
            }

            uint[] data = value._data;
            int sign = value._sign;

            bool neg_res = sign == 1;

            var result = new uint[data.Length];

            ulong carry = 1, borrow = 1;

            int i;
            for (i = 0; i < result.Length; ++i)
            {
                uint word = data[i];
                if (sign == -1)
                {
                    carry = ~word + carry;
                    word = (uint) carry;
                    carry = (uint) (carry >> 32);
                }

                word = ~word;

                if (neg_res)
                {
                    borrow = word - borrow;
                    word = ~(uint) borrow;
                    borrow = (uint) (borrow >> 32) & 0x1u;
                }

                result[i] = word;
            }

            for (i = result.Length - 1; i >= 0 && result[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }

            if (i < result.Length - 1)
            {
                result = Resize(result, i + 1);
            }

            return new BigInteger(neg_res ? (short) -1 : (short) 1, result);
        }

        //returns the 0-based index of the most significant set bit
        //returns 0 if no bit is set, so extra care when using it
        private static int BitScanBackward(uint word)
        {
            for (int i = 31; i >= 0; --i)
            {
                uint mask = 1u << i;
                if ((word & mask) == mask)
                {
                    return i;
                }
            }
            return 0;
        }

        public static BigInteger operator <<(BigInteger value, int shift)
        {
            if (shift == 0 || value._sign == 0)
            {
                return value;
            }
            if (shift < 0)
            {
                return value >> -shift;
            }

            uint[] data = value._data;
            int sign = value._sign;

            int topMostIdx = BitScanBackward(data[data.Length - 1]);
            int bits = shift - (31 - topMostIdx);
            int extra_words = (bits >> 5) + ((bits & 0x1F) != 0 ? 1 : 0);

            var res = new uint[data.Length + extra_words];

            int idx_shift = shift >> 5;
            int bit_shift = shift & 0x1F;
            int carry_shift = 32 - bit_shift;

            if (carry_shift == 32)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    uint word = data[i];
                    res[i + idx_shift] |= word << bit_shift;
                }
            }
            else
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    uint word = data[i];
                    res[i + idx_shift] |= word << bit_shift;
                    if (i + idx_shift + 1 < res.Length)
                    {
                        res[i + idx_shift + 1] = word >> carry_shift;
                    }
                }
            }

            return new BigInteger((short) sign, res);
        }

        public static BigInteger operator >>(BigInteger value, int shift)
        {
            if (shift == 0 || value._sign == 0)
            {
                return value;
            }
            if (shift < 0)
            {
                return value << -shift;
            }

            uint[] data = value._data;
            int sign = value._sign;

            int topMostIdx = BitScanBackward(data[data.Length - 1]);
            int idx_shift = shift >> 5;
            int bit_shift = shift & 0x1F;

            int extra_words = idx_shift;
            if (bit_shift > topMostIdx)
            {
                ++extra_words;
            }
            int size = data.Length - extra_words;

            if (size <= 0)
            {
                if (sign == 1)
                {
                    return new BigInteger(0, ZERO);
                }
                return new BigInteger(-1, ONE);
            }

            var res = new uint[size];
            int carry_shift = 32 - bit_shift;

            if (carry_shift == 32)
            {
                for (int i = data.Length - 1; i >= idx_shift; --i)
                {
                    uint word = data[i];

                    if (i - idx_shift < res.Length)
                    {
                        res[i - idx_shift] |= word >> bit_shift;
                    }
                }
            }
            else
            {
                for (int i = data.Length - 1; i >= idx_shift; --i)
                {
                    uint word = data[i];

                    if (i - idx_shift < res.Length)
                    {
                        res[i - idx_shift] |= word >> bit_shift;
                    }
                    if (i - idx_shift - 1 >= 0)
                    {
                        res[i - idx_shift - 1] = word << carry_shift;
                    }
                }
            }

            //Round down instead of toward zero
            if (sign == -1)
            {
                for (int i = 0; i < idx_shift; i++)
                {
                    if (data[i] != 0u)
                    {
                        var tmp = new BigInteger((short) sign, res);
                        --tmp;
                        return tmp;
                    }
                }
                if (bit_shift > 0 && (data[idx_shift] << carry_shift) != 0u)
                {
                    var tmp = new BigInteger((short) sign, res);
                    --tmp;
                    return tmp;
                }
            }
            return new BigInteger((short) sign, res);
        }

        public static bool operator <(BigInteger left, BigInteger right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator <(BigInteger left, long right)
        {
            return left.CompareTo(right) < 0;
        }


        public static bool operator <(long left, BigInteger right)
        {
            return right.CompareTo(left) > 0;
        }


        [CLSCompliant(false)]
        public static bool operator <(BigInteger left, ulong right)
        {
            return left.CompareTo(right) < 0;
        }

        [CLSCompliant(false)]
        public static bool operator <(ulong left, BigInteger right)
        {
            return right.CompareTo(left) > 0;
        }

        public static bool operator <=(BigInteger left, BigInteger right)
        {
            return Compare(left, right) <= 0;
        }

        public static bool operator <=(BigInteger left, long right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator <=(long left, BigInteger right)
        {
            return right.CompareTo(left) >= 0;
        }

        [CLSCompliant(false)]
        public static bool operator <=(BigInteger left, ulong right)
        {
            return left.CompareTo(right) <= 0;
        }

        [CLSCompliant(false)]
        public static bool operator <=(ulong left, BigInteger right)
        {
            return right.CompareTo(left) >= 0;
        }

        public static bool operator >(BigInteger left, BigInteger right)
        {
            return Compare(left, right) > 0;
        }

        public static bool operator >(BigInteger left, long right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >(long left, BigInteger right)
        {
            return right.CompareTo(left) < 0;
        }

        [CLSCompliant(false)]
        public static bool operator >(BigInteger left, ulong right)
        {
            return left.CompareTo(right) > 0;
        }

        [CLSCompliant(false)]
        public static bool operator >(ulong left, BigInteger right)
        {
            return right.CompareTo(left) < 0;
        }

        public static bool operator >=(BigInteger left, BigInteger right)
        {
            return Compare(left, right) >= 0;
        }

        public static bool operator >=(BigInteger left, long right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator >=(long left, BigInteger right)
        {
            return right.CompareTo(left) <= 0;
        }

        [CLSCompliant(false)]
        public static bool operator >=(BigInteger left, ulong right)
        {
            return left.CompareTo(right) >= 0;
        }

        [CLSCompliant(false)]
        public static bool operator >=(ulong left, BigInteger right)
        {
            return right.CompareTo(left) <= 0;
        }

        public static bool operator ==(BigInteger left, BigInteger right)
        {
            return Compare(left, right) == 0;
        }

        public static bool operator ==(BigInteger left, long right)
        {
            return left.CompareTo(right) == 0;
        }

        public static bool operator ==(long left, BigInteger right)
        {
            return right.CompareTo(left) == 0;
        }

        [CLSCompliant(false)]
        public static bool operator ==(BigInteger left, ulong right)
        {
            return left.CompareTo(right) == 0;
        }

        [CLSCompliant(false)]
        public static bool operator ==(ulong left, BigInteger right)
        {
            return right.CompareTo(left) == 0;
        }

        public static bool operator !=(BigInteger left, BigInteger right)
        {
            return Compare(left, right) != 0;
        }

        public static bool operator !=(BigInteger left, long right)
        {
            return left.CompareTo(right) != 0;
        }

        public static bool operator !=(long left, BigInteger right)
        {
            return right.CompareTo(left) != 0;
        }

        [CLSCompliant(false)]
        public static bool operator !=(BigInteger left, ulong right)
        {
            return left.CompareTo(right) != 0;
        }

        [CLSCompliant(false)]
        public static bool operator !=(ulong left, BigInteger right)
        {
            return right.CompareTo(left) != 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BigInteger))
            {
                return false;
            }
            return Equals((BigInteger) obj);
        }

        public bool Equals(BigInteger other)
        {
            if (_sign != other._sign)
            {
                return false;
            }

            int alen = _data != null ? _data.Length : 0;
            int blen = other._data != null ? other._data.Length : 0;

            if (alen != blen)
            {
                return false;
            }
            for (int i = 0; i < alen; ++i)
            {
                if (_data[i] != other._data[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(long other)
        {
            return CompareTo(other) == 0;
        }

        public override string ToString()
        {
            return ToString(10, null);
        }

        private string ToStringWithPadding(string format, uint radix, IFormatProvider provider)
        {
            if (format.Length > 1)
            {
                int precision = Convert.ToInt32(format.Substring(1), CultureInfo.InvariantCulture.NumberFormat);
                string baseStr = ToString(radix, provider);
                if (baseStr.Length < precision)
                {
                    var additional = new String('0', precision - baseStr.Length);
                    if (baseStr[0] != '-')
                    {
                        return additional + baseStr;
                    }
                    return "-" + additional + baseStr.Substring(1);
                }
                return baseStr;
            }
            return ToString(radix, provider);
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString(null, provider);
        }


        public string ToString(string format, IFormatProvider provider)
        {
            if (format == null || format == "")
            {
                return ToString(10, provider);
            }

            switch (format[0])
            {
                case 'd':
                case 'D':
                case 'g':
                case 'G':
                case 'r':
                case 'R':
                    return ToStringWithPadding(format, 10, provider);
                case 'x':
                case 'X':
                    return ToStringWithPadding(format, 16, null);
                default:
                    throw new FormatException(string.Format("format '{0}' not implemented", format));
            }
        }

        private static uint[] MakeTwoComplement(uint[] v)
        {
            var res = new uint[v.Length];

            ulong carry = 1;
            for (int i = 0; i < v.Length; ++i)
            {
                uint word = v[i];
                carry = ~word + carry;
                word = (uint) carry;
                carry = (uint) (carry >> 32);
                res[i] = word;
            }

            uint last = res[res.Length - 1];
            int idx = FirstNonFFByte(last);
            uint mask = 0xFF;
            for (int i = 1; i < idx; ++i)
            {
                mask = (mask << 8) | 0xFF;
            }

            res[res.Length - 1] = last & mask;
            return res;
        }

        private string ToString(uint radix, IFormatProvider provider)
        {
            const string characterSet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (characterSet.Length < radix)
            {
                throw new ArgumentException("charSet length less than radix", "characterSet");
            }
            if (radix == 1)
            {
                throw new ArgumentException("There is no such thing as radix one notation", "radix");
            }

            if (_sign == 0)
            {
                return "0";
            }
            if (_data.Length == 1 && _data[0] == 1)
            {
                return _sign == 1 ? "1" : "-1";
            }

            var digits = new List<char>(1 + _data.Length*3/10);

            BigInteger a;
            if (_sign == 1)
            {
                a = this;
            }
            else
            {
                uint[] dt = _data;
                if (radix > 10)
                {
                    dt = MakeTwoComplement(dt);
                }
                a = new BigInteger(1, dt);
            }

            while (a != 0)
            {
                BigInteger rem;
                a = DivRem(a, radix, out rem);
                digits.Add(characterSet[(int) rem]);
            }

            if (_sign == -1 && radix == 10)
            {
                NumberFormatInfo info = null;
                if (provider != null)
                {
                    info = provider.GetFormat(typeof (NumberFormatInfo)) as NumberFormatInfo;
                }
                if (info != null)
                {
                    string str = info.NegativeSign;
                    for (int i = str.Length - 1; i >= 0; --i)
                    {
                        digits.Add(str[i]);
                    }
                }
                else
                {
                    digits.Add('-');
                }
            }

            char last = digits[digits.Count - 1];
            if (_sign == 1 && radix > 10 && (last < '0' || last > '9'))
            {
                digits.Add('0');
            }

            digits.Reverse();

            return new String(digits.ToArray());
        }

        public static BigInteger Parse(string value)
        {
            Exception ex;
            BigInteger result;

            if (!Parse(value, false, out result, out ex))
            {
                throw ex;
            }
            return result;
        }

        public static bool TryParse(string value, out BigInteger result)
        {
            Exception ex;
            return Parse(value, true, out result, out ex);
        }

#if NET_4_0
		[MonoTODO]
		public static BigInteger Parse (string value, NumberStyles style)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static BigInteger Parse (string value, IFormatProvider provider)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static BigInteger Parse (
			string value, NumberStyles style, IFormatProvider provider)
		{
			throw new InvalidOperationException ();
		}
		
		[MonoTODO]
		public static bool TryParse (
			string value, NumberStyles style, IFormatProvider provider,
			out BigInteger result)
		{
			throw new NotImplementedException ();
		}
#endif


        private static Exception GetFormatException()
        {
            return new FormatException("Input string was not in the correct format");
        }

        private static bool ProcessTrailingWhitespace(bool tryParse, string s, int position, ref Exception exc)
        {
            int len = s.Length;

            for (int i = position; i < len; i++)
            {
                char c = s[i];

                if (c != 0 && !Char.IsWhiteSpace(c))
                {
                    if (!tryParse)
                    {
                        exc = GetFormatException();
                    }
                    return false;
                }
            }
            return true;
        }

        private static bool Parse(string s, bool tryParse, out BigInteger result, out Exception exc)
        {
            int len;
            int i, sign = 1;
            bool digits_seen = false;

            result = Zero;
            exc = null;

            if (s == null)
            {
                if (!tryParse)
                {
                    exc = new ArgumentNullException("value");
                }
                return false;
            }

            len = s.Length;

            char c;
            for (i = 0; i < len; i++)
            {
                c = s[i];
                if (!Char.IsWhiteSpace(c))
                {
                    break;
                }
            }

            if (i == len)
            {
                if (!tryParse)
                {
                    exc = GetFormatException();
                }
                return false;
            }

            NumberFormatInfo info = CultureInfo.CurrentCulture.NumberFormat;

            string negative = info.NegativeSign;
            string positive = info.PositiveSign;

            if (string.CompareOrdinal(s, i, positive, 0, positive.Length) == 0)
            {
                i += positive.Length;
            }
            else if (string.CompareOrdinal(s, i, negative, 0, negative.Length) == 0)
            {
                sign = -1;
                i += negative.Length;
            }

            BigInteger val = Zero;
            for (; i < len; i++)
            {
                c = s[i];

                if (c == '\0')
                {
                    i = len;
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    var d = (byte) (c - '0');

                    val = val*10 + d;

                    digits_seen = true;
                }
                else if (!ProcessTrailingWhitespace(tryParse, s, i, ref exc))
                {
                    return false;
                }
            }

            if (!digits_seen)
            {
                if (!tryParse)
                {
                    exc = GetFormatException();
                }
                return false;
            }

            if (val._sign == 0)
            {
                result = val;
            }
            else if (sign == -1)
            {
                result = new BigInteger(-1, val._data);
            }
            else
            {
                result = new BigInteger(1, val._data);
            }

            return true;
        }

        public static BigInteger Min(BigInteger left, BigInteger right)
        {
            int ls = left._sign;
            int rs = right._sign;

            if (ls < rs)
            {
                return left;
            }
            if (rs < ls)
            {
                return right;
            }

            int r = CoreCompare(left._data, right._data);
            if (ls == -1)
            {
                r = -r;
            }

            if (r <= 0)
            {
                return left;
            }
            return right;
        }


        public static BigInteger Max(BigInteger left, BigInteger right)
        {
            int ls = left._sign;
            int rs = right._sign;

            if (ls > rs)
            {
                return left;
            }
            if (rs > ls)
            {
                return right;
            }

            int r = CoreCompare(left._data, right._data);
            if (ls == -1)
            {
                r = -r;
            }

            if (r >= 0)
            {
                return left;
            }
            return right;
        }

        public static BigInteger Abs(BigInteger value)
        {
            return new BigInteger(Math.Abs(value._sign), value._data);
        }


        public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out BigInteger remainder)
        {
            if (divisor._sign == 0)
            {
                throw new DivideByZeroException();
            }

            if (dividend._sign == 0)
            {
                remainder = dividend;
                return dividend;
            }

            uint[] quotient;
            uint[] remainder_value;

            DivModUnsigned(dividend._data, divisor._data, out quotient, out remainder_value);

            int i;
            for (i = remainder_value.Length - 1; i >= 0 && remainder_value[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                remainder = new BigInteger(0, ZERO);
            }
            else
            {
                if (i < remainder_value.Length - 1)
                {
                    remainder_value = Resize(remainder_value, i + 1);
                }
                remainder = new BigInteger(dividend._sign, remainder_value);
            }

            for (i = quotient.Length - 1; i >= 0 && quotient[i] == 0; --i)
            {
                ;
            }
            if (i == -1)
            {
                return new BigInteger(0, ZERO);
            }
            if (i < quotient.Length - 1)
            {
                quotient = Resize(quotient, i + 1);
            }

            return new BigInteger((short) (dividend._sign*divisor._sign), quotient);
        }

        public static BigInteger Pow(BigInteger value, int exponent)
        {
            if (exponent < 0)
            {
                throw new ArgumentOutOfRangeException("exponent", "exp must be >= 0");
            }
            if (exponent == 0)
            {
                return One;
            }
            if (exponent == 1)
            {
                return value;
            }

            BigInteger result = One;
            while (exponent != 0)
            {
                if ((exponent & 1) != 0)
                {
                    result = result*value;
                }
                if (exponent == 1)
                {
                    break;
                }

                value = value*value;
                exponent >>= 1;
            }
            return result;
        }

        public static BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus)
        {
            if (exponent._sign == -1)
            {
                throw new ArgumentOutOfRangeException("exponent", "power must be >= 0");
            }
            if (modulus._sign == 0)
            {
                throw new DivideByZeroException();
            }

            BigInteger result = One%modulus;
            while (exponent._sign != 0)
            {
                if (!exponent.IsEven)
                {
                    result = result*value;
                    result = result%modulus;
                }
                if (exponent.IsOne)
                {
                    break;
                }
                value = value*value;
                value = value%modulus;
                exponent >>= 1;
            }
            return result;
        }

        public static BigInteger GreatestCommonDivisor(BigInteger left, BigInteger right)
        {
            if (left._sign != 0 && left._data.Length == 1 && left._data[0] == 1)
            {
                return new BigInteger(1, ONE);
            }
            if (right._sign != 0 && right._data.Length == 1 && right._data[0] == 1)
            {
                return new BigInteger(1, ONE);
            }
            if (left.IsZero)
            {
                return Abs(right);
            }
            if (right.IsZero)
            {
                return Abs(left);
            }

            var x = new BigInteger(1, left._data);
            var y = new BigInteger(1, right._data);

            BigInteger g = y;

            while (x._data.Length > 1)
            {
                g = x;
                x = y%x;
                y = g;
            }
            if (x.IsZero)
            {
                return g;
            }

            // TODO: should we have something here if we can convert to long?

            //
            // Now we can just do it with single precision. I am using the binary gcd method,
            // as it should be faster.
            //

            uint yy = x._data[0];
            var xx = (uint) (y%yy);

            int t = 0;

            while (((xx | yy) & 1) == 0)
            {
                xx >>= 1;
                yy >>= 1;
                t++;
            }
            while (xx != 0)
            {
                while ((xx & 1) == 0)
                {
                    xx >>= 1;
                }
                while ((yy & 1) == 0)
                {
                    yy >>= 1;
                }
                if (xx >= yy)
                {
                    xx = (xx - yy) >> 1;
                }
                else
                {
                    yy = (yy - xx) >> 1;
                }
            }

            return yy << t;
        }

        /*LAMESPEC Log doesn't specify to how many ulp is has to be precise
		We are equilavent to MS with about 2 ULP
		*/

        public static double Log(BigInteger value, Double baseValue)
        {
            if (value._sign == -1 || baseValue == 1.0d || baseValue == -1.0d || baseValue == Double.NegativeInfinity || double.IsNaN(baseValue))
            {
                return double.NaN;
            }

            if (baseValue == 0.0d || baseValue == Double.PositiveInfinity)
            {
                return value.IsOne ? 0 : double.NaN;
            }

            if (value._sign == 0)
            {
                return double.NegativeInfinity;
            }

            int length = value._data.Length - 1;
            int bitCount = -1;
            for (int curBit = 31; curBit >= 0; curBit--)
            {
                if ((value._data[length] & (1 << curBit)) != 0)
                {
                    bitCount = curBit + length*32;
                    break;
                }
            }

            long bitlen = bitCount;
            Double c = 0, d = 1;

            BigInteger testBit = One;
            long tempBitlen = bitlen;
            while (tempBitlen > Int32.MaxValue)
            {
                testBit = testBit << Int32.MaxValue;
                tempBitlen -= Int32.MaxValue;
            }
            testBit = testBit << (int) tempBitlen;

            for (long curbit = bitlen; curbit >= 0; --curbit)
            {
                if ((value & testBit)._sign != 0)
                {
                    c += d;
                }
                d *= 0.5;
                testBit = testBit >> 1;
            }
            return (Math.Log(c) + Math.Log(2)*bitlen)/Math.Log(baseValue);
        }


        public static double Log(BigInteger value)
        {
            return Log(value, Math.E);
        }


        public static double Log10(BigInteger value)
        {
            return Log(value, 10);
        }

        [CLSCompliant(false)]
        public bool Equals(ulong other)
        {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            var hash = (uint) (_sign*0x01010101u);
            int len = _data != null ? _data.Length : 0;

            for (int i = 0; i < len; ++i)
            {
                hash ^= _data[i];
            }
            return (int) hash;
        }

        public static BigInteger Add(BigInteger left, BigInteger right)
        {
            return left + right;
        }

        public static BigInteger Subtract(BigInteger left, BigInteger right)
        {
            return left - right;
        }

        public static BigInteger Multiply(BigInteger left, BigInteger right)
        {
            return left*right;
        }

        public static BigInteger Divide(BigInteger dividend, BigInteger divisor)
        {
            return dividend/divisor;
        }

        public static BigInteger Remainder(BigInteger dividend, BigInteger divisor)
        {
            return dividend%divisor;
        }

        public static BigInteger Negate(BigInteger value)
        {
            return - value;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is BigInteger))
            {
                return -1;
            }

            return Compare(this, (BigInteger) obj);
        }

        public int CompareTo(BigInteger other)
        {
            return Compare(this, other);
        }

        [CLSCompliant(false)]
        public int CompareTo(ulong other)
        {
            if (_sign < 0)
            {
                return -1;
            }
            if (_sign == 0)
            {
                return other == 0 ? 0 : -1;
            }

            if (_data.Length > 2)
            {
                return 1;
            }

            var high = (uint) (other >> 32);
            var low = (uint) other;

            return LongCompare(low, high);
        }

        private int LongCompare(uint low, uint high)
        {
            uint h = 0;
            if (_data.Length > 1)
            {
                h = _data[1];
            }

            if (h > high)
            {
                return 1;
            }
            if (h < high)
            {
                return -1;
            }

            uint l = _data[0];

            if (l > low)
            {
                return 1;
            }
            if (l < low)
            {
                return -1;
            }

            return 0;
        }

        public int CompareTo(long other)
        {
            int ls = _sign;
            int rs = Math.Sign(other);

            if (ls != rs)
            {
                return ls > rs ? 1 : -1;
            }

            if (ls == 0)
            {
                return 0;
            }

            if (_data.Length > 2)
            {
                return _sign;
            }

            if (other < 0)
            {
                other = -other;
            }
            var low = (uint) other;
            var high = (uint) ((ulong) other >> 32);

            int r = LongCompare(low, high);
            if (ls == -1)
            {
                r = -r;
            }

            return r;
        }

        public static int Compare(BigInteger left, BigInteger right)
        {
            int ls = left._sign;
            int rs = right._sign;

            if (ls != rs)
            {
                return ls > rs ? 1 : -1;
            }

            int r = CoreCompare(left._data, right._data);
            if (ls < 0)
            {
                r = -r;
            }
            return r;
        }


        private static int TopByte(uint x)
        {
            if ((x & 0xFFFF0000u) != 0)
            {
                if ((x & 0xFF000000u) != 0)
                {
                    return 4;
                }
                return 3;
            }
            if ((x & 0xFF00u) != 0)
            {
                return 2;
            }
            return 1;
        }

        private static int FirstNonFFByte(uint word)
        {
            if ((word & 0xFF000000u) != 0xFF000000u)
            {
                return 4;
            }
            if ((word & 0xFF0000u) != 0xFF0000u)
            {
                return 3;
            }
            if ((word & 0xFF00u) != 0xFF00u)
            {
                return 2;
            }
            return 1;
        }

        public byte[] ToByteArray()
        {
            if (_sign == 0)
            {
                return new byte[1];
            }

            //number of bytes not counting upper word
            int bytes = (_data.Length - 1)*4;
            bool needExtraZero = false;

            uint topWord = _data[_data.Length - 1];
            int extra;

            //if the topmost bit is set we need an extra 
            if (_sign == 1)
            {
                extra = TopByte(topWord);
                uint mask = 0x80u << ((extra - 1)*8);
                if ((topWord & mask) != 0)
                {
                    needExtraZero = true;
                }
            }
            else
            {
                extra = TopByte(topWord);
            }

            var res = new byte[bytes + extra + (needExtraZero ? 1 : 0)];
            if (_sign == 1)
            {
                int j = 0;
                int end = _data.Length - 1;
                for (int i = 0; i < end; ++i)
                {
                    uint word = _data[i];

                    res[j++] = (byte) word;
                    res[j++] = (byte) (word >> 8);
                    res[j++] = (byte) (word >> 16);
                    res[j++] = (byte) (word >> 24);
                }
                while (extra-- > 0)
                {
                    res[j++] = (byte) topWord;
                    topWord >>= 8;
                }
            }
            else
            {
                int j = 0;
                int end = _data.Length - 1;

                uint carry = 1, word;
                ulong add;
                for (int i = 0; i < end; ++i)
                {
                    word = _data[i];
                    add = (ulong) ~word + carry;
                    word = (uint) add;
                    carry = (uint) (add >> 32);

                    res[j++] = (byte) word;
                    res[j++] = (byte) (word >> 8);
                    res[j++] = (byte) (word >> 16);
                    res[j++] = (byte) (word >> 24);
                }

                add = (ulong) ~topWord + (carry);
                word = (uint) add;
                carry = (uint) (add >> 32);
                if (carry == 0)
                {
                    int ex = FirstNonFFByte(word);
                    bool needExtra = (word & (1 << (ex*8 - 1))) == 0;
                    int to = ex + (needExtra ? 1 : 0);

                    if (to != extra)
                    {
                        res = Resize(res, bytes + to);
                    }

                    while (ex-- > 0)
                    {
                        res[j++] = (byte) word;
                        word >>= 8;
                    }
                    if (needExtra)
                    {
                        res[j++] = 0xFF;
                    }
                }
                else
                {
                    res = Resize(res, bytes + 5);
                    res[j++] = (byte) word;
                    res[j++] = (byte) (word >> 8);
                    res[j++] = (byte) (word >> 16);
                    res[j++] = (byte) (word >> 24);
                    res[j++] = 0xFF;
                }
            }

            return res;
        }

        private static byte[] Resize(byte[] v, int len)
        {
            var res = new byte[len];
            Array.Copy(v, res, Math.Min(v.Length, len));
            return res;
        }

        private static uint[] Resize(uint[] v, int len)
        {
            var res = new uint[len];
            Array.Copy(v, res, Math.Min(v.Length, len));
            return res;
        }

        private static uint[] CoreAdd(uint[] a, uint[] b)
        {
            if (a.Length < b.Length)
            {
                uint[] tmp = a;
                a = b;
                b = tmp;
            }

            int bl = a.Length;
            int sl = b.Length;

            var res = new uint[bl];

            ulong sum = 0;

            int i = 0;
            for (; i < sl; i++)
            {
                sum = sum + a[i] + b[i];
                res[i] = (uint) sum;
                sum >>= 32;
            }

            for (; i < bl; i++)
            {
                sum = sum + a[i];
                res[i] = (uint) sum;
                sum >>= 32;
            }

            if (sum != 0)
            {
                res = Resize(res, bl + 1);
                res[i] = (uint) sum;
            }

            return res;
        }

        /*invariant a > b*/

        private static uint[] CoreSub(uint[] a, uint[] b)
        {
            int bl = a.Length;
            int sl = b.Length;

            var res = new uint[bl];

            ulong borrow = 0;
            int i;
            for (i = 0; i < sl; ++i)
            {
                borrow = (ulong) a[i] - b[i] - borrow;

                res[i] = (uint) borrow;
                borrow = (borrow >> 32) & 0x1;
            }

            for (; i < bl; i++)
            {
                borrow = a[i] - borrow;
                res[i] = (uint) borrow;
                borrow = (borrow >> 32) & 0x1;
            }

            //remove extra zeroes
            for (i = bl - 1; i >= 0 && res[i] == 0; --i)
            {
                ;
            }
            if (i < bl - 1)
            {
                res = Resize(res, i + 1);
            }

            return res;
        }


        private static uint[] CoreAdd(uint[] a, uint b)
        {
            int len = a.Length;
            var res = new uint[len];

            ulong sum = b;
            int i;
            for (i = 0; i < len; i++)
            {
                sum = sum + a[i];
                res[i] = (uint) sum;
                sum >>= 32;
            }

            if (sum != 0)
            {
                res = Resize(res, len + 1);
                res[i] = (uint) sum;
            }

            return res;
        }

        private static uint[] CoreSub(uint[] a, uint b)
        {
            int len = a.Length;
            var res = new uint[len];

            ulong borrow = b;
            int i;
            for (i = 0; i < len; i++)
            {
                borrow = a[i] - borrow;
                res[i] = (uint) borrow;
                borrow = (borrow >> 32) & 0x1;
            }

            //remove extra zeroes
            for (i = len - 1; i >= 0 && res[i] == 0; --i)
            {
                ;
            }
            if (i < len - 1)
            {
                res = Resize(res, i + 1);
            }

            return res;
        }

        private static int CoreCompare(uint[] a, uint[] b)
        {
            int al = a != null ? a.Length : 0;
            int bl = b != null ? b.Length : 0;

            if (al > bl)
            {
                return 1;
            }
            if (bl > al)
            {
                return -1;
            }

            for (int i = al - 1; i >= 0; --i)
            {
                uint ai = a[i];
                uint bi = b[i];
                if (ai > bi)
                {
                    return 1;
                }
                if (ai < bi)
                {
                    return -1;
                }
            }
            return 0;
        }

        private static int GetNormalizeShift(uint value)
        {
            int shift = 0;

            if ((value & 0xFFFF0000) == 0)
            {
                value <<= 16;
                shift += 16;
            }
            if ((value & 0xFF000000) == 0)
            {
                value <<= 8;
                shift += 8;
            }
            if ((value & 0xF0000000) == 0)
            {
                value <<= 4;
                shift += 4;
            }
            if ((value & 0xC0000000) == 0)
            {
                value <<= 2;
                shift += 2;
            }
            if ((value & 0x80000000) == 0)
            {
                value <<= 1;
                shift += 1;
            }

            return shift;
        }

        private static void Normalize(uint[] u, int l, uint[] un, int shift)
        {
            uint carry = 0;
            int i;
            if (shift > 0)
            {
                int rshift = 32 - shift;
                for (i = 0; i < l; i++)
                {
                    uint ui = u[i];
                    un[i] = (ui << shift) | carry;
                    carry = ui >> rshift;
                }
            }
            else
            {
                for (i = 0; i < l; i++)
                {
                    un[i] = u[i];
                }
            }

            while (i < un.Length)
            {
                un[i++] = 0;
            }

            if (carry != 0)
            {
                un[l] = carry;
            }
        }

        private static void Unnormalize(uint[] un, out uint[] r, int shift)
        {
            int length = un.Length;
            r = new uint[length];

            if (shift > 0)
            {
                int lshift = 32 - shift;
                uint carry = 0;
                for (int i = length - 1; i >= 0; i--)
                {
                    uint uni = un[i];
                    r[i] = (uni >> shift) | carry;
                    carry = (uni << lshift);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    r[i] = un[i];
                }
            }
        }

        private const ulong Base = 0x100000000;

        private static void DivModUnsigned(uint[] u, uint[] v, out uint[] q, out uint[] r)
        {
            int m = u.Length;
            int n = v.Length;

            if (n <= 1)
            {
                //  Divide by single digit
                //
                ulong rem = 0;
                uint v0 = v[0];
                q = new uint[m];
                r = new uint[1];

                for (int j = m - 1; j >= 0; j--)
                {
                    rem *= Base;
                    rem += u[j];

                    ulong div = rem/v0;
                    rem -= div*v0;
                    q[j] = (uint) div;
                }
                r[0] = (uint) rem;
            }
            else if (m >= n)
            {
                int shift = GetNormalizeShift(v[n - 1]);

                var un = new uint[m + 1];
                var vn = new uint[n];

                Normalize(u, m, un, shift);
                Normalize(v, n, vn, shift);

                q = new uint[m - n + 1];
                r = null;

                //  Main division loop
                //
                for (int j = m - n; j >= 0; j--)
                {
                    ulong rr, qq;
                    int i;

                    rr = Base*un[j + n] + un[j + n - 1];
                    qq = rr/vn[n - 1];
                    rr -= qq*vn[n - 1];

                    for (;;)
                    {
                        // Estimate too big ?
                        //
                        if ((qq >= Base) || (qq*vn[n - 2] > (rr*Base + un[j + n - 2])))
                        {
                            qq--;
                            rr += vn[n - 1];
                            if (rr < Base)
                            {
                                continue;
                            }
                        }
                        break;
                    }


                    //  Multiply and subtract
                    //
                    long b = 0;
                    long t = 0;
                    for (i = 0; i < n; i++)
                    {
                        ulong p = vn[i]*qq;
                        t = un[i + j] - (long) (uint) p - b;
                        un[i + j] = (uint) t;
                        p >>= 32;
                        t >>= 32;
                        b = (long) p - t;
                    }
                    t = un[j + n] - b;
                    un[j + n] = (uint) t;

                    //  Store the calculated value
                    //
                    q[j] = (uint) qq;

                    //  Add back vn[0..n] to un[j..j+n]
                    //
                    if (t < 0)
                    {
                        q[j]--;
                        ulong c = 0;
                        for (i = 0; i < n; i++)
                        {
                            c = (ulong) vn[i] + un[j + i] + c;
                            un[j + i] = (uint) c;
                            c >>= 32;
                        }
                        c += un[j + n];
                        un[j + n] = (uint) c;
                    }
                }

                Unnormalize(un, out r, shift);
            }
            else
            {
                q = new uint[] {0};
                r = u;
            }
        }
    }
}
