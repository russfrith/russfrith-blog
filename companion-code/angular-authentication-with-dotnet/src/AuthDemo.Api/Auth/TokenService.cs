using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AuthDemo.Api.Auth;

public sealed class TokenService(IConfiguration configuration)
{
    private readonly SymmetricSecurityKey _signingKey = new(
        Convert.FromBase64String(configuration["Jwt:SigningKey"]!));

    public (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
        };

        var token = new JwtSecurityToken(
            issuer: "AuthDemo.Api",
            audience: "AuthDemo.Client",
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // 256-bit opaque token, not a JWT: the client never inspects it, so there's nothing
    // to gain from making it structured, and an opaque value can be revoked by a server-side
    // lookup alone. Returned only in an HttpOnly cookie - JavaScript never sees it.
    public static string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
