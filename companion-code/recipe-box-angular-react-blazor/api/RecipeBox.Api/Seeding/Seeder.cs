using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RecipeBox.Api.Images;
using RecipeBox.Shared;

namespace RecipeBox.Api.Seeding;

// Seeds/reseeds from seed/recipes.json + seed/images/. Reset deletes every
// recipe and the sqlite_sequence counter so ids come back as a deterministic
// 1..N (see spec.md "Endpoints" -> POST /api/test/reset).
public static class Seeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task SeedAsync(RecipeDbContext db, SeedPaths paths, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(paths.SeedRoot, "recipes.json"), ct);
        var recipes = JsonSerializer.Deserialize<List<Recipe>>(json, JsonOptions)
            ?? throw new InvalidOperationException("seed/recipes.json did not deserialize to any recipes.");

        Directory.CreateDirectory(paths.ImageRoot);
        foreach (var existingFile in Directory.GetFiles(paths.ImageRoot))
            File.Delete(existingFile);

        foreach (var recipe in recipes)
        {
            recipe.Id = 0;
            if (ImageStore.FileNameFromUrl(recipe.ImageUrl) is { } fileName)
            {
                var source = Path.Combine(paths.SeedRoot, "images", fileName);
                File.Copy(source, Path.Combine(paths.ImageRoot, fileName), overwrite: true);
            }
        }

        db.Recipes.AddRange(recipes);
        await db.SaveChangesAsync(ct);
    }

    public static async Task ResetAsync(RecipeDbContext db, SeedPaths paths, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Recipes;", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name = 'Recipes';", ct);
        await SeedAsync(db, paths, ct);
    }
}
