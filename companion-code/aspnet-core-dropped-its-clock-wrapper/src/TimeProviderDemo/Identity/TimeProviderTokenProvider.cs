using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace TimeProviderDemo.Identity;

/// <summary>
/// A hand-rolled replacement for Identity's built-in <c>DataProtectorTokenProvider</c>.
/// It exists because the built-in provider calls <c>DateTimeOffset.UtcNow</c> directly and has
/// no <see cref="TimeProvider"/> hook at all (verified against .NET 11 RC1 source), so it's one
/// wrapper that earns its keep: it carries the one piece of behavior the framework doesn't
/// expose a seam for yet.
/// </summary>
public sealed class TimeProviderTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
    where TUser : class
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _lifespan;

    public TimeProviderTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        TimeSpan? lifespan = null)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _timeProvider = timeProvider;
        _lifespan = lifespan ?? TimeSpan.FromHours(3);
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(true);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        var userId = await manager.GetUserIdAsync(user);
        var protector = CreateProtector(purpose, userId);

        var issuedAt = _timeProvider.GetUtcNow();
        var payload = $"{userId}|{issuedAt.ToUnixTimeSeconds()}";
        var protectedBytes = protector.Protect(Encoding.UTF8.GetBytes(payload));

        return Convert.ToBase64String(protectedBytes);
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        var userId = await manager.GetUserIdAsync(user);
        var protector = CreateProtector(purpose, userId);

        try
        {
            var unprotectedBytes = protector.Unprotect(Convert.FromBase64String(token));
            var payload = Encoding.UTF8.GetString(unprotectedBytes);
            var parts = payload.Split('|');

            if (parts.Length != 2 || parts[0] != userId)
            {
                return false;
            }

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[1]));
            return _timeProvider.GetUtcNow() <= issuedAt + _lifespan;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private IDataProtector CreateProtector(string purpose, string userId) =>
        _dataProtectionProvider.CreateProtector(
            $"{nameof(TimeProviderTokenProvider<TUser>)}:{purpose}:{userId}");
}
