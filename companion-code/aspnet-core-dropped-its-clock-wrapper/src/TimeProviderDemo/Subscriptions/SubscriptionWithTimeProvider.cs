namespace TimeProviderDemo.Subscriptions;

/// <summary>
/// The same trial subscription logic as <see cref="SubscriptionWithClock"/>, this time built
/// directly on <see cref="TimeProvider"/> instead of a hand-rolled wrapper.
/// </summary>
public sealed class SubscriptionWithTimeProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _startedAt;
    private readonly TimeSpan _trialLength;

    public SubscriptionWithTimeProvider(TimeProvider timeProvider, DateTimeOffset startedAt, TimeSpan trialLength)
    {
        _timeProvider = timeProvider;
        _startedAt = startedAt;
        _trialLength = trialLength;
    }

    public bool IsTrialExpired() => _timeProvider.GetUtcNow() >= _startedAt + _trialLength;
}
