// Scenario 1: absent value. A fresh .NET project template enables nullable
// reference types -- but violating that annotation is a BUILD WARNING, not an
// error, so this compiles and runs, then throws at the actual point of use.
// Separately: direct Dictionary indexing throws KeyNotFoundException on a
// missing key, same family of problem as the other languages' direct-index cases.
var record = new Dictionary<string, string> { ["id"] = "1", ["label"] = "widget" };

string label = GetLabel(record); // declared non-null, but can genuinely be null
Console.WriteLine($"label length: {label.Length}"); // NullReferenceException if GetLabel returned null

static string GetLabel(Dictionary<string, string> record)
{
    record.TryGetValue("missing-key", out var value);
    return value; // CS8603 warning expected here: possible null return, NOT a build error
}
