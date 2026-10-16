namespace RecipeBox.Shared;

public class RecipeSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = [];
    public int Servings { get; set; }
    public int PrepMinutes { get; set; }
    public int CookMinutes { get; set; }
    public string? ImageUrl { get; set; }
}
