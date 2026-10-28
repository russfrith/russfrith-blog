// Fixed: `async Task` instead of `async void` gives the caller a Task to await,
// so the exception propagates normally through a try/catch around the await.
async Task DoWorkAsync()
{
    await Task.Delay(10);
    throw new InvalidOperationException("thrown inside async Task");
}

try
{
    await DoWorkAsync();
    Console.WriteLine("call to DoWorkAsync() returned normally");
}
catch (Exception e)
{
    Console.WriteLine($"caught: {e.Message}");
}
