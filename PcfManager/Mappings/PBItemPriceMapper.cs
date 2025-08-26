using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PcfManager.Models;   // ItemPriceDto
using PcfManager.Services; // ItemPricePbDto

namespace PcfManager.Mappings
{
    public static class ItemPriceMapper
    {
        // ---------------------------
        // Option A: Explicit mapping
        // ---------------------------
        public static ItemPricePbDto ToPb(this ItemPriceDto s) => new ItemPricePbDto
        {
            // Different names
            ItemNum = s.Item,
            ItemDesc = s.Description,

            // Same-name properties (copied explicitly for compile-time safety)
            Family_Code = s.Family_Code,
            ListPrice = s.ListPrice,
            PP1Price = s.PP1Price,
            PP2Price = s.PP2Price,
            BM1Price = s.BM1Price,
            BM2Price = s.BM2Price,
            FOBPrice = s.FOBPrice,
            NewListPrice = s.NewListPrice,
            NewPP1Price = s.NewPP1Price,
            NewPP2Price = s.NewPP2Price,
            NewBM1Price = s.NewBM1Price,
            NewBM2Price = s.NewBM2Price,
            NewFOBPrice = s.NewFOBPrice
        };

        public static IEnumerable<ItemPricePbDto> ToPb(this IEnumerable<ItemPriceDto> src) =>
            src.Select(ToPb);

        // -----------------------------------------
        // Option B: Hybrid (reflection + overrides)
        // -----------------------------------------
        public static ItemPricePbDto ToPbHybrid(this ItemPriceDto s)
        {
            var t = new ItemPricePbDto();

            // 1) Copy same-name properties automatically
            CopySameNamedProps(s, t);

            // 2) Override the differently named properties
            t.ItemNum = s.Item;
            t.ItemDesc = s.Description;

            return t;
        }

        public static IEnumerable<ItemPricePbDto> ToPbHybrid(this IEnumerable<ItemPriceDto> src) =>
            src.Select(ToPbHybrid);

        // ---- Reflection helpers ----

        private static void CopySameNamedProps(object source, object target)
        {
            var sType = source.GetType();
            var tType = target.GetType();

            var sProps = sType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                              .Where(p => p.CanRead)
                              .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var tp in tType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite))
            {
                if (!sProps.TryGetValue(tp.Name, out var sp))
                    continue;

                var raw = sp.GetValue(source);
                var converted = ChangeType(raw, tp.PropertyType);
                tp.SetValue(target, converted);
            }
        }

        private static object? ChangeType(object? value, Type targetType)
        {
            if (value is null)
                return null;

            var nn = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Already assignable
            if (nn.IsInstanceOfType(value))
                return value;

            // Common conversions
            if (nn.IsEnum)
                return Enum.Parse(nn, value.ToString()!, true);
            if (nn == typeof(Guid))
                return Guid.Parse(value.ToString()!);
            if (nn == typeof(string))
                return value.ToString();
            if (nn == typeof(decimal))
                return Convert.ToDecimal(value);
            if (nn == typeof(double))
                return Convert.ToDouble(value);
            if (nn == typeof(float))
                return Convert.ToSingle(value);
            if (nn == typeof(long))
                return Convert.ToInt64(value);
            if (nn == typeof(int))
                return Convert.ToInt32(value);
            if (nn == typeof(short))
                return Convert.ToInt16(value);
            if (nn == typeof(bool))
            {
                var s = value.ToString()!;
                if (bool.TryParse(s, out var b))
                    return b;
                return s == "1" || s.Equals("y", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }

            return Convert.ChangeType(value, nn);
        }
    }
}
