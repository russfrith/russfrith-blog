using TimeProviderDemo.Clock;

namespace TimeProviderDemo.Subscriptions;

/// <summary>
/// A trial subscription, tested the old way: via a pass-through <see cref="IClock"/>.
/// </summary>
public sealed class SubscriptionWithClock
{
    private readonly IClock _clock;
    private readonly DateTimeOffset _startedAt;
    private readonly TimeSpan _trialLength;

    public SubscriptionWithClock(IClock clock, DateTimeOffset startedAt, TimeSpan trialLength)
    {
        _clock = clock;
        _startedAt = startedAt;
        _trialLength = trialLength;
    }

    public bool IsTrialExpired() => _clock.UtcNow >= _startedAt + _trialLength;
}
