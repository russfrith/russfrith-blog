using System.Net.Http.Json;
using RecipeBox.Shared;

namespace RecipeBox.Blazor;

// The Blazor "state container" for the header's live recipe count (spec.md
// Screens #4) - parallel to Angular's signal service and React's context.
public class RecipeCount(HttpClient http)
{
    public int? Count { get; private set; }
    public event Action? Changed;

    public async Task RefreshAsync()
    {
        try
        {
            var recipes = await http.GetFromJsonAsync<List<RecipeSummary>>("/api/recipes");
            Count = recipes?.Count;
        }
        catch (HttpRequestException)
        {
            // End-to-end test 4 forces every /api/recipes call to fail,
            // including this one - leave the count blank rather than
            // surfacing Blazor's global error UI for a header glitch.
            Count = null;
        }

        Changed?.Invoke();
    }
}
