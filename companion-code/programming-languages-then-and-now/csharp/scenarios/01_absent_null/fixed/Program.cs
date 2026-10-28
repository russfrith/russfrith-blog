// Scenario 1 fixed: declare the return type nullable (string?) and respect it,
// plus TryGetValue instead of indexing so absence never throws.
var record = new Dictionary<string, string> { ["id"] = "1", ["label"] = "widget" };

string? label = GetLabel(record);
if (label is null)
{
    Console.WriteLine("label absent");
}
else
{
    Console.WriteLine($"label length: {label.Length}");
}

static string? GetLabel(Dictionary<string, string> record)
{
    record.TryGetValue("missing-key", out var value);
    return value; // no warning: declared nullable, matches reality
}
