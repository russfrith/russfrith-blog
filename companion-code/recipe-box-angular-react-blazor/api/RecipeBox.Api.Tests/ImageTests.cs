using System.Net;
using System.Net.Http.Json;
using System.Text;
using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Api.Tests;

public class ImageTests : IDisposable
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly byte[] PngHeader =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly RecipeBoxFactory _factory = new();
    private readonly HttpClient _client;

    public ImageTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Valid_png_is_accepted_and_served_under_api_images()
    {
        var recipe = await CreateRecipe();

        var response = await Upload(recipe.Id, PngBytes(1024));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<Recipe>();
        Assert.StartsWith("/api/images/", updated!.ImageUrl);
        Assert.EndsWith(".png", updated.ImageUrl);
    }

    [Fact]
    public async Task Wrong_signature_is_rejected_keyed_file()
    {
        var recipe = await CreateRecipe();

        var response = await Upload(recipe.Id, Encoding.UTF8.GetBytes("this is not an image"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("file", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Exactly_five_megabytes_is_accepted()
    {
        var recipe = await CreateRecipe();
        var response = await Upload(recipe.Id, PngBytes(MaxBytes));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Five_megabytes_plus_one_byte_is_rejected_keyed_file()
    {
        var recipe = await CreateRecipe();
        var response = await Upload(recipe.Id, PngBytes(MaxBytes + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("file", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Upload_to_missing_recipe_404s()
    {
        var response = await Upload(999999, PngBytes(1024));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Replacing_an_image_deletes_the_old_file()
    {
        var recipe = await CreateRecipe();
        var first = await (await Upload(recipe.Id, PngBytes(1024))).Content.ReadFromJsonAsync<Recipe>();
        var oldPath = Path.Combine(_factory.ImageRoot, Path.GetFileName(first!.ImageUrl!));
        Assert.True(File.Exists(oldPath));

        await Upload(recipe.Id, PngBytes(2048));

        Assert.False(File.Exists(oldPath));
    }

    [Fact]
    public async Task Delete_image_404s_when_recipe_missing()
    {
        var response = await _client.DeleteAsync("/api/recipes/999999/image");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_image_is_idempotent_204_with_or_without_an_image()
    {
        var recipe = await CreateRecipe();

        var deleteWithNoImage = await _client.DeleteAsync($"/api/recipes/{recipe.Id}/image");
        Assert.Equal(HttpStatusCode.NoContent, deleteWithNoImage.StatusCode);

        await Upload(recipe.Id, PngBytes(1024));
        var deleteWithImage = await _client.DeleteAsync($"/api/recipes/{recipe.Id}/image");
        Assert.Equal(HttpStatusCode.NoContent, deleteWithImage.StatusCode);

        var fetched = await _client.GetFromJsonAsync<Recipe>($"/api/recipes/{recipe.Id}");
        Assert.Null(fetched!.ImageUrl);
    }

    private async Task<Recipe> CreateRecipe()
    {
        var response = await _client.PostAsync("/api/recipes",
            new StringContent(TestRecipes.ValidJson($"Image Test {Guid.NewGuid()}"), Encoding.UTF8, "application/json"));
        return (await response.Content.ReadFromJsonAsync<Recipe>())!;
    }

    private async Task<HttpResponseMessage> Upload(int recipeId, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "upload.png");
        return await _client.PostAsync($"/api/recipes/{recipeId}/image", content);
    }

    private static byte[] PngBytes(long totalLength)
    {
        var bytes = new byte[totalLength];
        PngHeader.CopyTo(bytes, 0);
        return bytes;
    }
}
