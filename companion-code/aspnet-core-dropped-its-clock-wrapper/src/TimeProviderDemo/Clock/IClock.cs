namespace TimeProviderDemo.Clock;

/// <summary>
/// A pass-through clock wrapper of the kind people wrote before <see cref="System.TimeProvider"/>
/// existed, purely so tests could control "now". It carries no domain meaning of its own.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
