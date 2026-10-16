using System.Text.Json;
using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Api.Tests;

public class SeedDataTests
{
    private static List<Recipe> LoadSeedRecipes()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "seed", "recipes.json")))
            dir = dir.Parent;

        var seedRoot = Path.Combine(dir!.FullName, "seed");
        var json = File.ReadAllText(Path.Combine(seedRoot, "recipes.json"));
        return JsonSerializer.Deserialize<List<Recipe>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    [Fact]
    public void Every_seed_recipe_passes_validation()
    {
        foreach (var recipe in LoadSeedRecipes())
            Assert.Empty(RecipeValidator.Validate(recipe));
    }

    [Fact]
    public void Exactly_one_seed_recipe_has_no_image()
    {
        var withoutImage = LoadSeedRecipes().Count(r => r.ImageUrl is null);
        Assert.Equal(1, withoutImage);
    }

    [Fact]
    public void A_seed_recipe_has_servings_three_and_a_one_third_cup_ingredient()
    {
        var match = LoadSeedRecipes().Any(r =>
            r.Servings == 3 && r.Ingredients.Any(i => i.Quantity == 0.3333m));

        Assert.True(match, "Expected a seed recipe with Servings=3 and a 0.3333 ingredient (end-to-end test 6's scale-by-3 case).");
    }

    [Fact]
    public void All_three_image_types_are_represented()
    {
        var extensions = LoadSeedRecipes()
            .Where(r => r.ImageUrl is not null)
            .Select(r => Path.GetExtension(r.ImageUrl).ToLowerInvariant())
            .ToHashSet();

        Assert.Contains(".jpg", extensions);
        Assert.Contains(".png", extensions);
        Assert.Contains(".webp", extensions);
    }
}
