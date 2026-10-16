using System.Net;
using System.Net.Http.Json;
using System.Text;
using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Api.Tests;

public class ResetTests
{
    [Fact]
    public async Task Reset_endpoint_404s_outside_testing()
    {
        using var factory = new RecipeBoxFactory(environment: "Production");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/test/reset", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reset_restores_the_seed_count_with_contiguous_ids_and_drops_extra_data()
    {
        using var factory = new RecipeBoxFactory(environment: "Testing");
        using var client = factory.CreateClient();

        var before = await client.GetFromJsonAsync<List<RecipeSummary>>("/api/recipes");
        var seedCount = before!.Count;

        var create = await client.PostAsync("/api/recipes",
            new StringContent(TestRecipes.ValidJson("Extra Recipe"), Encoding.UTF8, "application/json"));
        var extra = await create.Content.ReadFromJsonAsync<Recipe>();

        using (var content = new MultipartFormDataContent())
        {
            var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4]);
            content.Add(file, "file", "x.png");
            await client.PostAsync($"/api/recipes/{extra!.Id}/image", content);
        }

        var reset = await client.PostAsync("/api/test/reset", null);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var after = await client.GetFromJsonAsync<List<RecipeSummary>>("/api/recipes");
        Assert.Equal(seedCount, after!.Count);

        var ids = after.Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(Enumerable.Range(1, seedCount), ids);

        var knownSeedFiles = new[] { "lemon-pancakes.jpg", "veggie-stirfry.png", "choc-chip-cookies.webp", "tomato-soup.jpg", "banana-bread.png" };
        var remainingFiles = Directory.GetFiles(factory.ImageRoot).Select(Path.GetFileName);
        Assert.All(remainingFiles, name => Assert.Contains(name, knownSeedFiles));
    }
}
