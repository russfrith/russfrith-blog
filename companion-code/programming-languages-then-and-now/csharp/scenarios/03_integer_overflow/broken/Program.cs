// Scenario 3: integer overflow. C#'s default arithmetic context is `unchecked`
// -- overflow wraps silently, like Go/Kotlin, no exception.
int x = int.MaxValue;
int y = x + 1;
Console.WriteLine($"int.MaxValue + 1 = {y}");
