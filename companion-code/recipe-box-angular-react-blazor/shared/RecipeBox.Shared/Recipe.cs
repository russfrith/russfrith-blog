namespace RecipeBox.Shared;

public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int Servings { get; set; }
    public int PrepMinutes { get; set; }
    public int CookMinutes { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<Ingredient> Ingredients { get; set; } = [];
    public List<Step> Steps { get; set; } = [];
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
