namespace RecipeBox.Shared;

public class Ingredient
{
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = "";
}
