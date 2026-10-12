using Microsoft.Extensions.Time.Testing;
using TimeProviderDemo.Calendar;

namespace TimeProviderDemo.Tests;

/// <summary>
/// BusinessCalendar wraps TimeProvider to add domain meaning TimeProvider has no concept of:
/// a tenant's local time zone and which days count as business days. This is the kind of
/// wrapper worth keeping.
/// </summary>
public sealed class BusinessCalendarTests
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    [Fact]
    public void NowInTenantTime_converts_fake_utc_time_into_the_tenants_zone()
    {
        // 2026-06-15T02:30:00Z is 2026-06-14T22:30:00 Eastern (EDT, UTC-4) the evening before.
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-06-15T02:30:00Z"));
        var calendar = new BusinessCalendar(timeProvider, Eastern);

        var tenantNow = calendar.NowInTenantTime();

        Assert.Equal(new DateTime(2026, 6, 14, 22, 30, 0), tenantNow.DateTime);
    }

    [Fact]
    public void IsBusinessDay_is_false_on_a_weekend_in_the_tenants_zone()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-06-13T15:00:00Z")); // Saturday
        var calendar = new BusinessCalendar(timeProvider, Eastern);

        Assert.False(calendar.IsBusinessDay(timeProvider.GetUtcNow()));
    }

    [Fact]
    public void NextBusinessDay_skips_the_weekend()
    {
        // Friday 2026-06-12, late in the Eastern day.
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-06-12T23:00:00Z"));
        var calendar = new BusinessCalendar(timeProvider, Eastern);

        var next = calendar.NextBusinessDay();

        Assert.Equal(DayOfWeek.Monday, next.DayOfWeek);
        Assert.Equal(15, next.Day);
    }
}
