// Scenario 3 fixed: an explicit `checked` block throws OverflowException instead
// of wrapping silently.
int x = int.MaxValue;
try
{
    int y = checked(x + 1);
    Console.WriteLine($"int.MaxValue + 1 = {y}");
}
catch (OverflowException e)
{
    Console.WriteLine($"overflow detected: {e.Message}");
}
