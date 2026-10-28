// Scenario 5: resource cleanup. FileStream is IDisposable; no `using`, no
// explicit Dispose() -- relies on the finalizer to eventually release the
// handle, not deterministic.
var stream = new FileStream("/etc/hostname", FileMode.Open);
var buffer = new byte[64];
int n = stream.Read(buffer, 0, buffer.Length);
Console.WriteLine($"read {n} bytes, stream never explicitly disposed");
