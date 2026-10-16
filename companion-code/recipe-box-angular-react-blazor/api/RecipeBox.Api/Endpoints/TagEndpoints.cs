using Microsoft.EntityFrameworkCore;
using RecipeBox.Shared;

namespace RecipeBox.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tags", async (RecipeDbContext db) =>
        {
            var allTags = await db.Recipes.Select(r => r.Tags).ToListAsync();

            var counts = allTags
                .SelectMany(tags => tags)
                .GroupBy(t => t)
                .Select(g => new TagCount { Name = g.Key, Count = g.Count() })
                .OrderBy(t => t.Name, StringComparer.Ordinal);

            return Results.Ok(counts);
        }).Produces<List<TagCount>>();
    }
}
