using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace RecipeBox.Api.Tests;

public class ValidationTests : IDisposable
{
    private readonly RecipeBoxFactory _factory = new();
    private readonly HttpClient _client;

    public ValidationTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Explicit_null_for_a_numeric_field_is_a_parse_error_keyed_by_json_path()
    {
        var body = """{"title":"X","description":null,"servings":null,"prepMinutes":1,"cookMinutes":1,"tags":[],"ingredients":[{"quantity":1,"unit":null,"name":"a"}],"steps":[{"order":1,"instruction":"a"}]}""";

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("servings", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Non_numeric_value_for_a_numeric_field_is_a_parse_error_with_no_dollar_prefix()
    {
        var body = """{"title":"X","servings":"oops","prepMinutes":1,"cookMinutes":1,"tags":[],"ingredients":[{"quantity":1,"name":"a"}],"steps":[{"order":1,"instruction":"a"}]}""";

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("servings", problem!.Errors.Keys);
        Assert.DoesNotContain("$.servings", problem.Errors.Keys);
    }

    [Fact]
    public async Task Missing_numeric_property_defaults_to_zero_and_fails_field_validation()
    {
        // servings omitted entirely -> deserializes to 0 -> range violation, not a parse error.
        var body = """{"title":"X","prepMinutes":0,"cookMinutes":0,"tags":[],"ingredients":[{"quantity":1,"name":"a"}],"steps":[{"order":1,"instruction":"a"}]}""";

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("servings", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Missing_quantity_defaults_to_zero_and_fails_its_range_rule()
    {
        var body = """{"title":"X","servings":1,"prepMinutes":0,"cookMinutes":0,"tags":[],"ingredients":[{"name":"a"}],"steps":[{"order":1,"instruction":"a"}]}""";

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("ingredients[0].quantity", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Missing_prep_and_cook_minutes_default_to_zero_and_are_valid()
    {
        var body = """{"title":"Zero Prep And Cook","servings":1,"tags":[],"ingredients":[{"quantity":1,"name":"a"}],"steps":[{"order":1,"instruction":"a"}]}""";

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Missing_body_is_keyed_empty_string()
    {
        var response = await _client.PostAsync("/api/recipes", new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Nested_malformed_field_is_keyed_by_its_full_path()
    {
        var body = """{"title":"X","servings":1,"prepMinutes":0,"cookMinutes":0,"tags":[],"ingredients":[{"quantity":"not-a-number","name":"a"}],"steps":[{"order":1,"instruction":"a"}]}""";

        var response = await Post(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("ingredients[0].quantity", problem!.Errors.Keys);
    }

    private Task<HttpResponseMessage> Post(string json) =>
        _client.PostAsync("/api/recipes", new StringContent(json, Encoding.UTF8, "application/json"));
}
