// C#'s signature footgun: an exception thrown inside an `async void` method is
// NOT caught by a try/catch around the call site -- it's rethrown directly on
// the SynchronizationContext (or, with none, crashes the process), because
// `async void` has no Task for the caller to observe or await.
async void DoWorkAsync()
{
    await Task.Delay(10);
    throw new InvalidOperationException("thrown inside async void");
}

try
{
    DoWorkAsync(); // looks like a normal call -- the compiler gives no warning here
    Console.WriteLine("call to DoWorkAsync() returned normally (exception hasn't happened yet)");
}
catch (Exception e)
{
    // this will NOT run, even though the exception above is a direct throw
    Console.WriteLine($"caught: {e.Message}");
}

await Task.Delay(100); // give the async void method's continuation time to actually throw
Console.WriteLine("reached end of Main (if you see this with no crash and no 'caught:' line, that's the footgun)");
