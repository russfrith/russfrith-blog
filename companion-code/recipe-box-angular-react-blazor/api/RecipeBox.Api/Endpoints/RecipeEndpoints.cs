using Microsoft.EntityFrameworkCore;
using RecipeBox.Api.Images;
using RecipeBox.Shared;

namespace RecipeBox.Api.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recipes");

        group.MapGet("", async (string? search, string? tag, RecipeDbContext db) =>
        {
            IEnumerable<Recipe> recipes = await db.Recipes.ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                recipes = recipes.Where(r =>
                    r.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    r.Ingredients.Any(i => i.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var normalized = tag.Trim().ToLowerInvariant();
                recipes = recipes.Where(r => r.Tags.Contains(normalized));
            }

            var ordered = recipes.OrderBy(r => r.Title, StringComparer.Ordinal).Select(ToSummary).ToList();
            return Results.Ok(ordered);
        }).Produces<List<RecipeSummary>>();

        group.MapGet("/{id:int}", async (int id, RecipeDbContext db) =>
        {
            var recipe = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            return recipe is null ? Results.NotFound() : Results.Ok(recipe);
        }).Produces<Recipe>().Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (HttpContext ctx, RecipeDbContext db) =>
        {
            var (input, error) = await JsonBody.ReadAsync<Recipe>(ctx);
            if (error is not null) return error;

            RecipeNormalizer.Normalize(input!);
            var errors = RecipeValidator.Validate(input!);
            await AddTitleUniquenessError(errors, db, input!.Title, excludeId: null);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            input.Id = 0;
            input.ImageUrl = null;
            input.CreatedAt = DateTime.UtcNow;
            input.UpdatedAt = input.CreatedAt;

            db.Recipes.Add(input);
            await db.SaveChangesAsync();

            return Results.Created($"/api/recipes/{input.Id}", input);
        }).Produces<Recipe>(StatusCodes.Status201Created).ProducesValidationProblem();

        group.MapPut("/{id:int}", async (int id, HttpContext ctx, RecipeDbContext db) =>
        {
            // Tracked, not AsNoTracking: the owned Ingredients/Steps JSON
            // collections need a tracked owner to replace wholesale.
            // Update()-ing a brand-new detached graph throws on their
            // synthesized ordinal keys (see blog-notes.md).
            var existing = await db.Recipes.FirstOrDefaultAsync(r => r.Id == id);
            if (existing is null) return Results.NotFound();

            var (input, error) = await JsonBody.ReadAsync<Recipe>(ctx);
            if (error is not null) return error;

            RecipeNormalizer.Normalize(input!);
            var errors = RecipeValidator.Validate(input!);
            await AddTitleUniquenessError(errors, db, input!.Title, excludeId: id);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            existing.Title = input.Title;
            existing.Description = input.Description;
            existing.Servings = input.Servings;
            existing.PrepMinutes = input.PrepMinutes;
            existing.CookMinutes = input.CookMinutes;
            existing.Tags = input.Tags;
            existing.Ingredients = input.Ingredients;
            existing.Steps = input.Steps;
            existing.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(existing);
        }).Produces<Recipe>().Produces(StatusCodes.Status404NotFound).ProducesValidationProblem();

        group.MapDelete("/{id:int}", async (int id, RecipeDbContext db, ImageStore images) =>
        {
            var recipe = await db.Recipes.FindAsync(id);
            if (recipe is null) return Results.NotFound();

            images.Delete(ImageStore.FileNameFromUrl(recipe.ImageUrl));
            db.Recipes.Remove(recipe);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }

    private static RecipeSummary ToSummary(Recipe r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        Tags = r.Tags,
        Servings = r.Servings,
        PrepMinutes = r.PrepMinutes,
        CookMinutes = r.CookMinutes,
        ImageUrl = r.ImageUrl,
    };

    private static async Task AddTitleUniquenessError(
        Dictionary<string, string[]> errors, RecipeDbContext db, string title, int? excludeId)
    {
        var normalized = title.Trim().ToLowerInvariant();
        var exists = await db.Recipes.AnyAsync(r =>
            r.Title.ToLower() == normalized && r.Id != (excludeId ?? -1));
        if (!exists) return;

        const string message = "A recipe with this title already exists.";
        errors["title"] = errors.TryGetValue("title", out var existing) ? [.. existing, message] : [message];
    }
}
