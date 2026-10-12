namespace TimeProviderDemo.Calendar;

/// <summary>
/// A wrapper worth keeping: it doesn't just forward "now", it adds domain meaning that
/// <see cref="TimeProvider"/> has no concept of: a tenant's local time zone and which days
/// count as business days.
/// </summary>
public sealed class BusinessCalendar
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _tenantTimeZone;
    private readonly IReadOnlySet<DayOfWeek> _weekendDays;

    public BusinessCalendar(TimeProvider timeProvider, TimeZoneInfo tenantTimeZone, IReadOnlySet<DayOfWeek>? weekendDays = null)
    {
        _timeProvider = timeProvider;
        _tenantTimeZone = tenantTimeZone;
        _weekendDays = weekendDays ?? new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
    }

    public DateTimeOffset NowInTenantTime() => _timeProvider.GetLocalNow(_tenantTimeZone);

    public bool IsBusinessDay(DateTimeOffset instant)
    {
        var localDay = TimeZoneInfo.ConvertTime(instant, _tenantTimeZone).DayOfWeek;
        return !_weekendDays.Contains(localDay);
    }

    public DateTimeOffset NextBusinessDay()
    {
        var candidate = NowInTenantTime().Date;
        var zoned = new DateTimeOffset(candidate, _tenantTimeZone.GetUtcOffset(candidate));
        do
        {
            zoned = zoned.AddDays(1);
        } while (!IsBusinessDay(zoned));

        return zoned;
    }
}
