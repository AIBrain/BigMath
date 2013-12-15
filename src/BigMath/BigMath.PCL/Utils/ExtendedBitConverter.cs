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

        /// <summary>
        ///     Converts <see cref="int" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this int value, bool asLittleEndian)
        {
            return unchecked ((uint) value).ToBytes(asLittleEndian);
        }

        /// <summary>
        ///     Converts <see cref="uint" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this uint value, bool? asLittleEndian = null)
        {
            var bytes = new byte[4];
            if (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
            {
                bytes[0] = (byte) value;
                bytes[1] = (byte) (value >> 8);
                bytes[2] = (byte) (value >> 16);
                bytes[3] = (byte) (value >> 24);
            }
            else
            {
                bytes[0] = (byte) (value >> 24);
                bytes[1] = (byte) (value >> 16);
                bytes[2] = (byte) (value >> 8);
                bytes[3] = (byte) value;
            }
            return bytes;
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="int" />.
        /// </summary>
        /// <param name="bytes">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="int" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(this byte[] bytes, int startIndex, bool? asLittleEndian = null)
        {
            if (bytes.Length - startIndex < 4)
            {
                throw new ArgumentOutOfRangeException("bytes",
                    string.Format("Length of bytes array minus offset must NOT be less than 4, actual is {0}.", bytes.Length - startIndex));
            }

            var buffer = new byte[4];
            Buffer.BlockCopy(bytes, startIndex, buffer, 0, buffer.Length);

            return (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
                ? buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24
                : buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }

        /// <summary>
        ///     Converts <see cref="long" /> to array of bytes.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="asLittleEndian">Convert to little endian.</param>
        /// <returns>Array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToBytes(this long value, bool? asLittleEndian = null)
        {
            var bytes = new byte[8];
            if (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
            {
                bytes[0] = (byte) value;
                bytes[1] = (byte) (value >> 8);
                bytes[2] = (byte) (value >> 16);
                bytes[3] = (byte) (value >> 24);
                bytes[4] = (byte) (value >> 32);
                bytes[5] = (byte) (value >> 40);
                bytes[6] = (byte) (value >> 48);
                bytes[7] = (byte) (value >> 56);
            }
            else
            {
                bytes[0] = (byte) (value >> 56);
                bytes[1] = (byte) (value >> 48);
                bytes[2] = (byte) (value >> 40);
                bytes[3] = (byte) (value >> 32);
                bytes[4] = (byte) (value >> 24);
                bytes[5] = (byte) (value >> 16);
                bytes[6] = (byte) (value >> 8);
                bytes[7] = (byte) value;
            }
            return bytes;
        }

        /// <summary>
        ///     Converts array of bytes to <see cref="long" />.
        /// </summary>
        /// <param name="bytes">An array of bytes. </param>
        /// <param name="startIndex">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="long" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(this byte[] bytes, int startIndex, bool? asLittleEndian = null)
        {
            if (bytes.Length - startIndex < 8)
            {
                throw new ArgumentOutOfRangeException("bytes",
                    string.Format("Length of bytes array minus offset must NOT be less than 8, actual is {0}.", bytes.Length - startIndex));
            }
            var buffer = new byte[8];
            Buffer.BlockCopy(bytes, startIndex, buffer, 0, buffer.Length);

            return (asLittleEndian.HasValue ? asLittleEndian.Value : IsLittleEndian)
                ? buffer[0] | (long) buffer[1] << 8 | (long) buffer[2] << 16 | (long) buffer[3] << 24 | (long) buffer[4] << 32 | (long) buffer[5] << 40 | (long) buffer[6] << 48 |
                    (long) buffer[7] << 56
                : (long) buffer[0] << 56 | (long) buffer[1] << 48 | (long) buffer[2] << 40 | (long) buffer[3] << 32 | (long) buffer[4] << 24 | (long) buffer[5] << 16 |
                    (long) buffer[6] << 8 | buffer[7];
        }

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
        ///     Converts array of bytes to <see cref="ulong" />.
        /// </summary>
        /// <param name="bytes">An array of bytes. </param>
        /// <param name="startIndex">The starting position within <paramref name="bytes" />.</param>
        /// <param name="asLittleEndian">Convert from little endian.</param>
        /// <returns><see cref="ulong" /> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(this byte[] bytes, int startIndex, bool? asLittleEndian = null)
        {
            return (ulong) bytes.ToInt64(startIndex, asLittleEndian);
        }
    }
}
