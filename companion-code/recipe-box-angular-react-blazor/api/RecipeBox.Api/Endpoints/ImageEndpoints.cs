using Microsoft.EntityFrameworkCore;
using RecipeBox.Api.Images;

namespace RecipeBox.Api.Endpoints;

public static class ImageEndpoints
{
    private const long MaxBytes = 5 * 1024 * 1024;

    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapPost("/api/recipes/{id:int}/image", async (int id, IFormFile? file, RecipeDbContext db, ImageStore store) =>
        {
            var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == id);
            if (recipe is null) return Results.NotFound();

            if (file is null || file.Length == 0)
                return Results.ValidationProblem(Error("A file is required."));

            if (file.Length > MaxBytes)
                return Results.ValidationProblem(Error("File must be at most 5 MB."));

            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header);
            var extension = ImageSignature.DetectExtension(header.AsSpan(0, read));
            if (extension is null)
                return Results.ValidationProblem(Error("File must be a JPEG, PNG, or WebP image."));

            stream.Position = 0;
            var oldFileName = ImageStore.FileNameFromUrl(recipe.ImageUrl);
            var savedFileName = await store.SaveAsync(stream, extension, default);
            store.Delete(oldFileName);

            recipe.ImageUrl = $"/api/images/{savedFileName}";
            recipe.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(recipe);
        })
        .DisableAntiforgery()
        .Produces<RecipeBox.Shared.Recipe>()
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        app.MapDelete("/api/recipes/{id:int}/image", async (int id, RecipeDbContext db, ImageStore store) =>
        {
            var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == id);
            if (recipe is null) return Results.NotFound();

            store.Delete(ImageStore.FileNameFromUrl(recipe.ImageUrl));
            recipe.ImageUrl = null;
            recipe.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }

    private static Dictionary<string, string[]> Error(string message) => new() { ["file"] = [message] };
}
