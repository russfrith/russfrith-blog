// Scenario 5 fixed: `using` calls Dispose() deterministically when the
// declaration's scope ends, even on an exception.
using var stream = new FileStream("/etc/hostname", FileMode.Open);
var buffer = new byte[64];
int n = stream.Read(buffer, 0, buffer.Length);
Console.WriteLine($"read {n} bytes, stream disposed deterministically via using");
