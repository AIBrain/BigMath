// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtils.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text;

namespace BigMath.Utils
{
    internal static class StringUtils
    {
        public static string ToHexaString(ulong[] values, bool caps, int min)
        {
            var sb = new StringBuilder();
            string x = (caps ? "X" : "x") + 16;

            for (int i = values.Length - 1; i >= 0; i--)
            {
                sb.Append(values[i].ToString(x));
            }

            string value = sb.ToString().TrimStart('0');

            int dif = min - value.Length;
            if (dif > 0)
            {
                value = new string('0', min - value.Length) + value;
            }

            return value;
        }
    }
}
