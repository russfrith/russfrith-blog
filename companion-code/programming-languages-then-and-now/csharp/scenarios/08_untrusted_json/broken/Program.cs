// Scenario 8: parsing untrusted JSON. System.Text.Json with a plain record, no
// `required` modifier. Prediction: missing field silently zero-fills (closer to
// Go than to serde/kotlinx.serialization); wrong-typed field throws.
using System.Text.Json;

var client = new HttpClient();

async Task FetchAndDecode(string url)
{
    var body = await client.GetStringAsync(url);
    try
    {
        var item = JsonSerializer.Deserialize<Item>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Console.WriteLine($"{url}: {item} (no error if Count was absent -- no way to tell 0 from a real zero)");
    }
    catch (JsonException e)
    {
        Console.WriteLine($"{url}: decode error (correctly caught): {e.Message}");
    }
}

await FetchAndDecode("http://mockserver:8080/json/missing-field");
await FetchAndDecode("http://mockserver:8080/json/wrong-type");

record Item(int Id, string Label, int Count);
