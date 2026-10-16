namespace RecipeBox.Shared;

// Runs before RecipeValidator so validation sees trimmed, lowercased,
// order-corrected data. See spec.md "Validation errors" and the Step/Tag rules.
public static class RecipeNormalizer
{
    public static void Normalize(Recipe recipe)
    {
        recipe.Title = recipe.Title.Trim();
        recipe.Description = NullIfBlank(recipe.Description);
        recipe.Tags = recipe.Tags.Select(t => t.Trim().ToLowerInvariant()).ToList();

        foreach (var ingredient in recipe.Ingredients)
        {
            ingredient.Name = ingredient.Name.Trim();
            ingredient.Unit = NullIfBlank(ingredient.Unit);
        }

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            recipe.Steps[i].Instruction = recipe.Steps[i].Instruction.Trim();
            recipe.Steps[i].Order = i + 1;
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
