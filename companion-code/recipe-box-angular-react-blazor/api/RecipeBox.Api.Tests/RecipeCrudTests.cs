using System.Net;
using System.Net.Http.Json;
using RecipeBox.Shared;
using Xunit;

namespace RecipeBox.Api.Tests;

public class RecipeCrudTests : IDisposable
{
    private readonly RecipeBoxFactory _factory = new();
    private readonly HttpClient _client;

    public RecipeCrudTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task List_returns_seeded_recipes_sorted_by_title()
    {
        var recipes = await _client.GetFromJsonAsync<List<RecipeSummary>>("/api/recipes");

        Assert.NotNull(recipes);
        Assert.True(recipes!.Count >= 9);
        Assert.Equal(recipes.OrderBy(r => r.Title, StringComparer.Ordinal).Select(r => r.Title), recipes.Select(r => r.Title));
    }

    [Fact]
    public async Task Get_by_id_404s_when_missing()
    {
        var response = await _client.GetAsync("/api/recipes/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_get_round_trips()
    {
        var create = await _client.PostAsync("/api/recipes",
            new StringContent(TestRecipes.ValidJson("Brand New Recipe"), System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);

        var created = await create.Content.ReadFromJsonAsync<Recipe>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Null(created.ImageUrl);
        Assert.Equal(created.CreatedAt, created.UpdatedAt);

        var fetched = await _client.GetFromJsonAsync<Recipe>($"/api/recipes/{created.Id}");
        Assert.Equal("Brand New Recipe", fetched!.Title);
    }

    [Fact]
    public async Task Put_replaces_ingredients_and_steps_wholesale()
    {
        var created = await CreateRecipe("Original Title");

        var replacement = $$"""
            {
              "title": "Original Title",
              "servings": 2,
              "prepMinutes": 1,
              "cookMinutes": 1,
              "tags": ["replaced"],
              "ingredients": [
                { "quantity": 2, "unit": "tbsp", "name": "sugar" },
                { "quantity": 3, "unit": "tbsp", "name": "butter" }
              ],
              "steps": [{ "order": 1, "instruction": "Only one step now." }]
            }
            """;

        var put = await _client.PutAsync($"/api/recipes/{created.Id}",
            new StringContent(replacement, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<Recipe>();

        Assert.Equal(2, updated!.Ingredients.Count);
        Assert.Single(updated.Steps);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task Put_with_unchanged_title_does_not_conflict_with_itself()
    {
        var created = await CreateRecipe("Keep My Own Title");

        var put = await _client.PutAsync($"/api/recipes/{created.Id}",
            new StringContent(TestRecipes.ValidJson("Keep My Own Title"), System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
    }

    [Fact]
    public async Task Put_colliding_with_a_different_recipes_title_400s()
    {
        await CreateRecipe("First Recipe Title");
        var second = await CreateRecipe("Second Recipe Title");

        var put = await _client.PutAsync($"/api/recipes/{second.Id}",
            new StringContent(TestRecipes.ValidJson("First Recipe Title"), System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        var problem = await put.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Contains("title", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_404s_when_missing()
    {
        var put = await _client.PutAsync("/api/recipes/999999",
            new StringContent(TestRecipes.ValidJson(), System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_recipe_then_404s()
    {
        var created = await CreateRecipe("Delete Me");

        var delete = await _client.DeleteAsync($"/api/recipes/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var getAfter = await _client.GetAsync($"/api/recipes/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
    }

    [Fact]
    public async Task Delete_404s_when_missing()
    {
        var delete = await _client.DeleteAsync("/api/recipes/999999");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Search_matches_title_and_ingredient_name_case_insensitively()
    {
        var byTitle = await _client.GetFromJsonAsync<List<RecipeSummary>>("/api/recipes?search=LEMON");
        Assert.Contains(byTitle!, r => r.Title == "Lemon Pancakes");

        var byIngredient = await _client.GetFromJsonAsync<List<RecipeSummary>>("/api/recipes?search=cocoa");
        Assert.Empty(byIngredient!);
    }

    [Fact]
    public async Task Tag_filter_returns_empty_for_unknown_tag_not_404()
    {
        var response = await _client.GetAsync("/api/recipes?tag=not-a-real-tag");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recipes = await response.Content.ReadFromJsonAsync<List<RecipeSummary>>();
        Assert.Empty(recipes!);
    }

    [Fact]
    public async Task Timestamps_round_trip_as_utc_with_trailing_z()
    {
        var create = await _client.PostAsync("/api/recipes",
            new StringContent(TestRecipes.ValidJson("Timestamp Check"), System.Text.Encoding.UTF8, "application/json"));
        var raw = await create.Content.ReadAsStringAsync();
        Assert.Contains("Z\"", raw);
    }

    private async Task<Recipe> CreateRecipe(string title)
    {
        var response = await _client.PostAsync("/api/recipes",
            new StringContent(TestRecipes.ValidJson(title), System.Text.Encoding.UTF8, "application/json"));
        return (await response.Content.ReadFromJsonAsync<Recipe>())!;
    }
}
