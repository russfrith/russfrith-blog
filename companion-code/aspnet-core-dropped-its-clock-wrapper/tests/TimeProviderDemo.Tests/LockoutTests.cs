using Microsoft.AspNetCore.Identity;
using TimeProviderDemo.Identity;

namespace TimeProviderDemo.Tests;

/// <summary>
/// Identity's lockout check (UserManager.IsLockedOutAsync / AccessFailedAsync) pulls its clock
/// from DI via a private UtcNow() helper that falls back to DateTimeOffset.UtcNow when no
/// TimeProvider is registered. Confirmed against dotnet/aspnetcore source: this helper only
/// exists starting at the .NET 11 RC1 tag, not at the release/8.0, release/9.0, or release/10.0
/// tags. It's also not exposed as a settable option on IdentityOptions; it only works because
/// we register a TimeProvider in DI.
/// </summary>
public sealed class LockoutTests
{
    [Fact]
    public async Task User_is_locked_out_after_max_failed_attempts()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "locked-out@example.com", Email = "locked-out@example.com" };
        await userManager.CreateAsync(user);

        for (var i = 0; i < 3; i++)
        {
            await userManager.AccessFailedAsync(user);
        }

        Assert.True(await userManager.IsLockedOutAsync(user));
    }

    [Fact]
    public async Task Lockout_clears_once_fake_time_advances_past_lockout_window()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "unlocks@example.com", Email = "unlocks@example.com" };
        await userManager.CreateAsync(user);

        for (var i = 0; i < 3; i++)
        {
            await userManager.AccessFailedAsync(user);
        }

        Assert.True(await userManager.IsLockedOutAsync(user));

        // No Thread.Sleep: advance the fake clock past the 5-minute lockout window.
        fixture.TimeProvider.Advance(TimeSpan.FromMinutes(6));

        Assert.False(await userManager.IsLockedOutAsync(user));
    }

    [Fact]
    public async Task Lockout_does_not_clear_before_the_window_elapses()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "still-locked@example.com", Email = "still-locked@example.com" };
        await userManager.CreateAsync(user);

        for (var i = 0; i < 3; i++)
        {
            await userManager.AccessFailedAsync(user);
        }

        fixture.TimeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.True(await userManager.IsLockedOutAsync(user));
    }
}
