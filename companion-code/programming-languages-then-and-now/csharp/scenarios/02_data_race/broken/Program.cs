// Scenario 2: data race. Two Tasks increment a shared counter with no
// synchronization.
const int iterations = 200_000;
long counter = 0;

var t1 = Task.Run(() =>
{
    for (int i = 0; i < iterations; i++) counter++;
});
var t2 = Task.Run(() =>
{
    for (int i = 0; i < iterations; i++) counter++;
});
await Task.WhenAll(t1, t2);

Console.WriteLine($"counter = {counter} (expected {2 * iterations})");
