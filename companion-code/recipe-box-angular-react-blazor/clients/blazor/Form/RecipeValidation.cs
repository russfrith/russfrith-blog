using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RecipeBox.Shared;

namespace RecipeBox.Blazor.Form;

// Validates a clone of the bound Recipe - RecipeNormalizer.Normalize()
// reassigns Tags to a new list, so normalizing the live model would alias
// away from what TagInput holds a reference to. Client and server errors
// both land in one ValidationMessageStore under the server's own literal
// field-path keys (e.g. "ingredients[0].quantity") - no bracket-to-dot-path
// translation needed, since EditContext.Field(string) takes any string.
public class RecipeValidation : ComponentBase
{
    private static readonly HashSet<string> CollectionKeys = ["tags", "ingredients", "steps"];

    [CascadingParameter] public EditContext EditContext { get; set; } = default!;

    private ValidationMessageStore messages = default!;
    private readonly HashSet<string> touched = [];
    private bool submitted;

    protected override void OnInitialized()
    {
        messages = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += (_, _) => { submitted = true; Validate(); };
        EditContext.OnFieldChanged += (_, e) => { touched.Add(ToKey(e.FieldIdentifier)); Validate(); };
    }

    private void Validate()
    {
        var recipe = (Recipe)EditContext.Model;
        var copy = JsonSerializer.Deserialize<Recipe>(JsonSerializer.Serialize(recipe))!;
        RecipeNormalizer.Normalize(copy);
        var errors = RecipeValidator.Validate(copy);
        Show(errors.Where(e => submitted || touched.Contains(e.Key) ||
                                (CollectionKeys.Contains(e.Key) && touched.Count > 0)));
    }

    public void ApplyServerErrors(Dictionary<string, string[]> errors) => Show(errors);

    private void Show(IEnumerable<KeyValuePair<string, string[]>> errors)
    {
        messages.Clear();
        foreach (var (key, list) in errors) messages.Add(EditContext.Field(key), list);
        EditContext.NotifyValidationStateChanged();
    }

    private string ToKey(FieldIdentifier field)
    {
        var name = JsonNamingPolicy.CamelCase.ConvertName(field.FieldName);
        var recipe = (Recipe)EditContext.Model;
        return field.Model switch
        {
            Ingredient i => $"ingredients[{recipe.Ingredients.IndexOf(i)}].{name}",
            Step s => $"steps[{recipe.Steps.IndexOf(s)}].instruction",
            _ => name,
        };
    }
}
