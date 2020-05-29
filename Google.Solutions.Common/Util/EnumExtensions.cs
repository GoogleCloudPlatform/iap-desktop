using System;
using System.Runtime.Remoting.Lifetime;

namespace Google.Solutions.Common.Util
{
    public static class EnumExtensions
    {
        private static bool IsPowerOfTwo(int v)
        {
            return v != 0 && (v & (v - 1)) == 0;
        }

        public static bool IsSingleFlag<TEnum>(this TEnum enumValue) 
            where TEnum : Enum
        {
            return IsPowerOfTwo((int)(object)enumValue);
        }

        public static bool IsFlagCombination<TEnum>(this TEnum enumValue)
            where TEnum : Enum
        {
            var v = (int)(object)enumValue;
            return v != 0 && !IsPowerOfTwo(v);
        }
    }
}
