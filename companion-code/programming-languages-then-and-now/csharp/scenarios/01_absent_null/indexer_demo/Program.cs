// Supplementary demo for scenario 1: Dictionary's indexer vs. TryGetValue.
var record = new Dictionary<string, string> { ["id"] = "1", ["label"] = "widget" };

try
{
    var value = record["count"]; // throws KeyNotFoundException
    Console.WriteLine($"count = {value}");
}
catch (KeyNotFoundException e)
{
    Console.WriteLine($"indexer threw: {e.GetType().Name}: {e.Message}");
}

if (record.TryGetValue("count", out var tgv))
{
    Console.WriteLine($"count = {tgv}");
}
else
{
    Console.WriteLine("TryGetValue: count absent, no exception");
}
