// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace BigMath.Utils
{
    /// <summary>
    ///     Utils for the <see cref="Array" /> class.
    /// </summary>
    public static class ArrayUtils
    {
        /// <summary>
        ///     Converts an array of one type to an array of another type.
        /// </summary>
        /// <returns>
        ///     An array of the target type containing the converted elements from the source array.
        /// </returns>
        /// <param name="array">The one-dimensional, zero-based <see cref="T:System.Array" /> to convert to a target type.</param>
        /// <param name="convert">A <see cref="Func{TInput, TOutput}" /> that converts each element from one type to another type.</param>
        /// <typeparam name="TInput">The type of the elements of the source array.</typeparam>
        /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array" /> is null.-or-<paramref name="convert" /> is
        ///     null.
        /// </exception>
        public static TOutput[] ConvertAll<TInput, TOutput>(this TInput[] array, Func<TInput, TOutput> convert)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (convert == null)
            {
                throw new ArgumentNullException("convert");
            }
            var outputArray = new TOutput[array.Length];
            for (int index = 0; index < array.Length; ++index)
            {
                outputArray[index] = convert(array[index]);
            }
            return outputArray;
        }

        /// <summary>
        ///     Get length of serial non zero items.
        /// </summary>
        /// <param name="bytes">Array of bytes.</param>
        /// <param name="asLittleEndian">True - skip all zero items from high. False - skip all zero items from low.</param>
        /// <returns>Length of serial non zero items.</returns>
        public static int GetNonZeroLength(this byte[] bytes, bool? asLittleEndian = null)
        {
            bool ale = GetIsLittleEndian(asLittleEndian);

            if (ale)
            {
                int index = bytes.Length - 1;
                while ((index >= 0) && (bytes[index] == 0))
                {
                    index--;
                }
                index = index < 0 ? 0 : index;
                return index + 1;
            }
            else
            {
                int index = 0;
                while ((index < bytes.Length) && (bytes[index] == 0))
                {
                    index++;
                }
                index = index >= bytes.Length ? bytes.Length - 1 : index;
                return bytes.Length - index;
            }
        }

        /// <summary>
        ///     Trim zero items.
        /// </summary>
        /// <param name="bytes">Array of bytes.</param>
        /// <param name="asLittleEndian">True - trim from high, False - trim from low.</param>
        /// <returns>Trimmed array of bytes.</returns>
        public static byte[] TrimZeros(this byte[] bytes, bool? asLittleEndian = null)
        {
            bool ale = GetIsLittleEndian(asLittleEndian);

            int length = GetNonZeroLength(bytes, ale);

            var trimmed = new byte[length];
            Buffer.BlockCopy(bytes, ale ? 0 : bytes.Length - length, trimmed, 0, length);
            return trimmed;
        }

        private static bool GetIsLittleEndian(bool? asLittleEndian)
        {
            return asLittleEndian.HasValue ? asLittleEndian.Value : BitConverter.IsLittleEndian;
        }
    }
}
