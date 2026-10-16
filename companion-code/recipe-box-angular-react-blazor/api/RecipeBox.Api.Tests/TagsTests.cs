using System.Net.Http.Json;
using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Api.Tests;

public class TagsTests : IDisposable
{
    private readonly RecipeBoxFactory _factory = new();
    private readonly HttpClient _client;

    public TagsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Tags_are_sorted_by_name_with_counts()
    {
        var tags = await _client.GetFromJsonAsync<List<TagCount>>("/api/tags");

        Assert.NotNull(tags);
        Assert.NotEmpty(tags!);
        Assert.Equal(tags.OrderBy(t => t.Name, StringComparer.Ordinal).Select(t => t.Name), tags.Select(t => t.Name));
        Assert.All(tags, t => Assert.True(t.Count > 0));
    }
}
