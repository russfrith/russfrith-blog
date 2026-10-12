namespace TimeProviderDemo.Clock;

public sealed class SystemClock : IClock
{
    // The one legitimate call site: a banned-API analyzer rule has no way to distinguish
    // "the wrapper's own implementation" from "code that should have used TimeProvider instead",
    // so every pass-through wrapper needs its own suppression here.
#pragma warning disable RS0030
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
#pragma warning restore RS0030
}
