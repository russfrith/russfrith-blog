using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeProviderDemo.Identity;

namespace TimeProviderDemo.Tests;

/// <summary>
/// SecurityStampValidatorOptions.TimeProvider has been a settable property since .NET 8. It's
/// used directly by SecurityStampValidator&lt;TUser&gt;.ValidateAsync, so this one is testable
/// with FakeTimeProvider via DI, same as lockout, with no extra plumbing.
/// </summary>
public sealed class SecurityStampTests
{
    private static async Task<CookieValidatePrincipalContext> ValidateAsync(
        IdentityFixture fixture, System.Security.Claims.ClaimsPrincipal principal, DateTimeOffset issuedUtc)
    {
        var validator = fixture.GetRequiredService<ISecurityStampValidator>();

        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { IssuedUtc = issuedUtc }, scheme.Name);

        var cookieOptions = fixture.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        var httpContext = new DefaultHttpContext { RequestServices = fixture.Services };
        fixture.GetRequiredService<SignInManager<AppUser>>().Context = httpContext;

        var context = new CookieValidatePrincipalContext(httpContext, scheme, cookieOptions, ticket);

        await validator.ValidateAsync(context);
        return context;
    }

    [Fact]
    public async Task Principal_is_refreshed_once_fake_time_passes_the_validation_interval()
    {
        using var fixture = new IdentityFixture(configureServices: services =>
            services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(30)));

        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();
        var signInManager = fixture.GetRequiredService<SignInManager<AppUser>>();
        var user = new AppUser { UserName = "stamp@example.com", Email = "stamp@example.com" };
        await userManager.CreateAsync(user);

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var issuedAt = fixture.TimeProvider.GetUtcNow();
        fixture.TimeProvider.Advance(TimeSpan.FromMinutes(31));

        var context = await ValidateAsync(fixture, principal, issuedAt);

        Assert.True(context.ShouldRenew);
        Assert.NotNull(context.Principal);
    }

    [Fact]
    public async Task Principal_is_not_renewed_before_the_validation_interval_elapses()
    {
        using var fixture = new IdentityFixture(configureServices: services =>
            services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(30)));

        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();
        var signInManager = fixture.GetRequiredService<SignInManager<AppUser>>();
        var user = new AppUser { UserName = "stamp-fresh@example.com", Email = "stamp-fresh@example.com" };
        await userManager.CreateAsync(user);

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var issuedAt = fixture.TimeProvider.GetUtcNow();
        fixture.TimeProvider.Advance(TimeSpan.FromMinutes(5));

        var context = await ValidateAsync(fixture, principal, issuedAt);

        Assert.False(context.ShouldRenew);
    }

    [Fact]
    public async Task Principal_is_rejected_once_the_security_stamp_no_longer_matches()
    {
        using var fixture = new IdentityFixture(configureServices: services =>
            services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(30)));

        var userManager = fixture.GetRequiredService<UserManager<AppUser>>();
        var signInManager = fixture.GetRequiredService<SignInManager<AppUser>>();
        var user = new AppUser { UserName = "stamp-changed@example.com", Email = "stamp-changed@example.com" };
        await userManager.CreateAsync(user);

        // The principal (as it would be stored in the cookie) is built from the stamp at issue time.
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var issuedAt = fixture.TimeProvider.GetUtcNow();

        // Something changed the user's security stamp (e.g. a password reset) after the cookie was issued.
        await userManager.UpdateSecurityStampAsync(user);

        fixture.TimeProvider.Advance(TimeSpan.FromMinutes(31));

        var context = await ValidateAsync(fixture, principal, issuedAt);

        Assert.Null(context.Principal);
    }
}
