namespace TimeProviderDemo.Tests;

/// <summary>
/// Honest downsides, demonstrated rather than asserted as opinion.
/// </summary>
public sealed class DownsidesTests
{
    // Downside 1: TimeProvider is abstract with a real surface area, not a one-member interface.
    // IClock in this repo is one property. A minimal custom TimeProvider has to decide what to
    // do with GetUtcNow, LocalTimeZone, GetTimestamp, TimestampFrequency, and CreateTimer.
    // GetUtcNow is the only one you can usually get away with overriding, but the other four
    // still exist on every subclass whether you think about them or not.
    private sealed class MinimalTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _fixedUtcNow;

        public MinimalTimeProvider(DateTimeOffset fixedUtcNow) => _fixedUtcNow = fixedUtcNow;

        public override DateTimeOffset GetUtcNow() => _fixedUtcNow;
        // LocalTimeZone, GetTimestamp, TimestampFrequency, and CreateTimer are all inherited
        // from the base class with real (if generic) default behavior. Easy to forget they're
        // there until something depends on one of them.
    }

    [Fact]
    public void A_minimal_custom_TimeProvider_still_inherits_LocalTimeZone_and_timer_support()
    {
        var provider = new MinimalTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        // These all work despite only overriding GetUtcNow, which is reassuring, but also
        // means a subclass that only cares about "now" still carries the rest of the contract.
        Assert.NotNull(provider.LocalTimeZone);
        Assert.True(provider.TimestampFrequency > 0);
        using var timer = provider.CreateTimer(_ => { }, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        Assert.NotNull(timer);
    }

    // Downside 2: TimeProvider.System.LocalTimeZone is whatever the machine (or container) is
    // set to. It has no idea what "local" should mean for a given tenant or user. In most CI
    // containers and server deployments that's UTC, which quietly turns "local" time code into
    // UTC time code until someone runs it on a workstation set to a different zone.
    [Fact]
    public void TimeProvider_System_LocalTimeZone_is_just_the_machines_zone_not_a_business_concept()
    {
        Assert.Equal(TimeZoneInfo.Local, TimeProvider.System.LocalTimeZone);
    }
}
