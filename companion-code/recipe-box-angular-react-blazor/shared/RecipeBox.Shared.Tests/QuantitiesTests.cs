using System.Text.Json;
using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Shared.Tests;

public class QuantitiesTests
{
    public static TheoryData<TestVector> Vectors()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test-vectors.json"));
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var vectors = JsonSerializer.Deserialize<List<TestVector>>(json, options)!;

        var data = new TheoryData<TestVector>();
        foreach (var vector in vectors)
            data.Add(vector);
        return data;
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void FormatQuantity_matches_shared_test_vectors(TestVector vector)
    {
        var scaled = Quantities.ScaledQuantity(vector.Quantity, vector.OriginalServings, vector.TargetServings);
        var formatted = Quantities.FormatQuantity(scaled);
        Assert.Equal(vector.Expected, formatted);
    }

    public record TestVector(string Description, decimal Quantity, int OriginalServings, int TargetServings, string Expected)
    {
        public override string ToString() => Description;
    }
}
