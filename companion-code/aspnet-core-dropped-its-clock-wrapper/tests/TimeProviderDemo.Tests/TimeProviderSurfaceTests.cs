using Microsoft.Extensions.Time.Testing;

namespace TimeProviderDemo.Tests;

/// <summary>
/// The core TimeProvider surface: timestamps, timers, and the handful of BCL APIs that grew a
/// TimeProvider overload specifically so they could be driven by a fake clock in tests.
/// </summary>
public sealed class TimeProviderSurfaceTests
{
    [Fact]
    public void GetUtcNow_reflects_the_starting_instant_and_subsequent_advances()
    {
        var start = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
        var timeProvider = new FakeTimeProvider(start);

        Assert.Equal(start, timeProvider.GetUtcNow());

        timeProvider.Advance(TimeSpan.FromHours(3));

        Assert.Equal(start + TimeSpan.FromHours(3), timeProvider.GetUtcNow());
    }

    [Fact]
    public void GetTimestamp_and_GetElapsedTime_measure_fake_elapsed_time()
    {
        var timeProvider = new FakeTimeProvider();
        var start = timeProvider.GetTimestamp();

        timeProvider.Advance(TimeSpan.FromSeconds(42));

        Assert.Equal(TimeSpan.FromSeconds(42), timeProvider.GetElapsedTime(start));
    }

    [Fact]
    public async Task Task_Delay_with_a_fake_time_provider_completes_once_advanced_past_the_delay()
    {
        var timeProvider = new FakeTimeProvider();
        var delayTask = Task.Delay(TimeSpan.FromMinutes(5), timeProvider);

        Assert.False(delayTask.IsCompleted);

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        await delayTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(delayTask.IsCompleted);
    }

    [Fact]
    public async Task Task_WaitAsync_with_a_fake_time_provider_times_out_once_advanced_past_the_timeout()
    {
        var timeProvider = new FakeTimeProvider();
        var neverCompletes = new TaskCompletionSource();

        var waitTask = neverCompletes.Task.WaitAsync(TimeSpan.FromSeconds(30), timeProvider);

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        await Assert.ThrowsAsync<TimeoutException>(() => waitTask);
    }

    [Fact]
    public async Task CancellationTokenSource_with_a_fake_time_provider_cancels_once_advanced_past_the_delay()
    {
        var timeProvider = new FakeTimeProvider();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10), timeProvider);

        var tcs = new TaskCompletionSource();
        cts.Token.Register(() => tcs.TrySetResult());

        Assert.False(cts.Token.IsCancellationRequested);

        timeProvider.Advance(TimeSpan.FromSeconds(10));

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task PeriodicTimer_with_a_fake_time_provider_ticks_once_per_period_advanced()
    {
        var timeProvider = new FakeTimeProvider();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);

        var firstTick = timer.WaitForNextTickAsync();
        Assert.False(firstTick.IsCompleted);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.True(await firstTick);

        var secondTick = timer.WaitForNextTickAsync();
        Assert.False(secondTick.IsCompleted);

        // Honest downside: advancing by less than a full period doesn't tick, and advancing by
        // more than one period's worth only fires a single tick, not one per elapsed period.
        // PeriodicTimer coalesces catch-up ticks rather than queuing them.
        timeProvider.Advance(TimeSpan.FromMilliseconds(500));
        Assert.False(secondTick.IsCompleted);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.True(await secondTick);
    }
}
