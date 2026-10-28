// Same plain, unmarked arithmetic as broken/Program.cs -- but this project sets
// <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>, which makes EVERY
// arithmetic operation in the project checked by default, no `checked` keyword
// needed anywhere in the source.
int x = int.MaxValue;
try
{
    int y = x + 1;
    Console.WriteLine($"int.MaxValue + 1 = {y}");
}
catch (OverflowException e)
{
    Console.WriteLine($"overflow detected (project-level CheckForOverflowUnderflow): {e.Message}");
}
