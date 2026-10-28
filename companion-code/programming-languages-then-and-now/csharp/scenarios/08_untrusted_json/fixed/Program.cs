// Scenario 8 fixed: the `required` modifier (C# 11+) makes System.Text.Json
// throw if the property is missing from the JSON, instead of zero-filling it.
using System.Text.Json;

var client = new HttpClient();

async Task FetchAndDecode(string url)
{
    var body = await client.GetStringAsync(url);
    try
    {
        var item = JsonSerializer.Deserialize<Item>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Console.WriteLine($"{url}: Count={item?.Count}");
    }
    catch (JsonException e)
    {
        Console.WriteLine($"{url}: decode error (correctly caught): {e.Message}");
    }
}

await FetchAndDecode("http://mockserver:8080/json/missing-field");
await FetchAndDecode("http://mockserver:8080/json/wrong-type");

class Item
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public required int Count { get; set; }
}
