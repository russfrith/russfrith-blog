using Microsoft.AspNetCore.Identity;
using TimeProviderDemo.Identity;

namespace TimeProviderDemo.Tests;

/// <summary>
/// Unlike the built-in DataProtectorTokenProvider, our hand-rolled TimeProviderTokenProvider
/// (TimeProviderDemo/Identity/TimeProviderTokenProvider.cs) reads TimeProvider directly, so it
/// closes the gap TokenExpiryGapTests demonstrates, and is itself testable with FakeTimeProvider.
/// This is the wrapper-that-earns-its-keep example: it exists because the framework has a real,
/// verified limitation, not because testability alone demanded it.
/// </summary>
public sealed class TimeProviderTokenProviderTests
{
    [Fact]
    public async Task Token_is_valid_within_its_lifespan()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "custom-token@example.com", Email = "custom-token@example.com" };
        await userManager.CreateAsync(user);

        var token = await userManager.GenerateUserTokenAsync(
            user, IdentityFixture.TimeProviderTokenProviderName, "test-purpose");

        fixture.TimeProvider.Advance(TimeSpan.FromHours(2));

        var isValid = await userManager.VerifyUserTokenAsync(
            user, IdentityFixture.TimeProviderTokenProviderName, "test-purpose", token);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Token_expires_once_fake_time_advances_past_its_lifespan()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "custom-token-expired@example.com", Email = "custom-token-expired@example.com" };
        await userManager.CreateAsync(user);

        var token = await userManager.GenerateUserTokenAsync(
            user, IdentityFixture.TimeProviderTokenProviderName, "test-purpose");

        // Default lifespan in TimeProviderTokenProvider is 3 hours.
        fixture.TimeProvider.Advance(TimeSpan.FromHours(4));

        var isValid = await userManager.VerifyUserTokenAsync(
            user, IdentityFixture.TimeProviderTokenProviderName, "test-purpose", token);

        // Unlike the built-in provider, this one DOES honor the fake clock: it's expired.
        Assert.False(isValid);
    }
}
