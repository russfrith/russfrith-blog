namespace RecipeBox.Api.Images;

// Uploads are always named {guid}.{ext}, never the uploaded filename.
// Seed images are a separate, fixed-filename path (see Seeding/Seeder.cs).
public class ImageStore(SeedPaths paths)
{
    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct)
    {
        Directory.CreateDirectory(paths.ImageRoot);
        var fileName = $"{Guid.NewGuid()}.{extension}";
        await using var file = File.Create(Path.Combine(paths.ImageRoot, fileName));
        await content.CopyToAsync(file, ct);
        return fileName;
    }

    public void Delete(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return;

        var path = Path.Combine(paths.ImageRoot, fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static string? FileNameFromUrl(string? imageUrl) =>
        imageUrl is null ? null : Path.GetFileName(imageUrl);
}
