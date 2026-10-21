using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using RecipeBox.Shared;

namespace RecipeBox.Blazor.List;

public partial class RecipeList : ComponentBase, IDisposable
{
    [SupplyParameterFromQuery] public string? Search { get; set; }
    [SupplyParameterFromQuery] public string? Tag { get; set; }

    private List<RecipeSummary>? recipes;
    private bool loading = true;
    private bool error;
    private string searchText = "";
    private CancellationTokenSource? debounce;

    protected override async Task OnParametersSetAsync()
    {
        searchText = Search ?? "";
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loading = true;
        error = false;
        StateHasChanged();

        var query = $"/api/recipes?search={Uri.EscapeDataString(Search ?? "")}&tag={Uri.EscapeDataString(Tag ?? "")}";
        try
        {
            recipes = await Http.GetFromJsonAsync<List<RecipeSummary>>(query);
        }
        catch (HttpRequestException)
        {
            error = true;
        }
        finally
        {
            loading = false;
        }
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        searchText = e.Value?.ToString() ?? "";
        debounce?.Cancel();
        debounce = new CancellationTokenSource();
        _ = DebounceAsync(searchText, debounce.Token);
    }

    private async Task DebounceAsync(string value, CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Nav.NavigateTo(Nav.GetUriWithQueryParameter("search", string.IsNullOrEmpty(value) ? null : value), replace: true);
    }

    private void ClearFilters()
    {
        searchText = "";
        Nav.NavigateTo(Nav.BaseUri);
    }

    public void Dispose() => debounce?.Cancel();
}
