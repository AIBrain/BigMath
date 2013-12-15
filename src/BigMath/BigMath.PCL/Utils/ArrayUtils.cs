// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace BigMath.Utils
{
    /// <summary>
    /// Utils for the <see cref="Array"/> class.
    /// </summary>
    internal static class ArrayUtils
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
    }
}