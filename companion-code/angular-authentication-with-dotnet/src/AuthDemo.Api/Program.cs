using System.Security.Claims;
using AuthDemo.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string DevClientPolicy = "DevClient";
var clientOrigin = builder.Configuration["Client:Origin"] ?? "http://localhost:4200";

builder.Services.AddCors(options => options.AddPolicy(DevClientPolicy, policy =>
    policy.WithOrigins(clientOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials())); // required so the browser attaches the refresh-token cookie

builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "AuthDemo.Api",
            ValidAudience = "AuthDemo.Client",
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(builder.Configuration["Jwt:SigningKey"]!)),
            ClockSkew = TimeSpan.FromSeconds(5),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors(DevClientPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", (LoginRequest request, UserStore users, RefreshTokenStore refreshTokens,
    TokenService tokenService, HttpResponse response) =>
{
    var user = users.FindByEmail(request.Email);
    if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
    IssueRefreshTokenCookie(response, refreshTokens, tokenService, user.Id);

    return Results.Ok(new LoginResponse(accessToken, expiresAt, user.Email, user.DisplayName));
});

app.MapPost("/api/auth/refresh", (HttpRequest request, HttpResponse response, UserStore users,
    RefreshTokenStore refreshTokens, TokenService tokenService) =>
{
    if (!request.Cookies.TryGetValue("refresh_token", out var refreshToken))
    {
        return Results.Unauthorized();
    }

    var userId = refreshTokens.Redeem(refreshToken);
    var user = userId is null ? null : users.FindById(userId.Value);
    if (user is null)
    {
        response.Cookies.Delete("refresh_token");
        return Results.Unauthorized();
    }

    // Redeem already removed the old token above, so issuing a fresh one here rotates it on
    // every use - that limits the blast radius if a refresh token ever leaks.
    var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
    IssueRefreshTokenCookie(response, refreshTokens, tokenService, user.Id);

    return Results.Ok(new LoginResponse(accessToken, expiresAt, user.Email, user.DisplayName));
});

app.MapPost("/api/auth/logout", (HttpRequest request, HttpResponse response, RefreshTokenStore refreshTokens) =>
{
    if (request.Cookies.TryGetValue("refresh_token", out var refreshToken))
    {
        refreshTokens.Revoke(refreshToken);
    }

    response.Cookies.Delete("refresh_token");
    return Results.NoContent();
});

app.MapGet("/api/me", (ClaimsPrincipal user) =>
    Results.Ok(new
    {
        email = user.FindFirst(ClaimTypes.Email)?.Value,
        name = user.Identity?.Name,
    })).RequireAuthorization();

app.Run();

static void IssueRefreshTokenCookie(HttpResponse response, RefreshTokenStore refreshTokens,
    TokenService tokenService, Guid userId)
{
    var refreshToken = TokenService.CreateRefreshToken();
    var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
    refreshTokens.Store(refreshToken, userId, expiresAt);

    response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = false, // dev only runs over http://localhost; set true behind HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = expiresAt,
        Path = "/api/auth",
    });
}

record LoginRequest(string Email, string Password);
record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string Email, string DisplayName);
