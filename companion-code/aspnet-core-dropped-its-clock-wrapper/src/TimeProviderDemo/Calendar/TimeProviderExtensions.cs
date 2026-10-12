namespace TimeProviderDemo.Calendar;

public static class TimeProviderExtensions
{
    /// <summary>
    /// Converts the provider's current UTC instant into the given time zone.
    /// TimeProvider has no time-zone concept of its own beyond <see cref="TimeProvider.LocalTimeZone"/>,
    /// which defaults to the machine's local zone (UTC in most containers), not a tenant's zone.
    /// </summary>
    public static DateTimeOffset GetLocalNow(this TimeProvider timeProvider, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
}
