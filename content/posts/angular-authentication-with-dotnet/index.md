---
title: "Angular Authentication With .NET"
description: "Wiring JWT access tokens and an HttpOnly refresh cookie between an ASP.NET Core 10/11 Web API and a standalone, zoneless Angular 22 app"
date: 2026-10-12T00:00:00-04:00
draft: true
cover:
  image: pexels-gotta-be-worth-it-1160845.jpg
  alt: "Assorted-color metal locks"
  caption: "Photo by [Gotta Be Worth It](https://www.pexels.com/@myersmc16/) from [Pexels](https://www.pexels.com/photo/assorted-color-metal-keys-1160845/)"
ShowToc: true
tags: ["angular", "dotnet", "csharp", "oauth", "jwt"]
categories: ["programming"]
---

{{< aside >}}
Built and tested against the .NET 10 SDK (10.0.401) and Node 22, running in Docker. Everything here targets ASP.NET Core 10, the current LTS; nothing about JWT bearer authentication changed for .NET 11, which was still in preview as of this writing. The [companion code](https://github.com/russfrith/russfrith-blog/tree/master/companion-code/angular-authentication-with-dotnet) is a runnable version of the whole flow, API and Angular app, with a `docker compose up` to try it yourself.
{{< /aside >}}

## Authentication vs authorization

Authentication is the act of validating that a user is whom they claim to be. Every other security measure rests on the assumption that the system has correctly identified the current user. Authorization is the act of granting an authenticated party permission to access a specific resource or function. You need both, but they're different problems with different failure modes: get authentication wrong and you let the wrong person in; get authorization wrong and you let the right person do the wrong thing.

Username and password is still the most common authentication mechanism, and it's still fraught with the same problems it always had. Users choose weak passwords and reuse them across sites. None of that is specific to Angular or .NET; it's why this post doesn't spend any more time on password hygiene than it takes to say: hash with something slow (PBKDF2, bcrypt, Argon2), never store plaintext, and let a library do it. The interesting part, for a single-page app talking to an API, is what happens *after* the password check succeeds.

### JWT

JSON Web Tokens (JWT) are an open, URL-safe, industry-standard ([RFC 7519](https://www.rfc-editor.org/rfc/rfc7519)) way to represent claims securely between two parties. A JWT is three base64url-encoded segments separated by dots: a header, a payload of claims, and a signature. The server signs it; anyone can read the claims (it's not encrypted), but nobody can forge or alter it without the signing key. That's what makes it useful as a bearer token: the API doesn't need to look anything up to trust it, just verify the signature and check the claims.

That "no lookup needed" property is also JWT's biggest limitation for session management: a signed, unexpired JWT is valid until it expires, full stop. There's no revocation list built into the format. The usual answer is to keep access tokens short-lived (minutes, not days) and pair them with a separate, server-tracked refresh token that *can* be revoked. That's the shape this post builds.

## The shape of the problem

An Angular SPA and an ASP.NET Core API are two separate applications, usually on two separate origins (`localhost:4200` and `localhost:5268` in dev; two different subdomains in production). That separation is exactly what makes this different from classic server-rendered auth, where the browser just holds a session cookie and the server does all the work. Here, the browser has to:

- Send credentials to the API and get back something it can prove it's authenticated with.
- Attach that proof to every subsequent API request.
- Deal with that proof expiring, ideally without forcing the user to log in again every five minutes.
- Let the user log out in a way that actually invalidates the session, not just "forget the token and hope."

The design used throughout this post, and in the companion code:

- **Access token**: a JWT, 5-minute lifetime, returned in the login response body and held in memory on the client (never in `localStorage` or `sessionStorage`).
- **Refresh token**: an opaque, server-tracked random value, 7-day lifetime, set as an `HttpOnly` cookie the client never touches directly. Rotated (replaced with a new one) on every use, so a leaked refresh token is only useful once before the legitimate client's next refresh invalidates it.

{{< aside >}}
Why not just put the access token in `localStorage` and skip the cookie entirely? Because `localStorage` is readable by any JavaScript running on the page, which means it's readable by any successful XSS injection too. Keeping the long-lived credential (the refresh token) in an `HttpOnly` cookie means an XSS payload can still steal the short-lived access token out of memory while the tab is open, but it can't read the thing that would let it mint new access tokens after the tab closes. That's the whole reason for splitting the two token types instead of using one.
{{< /aside >}}

## The ASP.NET Core side

Minimal APIs, no controllers, no EF Core, no `Microsoft.AspNetCore.Identity` database - just enough to show the auth mechanics clearly. (If you *are* using Identity with cookie-based auth, the `TimeProvider` caveats in [ASP.NET Core Dropped Its Own Clock Wrapper]({{< ref "aspnet-core-dropped-its-clock-wrapper" >}}) apply to your lockout and token-expiry tests; they don't apply here because nothing below touches Identity.)

### Issuing the access token

```csharp
// src/AuthDemo.Api/Auth/TokenService.cs
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
```

Nothing exotic: `System.IdentityModel.Tokens.Jwt`, a symmetric signing key, a 5-minute expiry. The API validates these the ordinary way, via `AddJwtBearer`:

```csharp
// src/AuthDemo.Api/Program.cs
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
```

`ClockSkew` defaults to 5 minutes in the library, which means a token you issued with a 5-minute lifetime would effectively be valid for up to 10. Tightening it to a few seconds here makes the 5-minute expiry mean what it says, which matters later when we want to actually exercise the refresh path without waiting five minutes for it.

### Issuing the refresh token

The refresh token is deliberately *not* a JWT. The client never inspects it, so there's nothing to gain from making it structured, and an opaque value can be revoked by deleting a server-side record - a JWT refresh token can't be revoked before it expires on its own, which defeats the point of having a revocable token at all.

```csharp
// src/AuthDemo.Api/Auth/TokenService.cs
public static string CreateRefreshToken() =>
    Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
```

It's stored server-side (in-memory `ConcurrentDictionary` in the demo; a real app uses Redis or a database table) keyed by the token value, and set on the response as an `HttpOnly` cookie scoped to the auth endpoints:

```csharp
// src/AuthDemo.Api/Program.cs
response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
{
    HttpOnly = true,
    Secure = false, // dev only runs over http://localhost; set true behind HTTPS
    SameSite = SameSiteMode.Lax,
    Expires = expiresAt,
    Path = "/api/auth",
});
```

`HttpOnly` is what keeps JavaScript from reading it. `Path = "/api/auth"` means the browser only sends it to the auth endpoints, not to every API call, which matters since those are the only endpoints that need it. `SameSite = Lax` works here because `localhost:4200` and `localhost:5268` are different origins but the same *site* (same scheme, same registrable domain, just different ports) - same-site cookies are still sent on cross-origin requests as long as the site matches.

### The refresh endpoint, and why it rotates

```csharp
// src/AuthDemo.Api/Program.cs
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

    var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
    IssueRefreshTokenCookie(response, refreshTokens, tokenService, user.Id);

    return Results.Ok(new LoginResponse(accessToken, expiresAt, user.Email, user.DisplayName));
});
```

`RefreshTokenStore.Redeem` removes the token as part of looking it up, so it can't be used twice - the endpoint always issues a brand-new refresh token alongside the new access token. This is rotation: every refresh invalidates the token that was just used. The benefit shows up if a refresh token ever leaks (logged somewhere it shouldn't be, intercepted, whatever): the legitimate client's *next* refresh will fail, because its token was already consumed by the attacker's use, which is a visible signal something's wrong rather than a silent, indefinite compromise.

## The Angular side

The client is a standalone, zoneless Angular 22 app - no `NgModule`, no `zone.js`, `provideZonelessChangeDetection()` in `app.config.ts`. Everything reactive is a signal.

### Holding the session

```typescript
// src/auth-client/src/app/auth/auth.service.ts
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<Session | null>(null);

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly currentUser = computed(() => this.session());

  login(email: string, password: string): Observable<boolean> {
    return this.http
      .post<LoginResponse>(`${API_BASE_URL}/api/auth/login`, { email, password }, { withCredentials: true })
      .pipe(
        tap((response) => this.applySession(response)),
        map(() => true),
        catchError(() => of(false)),
      );
  }
  // refresh() and logout() follow the same shape
}
```

`withCredentials: true` is what tells the browser's `fetch`/`XHR` to send and accept cookies on a cross-origin request - without it, the API's `Set-Cookie` for the refresh token would be silently dropped. The access token itself lives only in the `session` signal: nothing persists it across a page reload on purpose, because the whole point is that it's short-lived and cheap to lose.

### Attaching the token: a functional interceptor

Angular's `HttpInterceptorFn` (functional interceptors, registered via `provideHttpClient(withInterceptors(...))`) replaced the class-based `HttpInterceptor` as the default form years ago, and it's what `ng generate interceptor` scaffolds now. It's also a plain function, which makes the retry-on-401 logic easier to follow than the class version ever was:

```typescript
// src/auth-client/src/app/auth/auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  if (!req.url.startsWith(API_BASE_URL)) {
    return next(req);
  }

  const token = auth.accessToken();
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` }, withCredentials: true })
    : req.clone({ withCredentials: true });

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      const isAuthEndpoint = req.url.includes('/api/auth/');
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint) {
        return auth.refresh().pipe(
          switchMap((refreshed) => {
            if (!refreshed) return throwError(() => error);
            const retryToken = auth.accessToken();
            return next(req.clone({ setHeaders: { Authorization: `Bearer ${retryToken}` }, withCredentials: true }));
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
```

The `isAuthEndpoint` check matters: without it, a failed login (which correctly returns 401 for a bad password) would trigger a refresh attempt, which would also fail, and the user would see a confusing error about a token refresh instead of "wrong password." Only a 401 from a *protected resource* should trigger the refresh-and-retry dance.

### Guarding routes without a bounce-to-login flicker

A page reload clears the `session` signal, since it was only ever in memory. The naive guard would see `isAuthenticated() === false` immediately after every reload and redirect to `/login`, even for a user whose refresh cookie is still good for another six days. The guard below tries the refresh first:

```typescript
// src/auth-client/src/app/auth/auth.guard.ts
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return auth.refresh().pipe(map((refreshed) => refreshed || router.createUrlTree(['/login'])));
};
```

Returning a `UrlTree` from a `CanActivateFn` is how you redirect without a separate `router.navigate` call and a race against the guard's own return value - the router treats a returned `UrlTree` as "redirect here instead."

## Running it

```
git clone https://github.com/russfrith/russfrith-blog.git
cd russfrith-blog/companion-code/angular-authentication-with-dotnet
docker compose up --build
```

Then open `http://localhost:4200` and log in with `demo@example.com` / `correct-horse-battery-staple`. The profile page has a button that calls the protected `/api/me` endpoint directly, so you can watch the interceptor attach the bearer token in your browser's network tab - and if you wait out the 5-minute access token expiry and click it again, watch the 401-refresh-retry cycle happen without you doing anything.

## What this doesn't cover

This post is deliberately narrow. A few things it leaves out, on purpose:

- **OAuth/OIDC with a third-party identity provider** (Auth0, Entra ID, Google). The token handling on the Angular side looks similar once you have an access token, but getting that token involves a redirect-based authorization code flow this post doesn't touch.
- **CSRF on the refresh endpoint.** Because `localhost:4200` and `localhost:5268` are same-site, a malicious same-site page could trigger a `POST /api/auth/refresh` and force a token rotation the legitimate client doesn't know about - at worst a denial-of-service against your own session, not a credential theft, since the attacker's page can't read the JSON response across origins. A production deployment on a real domain, with the API and SPA on different registrable domains, would need an explicit CSRF defense (a double-submit token, or `SameSite=Strict` if your flows allow it) for that endpoint specifically.
- **Multi-tab logout.** Logging out in one tab doesn't notify other open tabs in this demo; a real app typically handles that with a `BroadcastChannel` or a storage event.

## References

1. [RFC 7519: JSON Web Token (JWT)](https://www.rfc-editor.org/rfc/rfc7519)
