// Scenario 4 fixed: catch the specific exception and report it; await the Task
// so its exception is observed and handled instead of discarded.
try
{
    File.ReadAllText("/no/such/path.txt");
}
catch (IOException e)
{
    // covers both FileNotFoundException and DirectoryNotFoundException --
    // "/no/such/path.txt" fails because /no/ doesn't exist, not just the file
    Console.WriteLine($"file not found: {e.Message}");
}

try
{
    await Task.Run(() =>
    {
        throw new InvalidOperationException("thrown inside an awaited Task");
    });
}
catch (InvalidOperationException e)
{
    Console.WriteLine($"task exception observed and handled: {e.Message}");
}
