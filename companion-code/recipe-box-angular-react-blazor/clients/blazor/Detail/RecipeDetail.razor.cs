using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using RecipeBox.Shared;

namespace RecipeBox.Blazor.Detail;

public partial class RecipeDetail : ComponentBase
{
    [Parameter] public int Id { get; set; }

    private Recipe? recipe;
    private int servings;
    private bool loading = true;
    private bool notFound;
    private bool error;
    private bool confirming;
    private int loadedId = -1;

    protected override async Task OnParametersSetAsync()
    {
        if (Id == loadedId) return;
        loadedId = Id;
        confirming = false;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loading = true;
        notFound = false;
        error = false;
        StateHasChanged();

        try
        {
            recipe = await Http.GetFromJsonAsync<Recipe>($"/api/recipes/{Id}");
            servings = recipe?.Servings ?? 1;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            notFound = true;
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

    private async Task DeleteAsync()
    {
        await Http.DeleteAsync($"/api/recipes/{Id}");
        await Count.RefreshAsync();
        Nav.NavigateTo("/");
    }
}
