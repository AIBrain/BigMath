// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedBitConverter.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace BigMath.Utils
{
    /// <summary>
    ///     Bit converter methods which support explicit endian.
    /// </summary>
    public static class ExtendedBitConverter
    {
        /// <summary>
        ///     Indicates the byte order ("endianness") in which data is stored in this computer architecture.
        /// </summary>
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        #region Int32
        /// <summary>
        ///     Converts <see cref="int" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this int value, bool? asLittleEndian = null)
        {
            return unchecked ((uint) value).ToBytes(asLittleEndian);
        }

        /// <summary>
        ///     Converts <see cref="int" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="buffer">Buffer at least 4 bytes.</param>
        /// <param name="offset">The starting position within <paramref name="buffer" />.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToBytes(this int value, byte[] buffer, int offset = 0, bool? asLittleEndian = null)
        {
            unchecked((uint) value).ToBytes(buffer, offset, asLittleEndian);
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="int" />.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="int" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(this byte[] bytes, int offset = 0, bool? asLittleEndian = null)
        {
            if (bytes.Length - offset < 4)
            {
                throw new ArgumentOutOfRangeException("bytes",
                    string.Format("Length of bytes array minus offset must NOT be less than 4, actual is {0}.", bytes.Length - offset));
            }

            return (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
                ? bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24
                : bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
        }
        #endregion

        #region UInt32
        /// <summary>
        ///     Converts <see cref="uint" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this uint value, bool? asLittleEndian = null)
        {
            var buffer = new byte[4];
            value.ToBytes(buffer, 0, asLittleEndian);
            return buffer;
        }

        /// <summary>
        ///     Converts <see cref="uint" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="buffer">Buffer at least 4 bytes.</param>
        /// <param name="offset">The starting position within <paramref name="buffer" />.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToBytes(this uint value, byte[] buffer, int offset = 0, bool? asLittleEndian = null)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
            {
                buffer[offset] = (byte) value;
                buffer[offset + 1] = (byte) (value >> 8);
                buffer[offset + 2] = (byte) (value >> 16);
                buffer[offset + 3] = (byte) (value >> 24);
            }
            else
            {
                buffer[offset + 0] = (byte) (value >> 24);
                buffer[offset + 1] = (byte) (value >> 16);
                buffer[offset + 2] = (byte) (value >> 8);
                buffer[offset + 3] = (byte) value;
            }
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="uint" />.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="uint" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(this byte[] bytes, int offset = 0, bool? asLittleEndian = null)
        {
            return (uint) bytes.ToInt32(offset, asLittleEndian);
        }
        #endregion

        #region Int64
        /// <summary>
        ///     Converts <see cref="long" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this long value, bool? asLittleEndian = null)
        {
            var buffer = new byte[8];
            value.ToBytes(buffer, 0, asLittleEndian);
            return buffer;
        }

        /// <summary>
        ///     Converts <see cref="long" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="buffer">Buffer at least 8 bytes.</param>
        /// <param name="offset">The starting position within <paramref name="buffer" />.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToBytes(this long value, byte[] buffer, int offset = 0, bool? asLittleEndian = null)
        {
            if (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
            {
                buffer[offset] = (byte) value;
                buffer[offset + 1] = (byte) (value >> 8);
                buffer[offset + 2] = (byte) (value >> 16);
                buffer[offset + 3] = (byte) (value >> 24);
                buffer[offset + 4] = (byte) (value >> 32);
                buffer[offset + 5] = (byte) (value >> 40);
                buffer[offset + 6] = (byte) (value >> 48);
                buffer[offset + 7] = (byte) (value >> 56);
            }
            else
            {
                buffer[offset] = (byte) (value >> 56);
                buffer[offset + 1] = (byte) (value >> 48);
                buffer[offset + 2] = (byte) (value >> 40);
                buffer[offset + 3] = (byte) (value >> 32);
                buffer[offset + 4] = (byte) (value >> 24);
                buffer[offset + 5] = (byte) (value >> 16);
                buffer[offset + 6] = (byte) (value >> 8);
                buffer[offset + 7] = (byte) value;
            }
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="long" />.
        /// </summary>
        /// <param name="bytes">An array of bytes. </param>
        /// <param name="offset">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="long" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(this byte[] bytes, int offset = 0, bool? asLittleEndian = null)
        {
            if (bytes.Length - offset < 8)
            {
                throw new ArgumentOutOfRangeException("bytes",
                    string.Format("Length of bytes array minus offset must NOT be less than 8, actual is {0}.", bytes.Length - offset));
            }

            return (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
                ? bytes[offset] | (long) bytes[offset + 1] << 8 | (long) bytes[offset + 2] << 16 | (long) bytes[offset + 3] << 24 | (long) bytes[offset + 4] << 32 |
                    (long) bytes[offset + 5] << 40 | (long) bytes[offset + 6] << 48 | (long) bytes[offset + 7] << 56
                : (long) bytes[offset] << 56 | (long) bytes[offset + 1] << 48 | (long) bytes[offset + 2] << 40 | (long) bytes[offset + 3] << 32 |
                    (long) bytes[offset + 4] << 24 | (long) bytes[offset + 5] << 16 | (long) bytes[offset + 6] << 8 | bytes[offset + 7];
        }
        #endregion

        #region UInt64
        /// <summary>
        ///     Converts <see cref="ulong" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this ulong value, bool? asLittleEndian = null)
        {
            return unchecked ((long) value).ToBytes(asLittleEndian);
        }

        /// <summary>
        ///     Converts <see cref="ulong" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="buffer">Buffer at least 8 bytes.</param>
        /// <param name="offset">The starting position within <paramref name="buffer" />.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToBytes(this ulong value, byte[] buffer, int offset = 0, bool? asLittleEndian = null)
        {
            unchecked((long) value).ToBytes(buffer, offset, asLittleEndian);
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="ulong" />.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="ulong" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(this byte[] bytes, int offset = 0, bool? asLittleEndian = null)
        {
            return (ulong) bytes.ToInt64(offset, asLittleEndian);
        }
        #endregion

        #region Int128
        /// <summary>
        ///     Converts an <see cref="Int128" /> value to an array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="buffer" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        public static void ToBytes(this Int128 value, byte[] buffer, int offset = 0, bool? asLittleEndian = null)
        {
            bool ale = GetIsLittleEndian(asLittleEndian);
            value.Low.ToBytes(buffer, ale ? offset : offset + 8, ale);
            value.High.ToBytes(buffer, ale ? offset + 8 : offset, ale);
        }

        /// <summary>
        ///     Converts an <see cref="Int128" /> value to a byte array.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <param name="trimZeros">Trim zero bytes from left or right, depending on endian.</param>
        /// <returns>Array of bytes.</returns>
        public static byte[] ToBytes(this Int128 value, bool? asLittleEndian = null, bool trimZeros = false)
        {
            var buffer = new byte[16];
            value.ToBytes(buffer, 0, asLittleEndian);

            if (trimZeros)
                buffer = buffer.TrimZeros(asLittleEndian);

            return buffer;
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="Int128" />.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="Int128" /> value.</returns>
        public static Int128 ToInt128(this byte[] bytes, int offset = 0, bool? asLittleEndian = null)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length < offset)
            {
                throw new InvalidOperationException("Array length could not be less than offset.");
            }

            bool ale = GetIsLittleEndian(asLittleEndian);

            byte[] data;
            if (bytes.Length - offset < 16)
            {
                data = new byte[16];
                Buffer.BlockCopy(bytes, offset, data, ale ? 0 : 16 - (bytes.Length - offset), bytes.Length - offset);
            }
            else
            {
                data = bytes;
            }

            return new Int128(data.ToUInt64(ale ? offset + 8 : offset, ale), data.ToUInt64(ale ? offset : offset + 8, ale));
        }
        #endregion

        #region Int256
        /// <summary>
        ///     Converts an <see cref="Int256" /> value to an array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="buffer" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        public static void ToBytes(this Int256 value, byte[] buffer, int offset = 0, bool? asLittleEndian = null)
        {
            bool ale = GetIsLittleEndian(asLittleEndian);

            value.D.ToBytes(buffer, ale ? offset : offset + 24, ale);
            value.C.ToBytes(buffer, ale ? offset + 8 : offset + 16, ale);
            value.B.ToBytes(buffer, ale ? offset + 16 : offset + 8, ale);
            value.A.ToBytes(buffer, ale ? offset + 24 : offset, ale);
        }

        /// <summary>
        ///     Converts an <see cref="Int256" /> value to a byte array.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <param name="trimZeros">Trim zero bytes from left or right, depending on endian.</param>
        /// <returns>Array of bytes.</returns>
        public static byte[] ToBytes(this Int256 value, bool? asLittleEndian = null, bool trimZeros = false)
        {
            var buffer = new byte[32];
            value.ToBytes(buffer, 0, asLittleEndian);
            
            if (trimZeros)
                buffer = buffer.TrimZeros(asLittleEndian);

            return buffer;
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="Int256" />.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="offset">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="Int256" /> value.</returns>
        public static Int256 ToInt256(this byte[] bytes, int offset = 0, bool? asLittleEndian = null)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytes.Length < offset)
            {
                throw new InvalidOperationException("Array length could not be less than offset.");
            }

            bool ale = GetIsLittleEndian(asLittleEndian);

            byte[] data;
            if (bytes.Length - offset < 32)
            {
                data = new byte[32];
                Buffer.BlockCopy(bytes, offset, data, ale ? 0 : 32 - (bytes.Length - offset), bytes.Length - offset);
            }
            else
            {
                data = bytes;
            }

            ulong a = data.ToUInt64(ale ? offset + 24 : offset, ale);
            ulong b = data.ToUInt64(ale ? offset + 16 : offset + 8, ale);
            ulong c = data.ToUInt64(ale ? offset + 8 : offset + 16, ale);
            ulong d = data.ToUInt64(ale ? offset : offset + 24, ale);

            return new Int256(a, b, c, d);
        }
        #endregion

        private static bool GetIsLittleEndian(bool? asLittleEndian)
        {
            return asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian;
        }
    }
}
