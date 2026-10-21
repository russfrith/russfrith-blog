using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RecipeBox.Shared;

namespace RecipeBox.Blazor.Form;

public partial class RecipeForm : ComponentBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    [Parameter] public int? Id { get; set; }

    private Recipe? recipe;
    private EditContext? editContext;
    private RecipeValidation? validation;
    private IBrowserFile? selectedFile;
    private bool saving;
    private bool initialized;
    private int? loadedId;
    private int? createdId;

    private int? RecipeId => Id ?? createdId;

    protected override async Task OnParametersSetAsync()
    {
        if (initialized && Id == loadedId) return;
        initialized = true;
        loadedId = Id;
        createdId = null;
        selectedFile = null;
        editContext = null;

        recipe = Id is int id ? await Http.GetFromJsonAsync<Recipe>($"/api/recipes/{id}") : NewRecipe();
        editContext = new EditContext(recipe!);
    }

    private static Recipe NewRecipe() => new()
    {
        Servings = 4,
        Ingredients = [new Ingredient()],
        Steps = [new Step()],
    };

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        if (e.File.Size > MaxImageBytes)
        {
            selectedFile = null;
            validation?.ApplyServerErrors(new() { ["file"] = ["File must be at most 5 MB."] });
            return;
        }

        selectedFile = e.File;
    }

    private async Task SubmitAsync()
    {
        if (editContext is null || !editContext.Validate()) return;

        saving = true;
        try
        {
            var response = RecipeId is int id
                ? await Http.PutAsJsonAsync($"/api/recipes/{id}", recipe)
                : await Http.PostAsJsonAsync("/api/recipes", recipe);
            if (!await HandleResponseAsync(response)) return;

            var saved = (await response.Content.ReadFromJsonAsync<Recipe>())!;
            createdId = saved.Id;

            if (selectedFile is not null)
            {
                var uploadResponse = await UploadImageAsync(saved.Id);
                if (!await HandleResponseAsync(uploadResponse)) return;
            }

            await Count.RefreshAsync();
            Nav.NavigateTo($"/recipes/{saved.Id}");
        }
        finally
        {
            saving = false;
        }
    }

    private async Task<bool> HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return true;

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
            validation?.ApplyServerErrors(problem?.Errors ?? []);
        }

        return false;
    }

    private async Task<HttpResponseMessage> UploadImageAsync(int id)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(selectedFile!.OpenReadStream(MaxImageBytes));
        if (!string.IsNullOrEmpty(selectedFile.ContentType))
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(selectedFile.ContentType);
        content.Add(fileContent, "file", selectedFile.Name);
        return await Http.PostAsync($"/api/recipes/{id}/image", content);
    }
}
