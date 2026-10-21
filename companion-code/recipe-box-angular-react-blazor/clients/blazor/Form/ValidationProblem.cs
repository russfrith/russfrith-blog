namespace RecipeBox.Blazor.Form;

// Substitutes for HttpValidationProblemDetails, which isn't available in a
// WASM client project.
public record ValidationProblem(Dictionary<string, string[]> Errors);
