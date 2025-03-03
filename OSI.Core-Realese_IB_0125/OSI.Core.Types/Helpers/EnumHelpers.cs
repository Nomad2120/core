using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Helpers
{
    public static class EnumHelpers
    {
        public static string FlagsToString<TEnum>(this TEnum flags)
            where TEnum : struct, Enum
        {
            if (typeof(TEnum).GetCustomAttribute<FlagsAttribute>() == null) { throw new InvalidOperationException($"Enum {typeof(TEnum).FullName} is not marked as flags"); }
            var result = string.Empty;
            foreach (var value in Enum.GetValues<TEnum>())
            {

                if (IsFlag(value) && flags.HasFlag(value))
                {
                    result += ',' + value.ToString();
                }
            }
            return result[1..];
        }

        public static string[] FlagsToStrings<TEnum>(this TEnum flags)
            where TEnum : struct, Enum
        {
            if (typeof(TEnum).GetCustomAttribute<FlagsAttribute>() == null) { throw new InvalidOperationException($"Enum {typeof(TEnum).FullName} is not marked as flags"); }
            var result = new List<string>();
            foreach (var value in Enum.GetValues<TEnum>())
            {

                if (IsFlag(value) && flags.HasFlag(value))
                {
                    result.Add(value.ToString());
                }
            }
            return result.ToArray();
        }

        public static bool IsFlag<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            var log2 = Math.Log2(Convert.ToDouble(value));
            return double.IsFinite(log2) && log2 == Math.Ceiling(log2);
        }
    }
}
