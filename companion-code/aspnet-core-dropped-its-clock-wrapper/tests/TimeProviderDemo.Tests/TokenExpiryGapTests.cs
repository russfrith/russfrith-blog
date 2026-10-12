using Microsoft.AspNetCore.Identity;
using TimeProviderDemo.Identity;

namespace TimeProviderDemo.Tests;

/// <summary>
/// The gap: Identity's default token providers (password reset, email confirmation, change
/// email/phone, all backed by DataProtectorTokenProvider&lt;TUser&gt;) call
/// DateTimeOffset.UtcNow directly. Verified against .NET 11 RC1 source: no TimeProvider hook
/// exists anywhere in that class. These tests prove it empirically: advancing the *fake* clock
/// far past the configured token lifespan has no effect, because the real wall clock is what
/// the provider actually reads.
/// </summary>
public sealed class TokenExpiryGapTests
{
    [Fact]
    public async Task Password_reset_token_ignores_fake_time_advancing_past_its_lifespan()
    {
        using var fixture = new IdentityFixture(configureIdentity: options =>
            options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider);
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "reset-token@example.com", Email = "reset-token@example.com" };
        await userManager.CreateAsync(user);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        // Identity's default DataProtectionTokenProviderOptions.TokenLifespan is 1 day.
        // Advance the FAKE clock 10 days. If the provider honored TimeProvider, this token
        // would now be expired.
        fixture.TimeProvider.Advance(TimeSpan.FromDays(10));

        var isValid = await userManager.VerifyUserTokenAsync(
            user, TokenOptions.DefaultProvider, "ResetPassword", token);

        // This is the gap: still valid, because DataProtectorTokenProvider reads the real
        // wall clock (DateTimeOffset.UtcNow), not the FakeTimeProvider we advanced.
        Assert.True(isValid);
    }

    [Fact]
    public async Task Email_confirmation_token_ignores_fake_time_advancing_past_its_lifespan()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "confirm-token@example.com", Email = "confirm-token@example.com" };
        await userManager.CreateAsync(user);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        fixture.TimeProvider.Advance(TimeSpan.FromDays(10));

        var result = await userManager.ConfirmEmailAsync(user, token);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Change_email_token_ignores_fake_time_advancing_past_its_lifespan()
    {
        using var fixture = new IdentityFixture();
        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser { UserName = "change-email-token@example.com", Email = "old@example.com" };
        await userManager.CreateAsync(user);

        var token = await userManager.GenerateChangeEmailTokenAsync(user, "new@example.com");

        fixture.TimeProvider.Advance(TimeSpan.FromDays(10));

        var result = await userManager.ChangeEmailAsync(user, "new@example.com", token);

        Assert.True(result.Succeeded);
    }
}
