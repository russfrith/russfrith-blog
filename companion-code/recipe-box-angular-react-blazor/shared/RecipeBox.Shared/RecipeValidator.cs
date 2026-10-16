namespace RecipeBox.Shared;

// Field-level and collection-level rules from spec.md's Models/Validation
// errors sections. Produces the exact camelCase, 0-based error-path keys
// the spec and e2e suite depend on. Call RecipeNormalizer.Normalize first.
// Title uniqueness is a server-only rule checked in the API, not here,
// since it needs the database.
public static class RecipeValidator
{
    public static Dictionary<string, string[]> Validate(Recipe recipe)
    {
        var errors = new Dictionary<string, List<string>>();
        void Fail(string key, string message) =>
            (errors.TryGetValue(key, out var list) ? list : errors[key] = []).Add(message);

        if (recipe.Title.Length is < 3 or > 100)
            Fail("title", "Title must be 3 to 100 characters.");

        if (recipe.Description is { Length: > 500 })
            Fail("description", "Description must be at most 500 characters.");

        if (recipe.Servings is < 1 or > 48)
            Fail("servings", "Servings must be between 1 and 48.");

        if (recipe.PrepMinutes is < 0 or > 1440)
            Fail("prepMinutes", "PrepMinutes must be between 0 and 1440.");

        if (recipe.CookMinutes is < 0 or > 1440)
            Fail("cookMinutes", "CookMinutes must be between 0 and 1440.");

        ValidateTags(recipe.Tags, Fail);
        ValidateIngredients(recipe.Ingredients, Fail);
        ValidateSteps(recipe.Steps, Fail);

        return errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    private static void ValidateTags(List<string> tags, Action<string, string> fail)
    {
        if (tags.Count > 8)
            fail("tags", "At most 8 tags are allowed.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < tags.Count; i++)
        {
            if (tags[i].Length is < 1 or > 30)
                fail($"tags[{i}]", "Each tag must be 1 to 30 characters.");
            else if (!seen.Add(tags[i]))
                fail($"tags[{i}]", "Tags must be unique.");
        }
    }

    private static void ValidateIngredients(List<Ingredient> ingredients, Action<string, string> fail)
    {
        if (ingredients.Count is < 1 or > 50)
            fail("ingredients", "At least 1 and at most 50 ingredients are required.");

        for (var i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredients[i];
            if (ingredient.Quantity <= 0 || ingredient.Quantity > 10000)
                fail($"ingredients[{i}].quantity", "Quantity must be greater than 0 and at most 10000.");

            if (ingredient.Unit is { Length: > 20 })
                fail($"ingredients[{i}].unit", "Unit must be at most 20 characters.");

            if (ingredient.Name.Length is < 1 or > 100)
                fail($"ingredients[{i}].name", "Name must be 1 to 100 characters.");
        }
    }

    private static void ValidateSteps(List<Step> steps, Action<string, string> fail)
    {
        if (steps.Count is < 1 or > 50)
            fail("steps", "At least 1 and at most 50 steps are required.");

        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Instruction.Length is < 1 or > 1000)
                fail($"steps[{i}].instruction", "Instruction must be 1 to 1000 characters.");
        }
    }
}
