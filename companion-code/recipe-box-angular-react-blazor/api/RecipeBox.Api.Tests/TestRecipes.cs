namespace RecipeBox.Api.Tests;

public static class TestRecipes
{
    // A minimal valid payload matching RecipeValidator's rules, as a JSON string
    // so tests can target individual fields without fighting serialization.
    public static string ValidJson(string title = "Test Recipe") => $$"""
        {
          "title": "{{title}}",
          "description": "A recipe for testing.",
          "servings": 4,
          "prepMinutes": 10,
          "cookMinutes": 20,
          "tags": ["test"],
          "ingredients": [{ "quantity": 1, "unit": "cup", "name": "flour" }],
          "steps": [{ "order": 1, "instruction": "Mix and bake." }]
        }
        """;
}
