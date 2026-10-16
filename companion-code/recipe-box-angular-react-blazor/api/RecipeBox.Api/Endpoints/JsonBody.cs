using System.Text.Json;

namespace RecipeBox.Api.Endpoints;

// Reads and deserializes the request body by hand (instead of relying on
// minimal API's automatic parameter binding) so a malformed body produces
// exactly the key the spec requires: the JSON path with no "$." prefix, or
// "" for a missing/unparseable body. See spec.md "Validation errors".
public static class JsonBody
{
    public static async Task<(T? Value, IResult? Error)> ReadAsync<T>(HttpContext context)
    {
        if (context.Request.ContentLength is null or 0)
            return (default, Results.ValidationProblem(Fail("", "A request body is required.")));

        try
        {
            var value = await context.Request.ReadFromJsonAsync<T>(context.RequestAborted);
            return value is null
                ? (default, Results.ValidationProblem(Fail("", "A request body is required.")))
                : (value, null);
        }
        catch (JsonException ex)
        {
            return (default, Results.ValidationProblem(Fail(PathToKey(ex.Path), ex.Message)));
        }
    }

    private static string PathToKey(string? path) => path switch
    {
        null or "$" => "",
        _ when path.StartsWith("$.", StringComparison.Ordinal) => path[2..],
        _ => path,
    };

    private static Dictionary<string, string[]> Fail(string key, string message) => new() { [key] = [message] };
}
