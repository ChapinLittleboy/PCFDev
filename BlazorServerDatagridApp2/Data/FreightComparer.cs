namespace BlazorServerDatagridApp2.Data;

using System;
using System.Globalization;
using System.Text.RegularExpressions;

public class FreightComparer
{
    public static int ConvertToInt(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        // Remove dollar signs and commas
        input = input.Replace("$", "").Replace(",", "").Trim();

        // Check if it ends with 'K' or 'k'
        bool hasK = input.EndsWith("K", StringComparison.OrdinalIgnoreCase);
        if (hasK)
            input = input.Substring(0, input.Length - 1);  // Remove the 'K'

        // Try parsing as decimal to handle cases like "12.5"
        if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            return 0;

        if (hasK)
            value *= 1000;

        // Return full dollars
        return (int)Math.Round(value);
    }

    public static bool AreFreightMinimumsEqual(string input1, string input2)
    {
        int val1 = ConvertToInt(input1);
        int val2 = ConvertToInt(input2);
        return val1 == val2;
    }

    public static bool AreFreightTermsEqual(string promoFrtTerms, string standardFrtTerms, bool MinimumsMatch)
    {
        // Compares Freight Terms to customer standard terms along with minimums
        if (MinimumsMatch)
    return string.Equals(promoFrtTerms, standardFrtTerms, StringComparison.OrdinalIgnoreCase);
return false;


    }
}

