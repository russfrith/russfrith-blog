using System.Text.Json.Serialization;

namespace RecipeBox.Api.Tests;

// Minimal shape for reading back application/problem+json responses,
// independent of which ProblemDetails type the server used to write them.
public class ProblemPayload
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]> Errors { get; set; } = [];
}
