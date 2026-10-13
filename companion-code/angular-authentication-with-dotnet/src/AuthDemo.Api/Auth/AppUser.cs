namespace AuthDemo.Api.Auth;

public sealed record AppUser(Guid Id, string Email, string DisplayName, string PasswordHash);
