namespace RecipeBox.Shared;

// scaledQuantity/formatQuantity from spec.md's "Shared logic" section.
// The TypeScript ports (Angular, React) must follow this exact algorithm:
// round to 4 decimal places (away from zero) before snapping to a
// fraction. shared/test-vectors.json is the cross-language contract.
public static class Quantities
{
    private static readonly decimal[] FractionValues =
        [0m, 0.125m, 0.25m, 1m / 3m, 0.375m, 0.5m, 0.625m, 2m / 3m, 0.75m, 0.875m, 1m];

    private static readonly string[] FractionLabels =
        ["", "1/8", "1/4", "1/3", "3/8", "1/2", "5/8", "2/3", "3/4", "7/8", ""];

    public static decimal ScaledQuantity(decimal quantity, int originalServings, int targetServings) =>
        quantity * targetServings / originalServings;

    public static string FormatQuantity(decimal value)
    {
        var rounded = Math.Round(value, 4, MidpointRounding.AwayFromZero);
        return rounded < 10m ? FormatUnderTen(rounded) : FormatTenOrMore(rounded);
    }

    private static string FormatUnderTen(decimal rounded)
    {
        var whole = Math.Floor(rounded);
        var fractional = rounded - whole;

        var bestIndex = 0;
        var bestDistance = decimal.MaxValue;
        for (var i = 0; i < FractionValues.Length; i++)
        {
            var distance = Math.Abs(FractionValues[i] - fractional);
            if (distance < bestDistance || (distance == bestDistance && FractionValues[i] > FractionValues[bestIndex]))
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex == FractionValues.Length - 1)
        {
            whole += 1;
            bestIndex = 0;
        }
        else if (bestIndex == 0 && whole == 0m)
        {
            bestIndex = 1; // the 1/8 floor: never show a bare "0"
        }

        return Combine(whole, FractionLabels[bestIndex]);
    }

    private static string FormatTenOrMore(decimal rounded)
    {
        var halves = Math.Round(rounded * 2m, MidpointRounding.AwayFromZero);
        var whole = Math.Floor(halves / 2m);
        var hasHalf = halves % 2m != 0m;
        return Combine(whole, hasHalf ? "1/2" : "");
    }

    private static string Combine(decimal whole, string fraction)
    {
        if (fraction == "")
            return whole.ToString("0");
        return whole == 0m ? fraction : $"{whole:0} {fraction}";
    }
}
