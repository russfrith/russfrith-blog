using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Shared.Tests;

public class RecipeValidatorTests
{
    private static Recipe ValidRecipe() => new()
    {
        Title = "Lemon Pancakes",
        Servings = 4,
        PrepMinutes = 10,
        CookMinutes = 15,
        Tags = ["breakfast"],
        Ingredients = [new Ingredient { Quantity = 1, Unit = "cup", Name = "flour" }],
        Steps = [new Step { Order = 1, Instruction = "Mix and cook." }],
    };

    [Fact]
    public void Valid_recipe_has_no_errors()
    {
        Assert.Empty(RecipeValidator.Validate(ValidRecipe()));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    public void Title_length_violations_are_keyed_title(string title)
    {
        var recipe = ValidRecipe();
        recipe.Title = title;
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void Too_many_tags_is_keyed_tags()
    {
        var recipe = ValidRecipe();
        recipe.Tags = Enumerable.Range(0, 9).Select(i => $"tag{i}").ToList();
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("tags", errors.Keys);
    }

    [Fact]
    public void Duplicate_normalized_tag_is_keyed_by_index()
    {
        var recipe = ValidRecipe();
        recipe.Tags = ["dinner", "dinner"];
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("tags[1]", errors.Keys);
        Assert.DoesNotContain("tags[0]", errors.Keys);
    }

    [Fact]
    public void Oversized_tag_is_keyed_by_index()
    {
        var recipe = ValidRecipe();
        recipe.Tags = [new string('a', 31)];
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("tags[0]", errors.Keys);
    }

    [Fact]
    public void No_ingredients_is_keyed_ingredients()
    {
        var recipe = ValidRecipe();
        recipe.Ingredients = [];
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("ingredients", errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Quantity_range_violation_is_keyed_by_index(decimal quantity)
    {
        var recipe = ValidRecipe();
        recipe.Ingredients[0].Quantity = quantity;
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("ingredients[0].quantity", errors.Keys);
    }

    [Fact]
    public void Empty_ingredient_name_is_keyed_by_index()
    {
        var recipe = ValidRecipe();
        recipe.Ingredients[0].Name = "";
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("ingredients[0].name", errors.Keys);
    }

    [Fact]
    public void No_steps_is_keyed_steps()
    {
        var recipe = ValidRecipe();
        recipe.Steps = [];
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("steps", errors.Keys);
    }

    [Fact]
    public void Empty_step_instruction_is_keyed_by_index()
    {
        var recipe = ValidRecipe();
        recipe.Steps[0].Instruction = "";
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("steps[0].instruction", errors.Keys);
    }

    [Fact]
    public void Multiple_errors_at_once_are_all_reported()
    {
        var recipe = ValidRecipe();
        recipe.Title = "";
        recipe.Steps = [];
        var errors = RecipeValidator.Validate(recipe);
        Assert.Contains("title", errors.Keys);
        Assert.Contains("steps", errors.Keys);
    }
}

public class RecipeNormalizerTests
{
    [Fact]
    public void Trims_strings_and_lowercases_tags()
    {
        var recipe = new Recipe
        {
            Title = "  Lemon Pancakes  ",
            Description = "  nice  ",
            Tags = ["  Breakfast  ", "QUICK"],
            Ingredients = [new Ingredient { Name = "  Flour  ", Unit = "  cup  " }],
            Steps = [new Step { Instruction = "  Mix.  " }],
        };

        RecipeNormalizer.Normalize(recipe);

        Assert.Equal("Lemon Pancakes", recipe.Title);
        Assert.Equal("nice", recipe.Description);
        Assert.Equal(["breakfast", "quick"], recipe.Tags);
        Assert.Equal("Flour", recipe.Ingredients[0].Name);
        Assert.Equal("cup", recipe.Ingredients[0].Unit);
        Assert.Equal("Mix.", recipe.Steps[0].Instruction);
    }

    [Fact]
    public void Blank_description_and_unit_become_null()
    {
        var recipe = new Recipe
        {
            Title = "Toast",
            Description = "   ",
            Ingredients = [new Ingredient { Name = "Bread", Unit = "  " }],
            Steps = [new Step { Instruction = "Toast it." }],
        };

        RecipeNormalizer.Normalize(recipe);

        Assert.Null(recipe.Description);
        Assert.Null(recipe.Ingredients[0].Unit);
    }

    [Fact]
    public void Step_order_is_rewritten_to_array_position()
    {
        var recipe = new Recipe
        {
            Title = "Toast",
            Steps = [new Step { Order = 99, Instruction = "a" }, new Step { Order = 1, Instruction = "b" }],
        };

        RecipeNormalizer.Normalize(recipe);

        Assert.Equal(1, recipe.Steps[0].Order);
        Assert.Equal(2, recipe.Steps[1].Order);
    }
}
