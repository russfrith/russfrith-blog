using Microsoft.Extensions.Time.Testing;
using TimeProviderDemo.Clock;
using TimeProviderDemo.Subscriptions;

namespace TimeProviderDemo.Tests;

/// <summary>
/// The same trial-subscription logic, tested two ways. The IClock version needs a hand-rolled
/// fake; the TimeProvider version gets FakeTimeProvider for free. Side by side, the wrapper buys
/// nothing the BCL type doesn't already give you.
/// </summary>
public sealed class PassThroughVsTimeProviderTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }

    [Fact]
    public void SubscriptionWithClock_expires_once_the_fake_clock_passes_the_trial_length()
    {
        var clock = new FakeClock { UtcNow = DateTimeOffset.Parse("2026-01-01T00:00:00Z") };
        var subscription = new SubscriptionWithClock(clock, clock.UtcNow, TimeSpan.FromDays(14));

        Assert.False(subscription.IsTrialExpired());

        clock.UtcNow = clock.UtcNow.AddDays(15);

        Assert.True(subscription.IsTrialExpired());
    }

    [Fact]
    public void SubscriptionWithTimeProvider_expires_once_fake_time_passes_the_trial_length()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var subscription = new SubscriptionWithTimeProvider(timeProvider, timeProvider.GetUtcNow(), TimeSpan.FromDays(14));

        Assert.False(subscription.IsTrialExpired());

        timeProvider.Advance(TimeSpan.FromDays(15));

        Assert.True(subscription.IsTrialExpired());
    }
}
