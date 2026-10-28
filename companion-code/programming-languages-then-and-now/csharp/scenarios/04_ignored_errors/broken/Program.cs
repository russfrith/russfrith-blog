// Scenario 4: ignored errors. Part 1: a swallowed catch. Part 2: a fire-and-forget
// Task whose body throws, never awaited, never stored, never observed.
try
{
    File.ReadAllText("/no/such/path.txt");
}
catch
{
    // swallowed
}

Console.WriteLine("part 1 done (swallowed catch)");

// Fire-and-forget: this Task is started and never awaited or observed.
_ = Task.Run(() =>
{
    throw new InvalidOperationException("thrown inside a fire-and-forget Task");
});

Console.WriteLine("part 2 done (fire-and-forget Task started, not awaited)");
await Task.Delay(500); // give the unobserved task time to actually run/throw before exit
Console.WriteLine("process reached the end without crashing");
