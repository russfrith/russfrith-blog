using RecipeBox.Api.Seeding;

namespace RecipeBox.Api.Endpoints;

public static class TestEndpoints
{
    // Only mapped when ASPNETCORE_ENVIRONMENT=Testing, so a reader's plain
    // `docker compose up` never exposes this. See spec.md's reset endpoint.
    public static void MapTestEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Testing"))
            return;

        app.MapPost("/api/test/reset", async (RecipeDbContext db, SeedPaths paths) =>
        {
            await Seeder.ResetAsync(db, paths, default);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);
    }
}
