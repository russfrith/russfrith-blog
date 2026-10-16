using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace RecipeBox.Api.Tests;

// Keeps api/openapi.json (the committed snapshot tools/typegen will read
// from in later phases) in sync with what the API actually serves.
// Set UPDATE_SNAPSHOTS=1 to rewrite the committed file instead of asserting.
public class OpenApiSnapshotTests : IDisposable
{
    private readonly RecipeBoxFactory _factory = new();
    private readonly HttpClient _client;

    public OpenApiSnapshotTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Committed_openapi_json_matches_what_the_api_serves()
    {
        var document = await _client.GetFromJsonAsync<JsonNode>("/openapi/v1.json");
        document!.AsObject().Remove("servers");
        var normalized = document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        var snapshotPath = FindApiRoot();

        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            await File.WriteAllTextAsync(snapshotPath, normalized + "\n");
            return;
        }

        var committed = await File.ReadAllTextAsync(snapshotPath);
        Assert.Equal(committed.Trim(), normalized.Trim());
    }

    private static string FindApiRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "api", "openapi.json")))
            dir = dir.Parent;

        return Path.Combine(dir!.FullName, "api", "openapi.json");
    }
}
