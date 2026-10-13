using System.Collections.Concurrent;

namespace AuthDemo.Api.Auth;

// In-memory and unseeded beyond one demo account - a real app swaps this for a database-backed
// store without touching the endpoints below, since they only depend on this interface shape.
public sealed class UserStore
{
    private readonly ConcurrentDictionary<string, AppUser> _usersByEmail = new();
    private readonly ConcurrentDictionary<Guid, AppUser> _usersById = new();

    public UserStore()
    {
        var demoUser = new AppUser(
            Guid.NewGuid(),
            Email: "demo@example.com",
            DisplayName: "Demo User",
            PasswordHash: PasswordHasher.Hash("correct-horse-battery-staple"));

        _usersByEmail[demoUser.Email] = demoUser;
        _usersById[demoUser.Id] = demoUser;
    }

    public AppUser? FindByEmail(string email) =>
        _usersByEmail.GetValueOrDefault(email);

    public AppUser? FindById(Guid id) =>
        _usersById.GetValueOrDefault(id);
}

// Refresh tokens are opaque and stored server-side so a logout or a detected-reuse event can
// revoke one immediately - a JWT refresh token can't be revoked before it expires on its own.
public sealed class RefreshTokenStore
{
    private sealed record Entry(Guid UserId, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _tokens = new();

    public void Store(string token, Guid userId, DateTimeOffset expiresAt) =>
        _tokens[token] = new Entry(userId, expiresAt);

    public Guid? Redeem(string token)
    {
        if (!_tokens.TryRemove(token, out var entry)) return null;
        return entry.ExpiresAt > DateTimeOffset.UtcNow ? entry.UserId : null;
    }

    public void Revoke(string token) => _tokens.TryRemove(token, out _);
}
