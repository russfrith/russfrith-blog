# Companion code: Angular Authentication With .NET

Runnable code behind [the post](../../content/posts/angular-authentication-with-dotnet/index.md). It targets ASP.NET Core 10 (LTS) on the backend and Angular 22 on the frontend, and was built and tested against .NET SDK 10.0.401 and Node 22.

## What's here

- `src/AuthDemo.Api/`: a minimal ASP.NET Core Web API. One demo user, JWT bearer access tokens (5-minute lifetime), and an opaque, server-tracked refresh token delivered as an HttpOnly cookie with rotation on every use.
- `src/auth-client/`: a standalone, zoneless Angular 22 app. A signal-backed `AuthService`, a functional `authInterceptor` that attaches the access token and retries once through `/api/auth/refresh` on a 401, and a functional `authGuard` that redeems the refresh cookie on a cold page load instead of bouncing straight to `/login`.

This is a demo, not a template to copy into a real app wholesale:

- The JWT signing key in `src/AuthDemo.Api/appsettings.json` is checked into this repo. That's fine for a throwaway demo key; never do it with a real one - use user-secrets, an environment variable, or a key vault instead.
- The hand-rolled PBKDF2 `PasswordHasher` exists only so this demo has no dependency beyond the BCL. A real app should use `Microsoft.AspNetCore.Identity`'s `PasswordHasher<TUser>`.
- The refresh-token cookie is issued with `Secure = false` because this only ever runs over `http://localhost`. Set it `true` behind HTTPS.

## Running it

### Docker Compose (no .NET or Node SDK required locally)

```
docker compose up --build
```

This builds and starts both services:

- API on `http://localhost:5268`
- Angular dev server on `http://localhost:4200`

Open `http://localhost:4200`, log in with `demo@example.com` / `correct-horse-battery-staple`, and use the "Call protected /api/me" button on the profile page to see the bearer token attached by the interceptor. Wait 5 minutes and click it again - the access token will have expired, and the interceptor will refresh it from the cookie transparently before retrying.

Stop it with:

```
docker compose down
```

### Locally, without Docker

You need the .NET 10 SDK and Node 22+ installed.

```
# API
cd src/AuthDemo.Api
dotnet run

# Client, in a second terminal
cd src/auth-client
npm install
npm start
```

The API listens on whatever port `dotnet run` prints (check `src/AuthDemo.Api/Properties/launchSettings.json` - it defaults to `http://localhost:5268`). If it picks a different port, update `API_BASE_URL` in `src/auth-client/src/app/auth/api-config.ts` and `Client:Origin` in `src/AuthDemo.Api/appsettings.json` to match.

## Last known-good run

Verified 2026-10-12 via `docker compose up --build`: login issues an access token and sets the refresh cookie, `GET /api/me` returns 401 with no token, a CORS preflight for `/api/auth/login` returns the expected `Access-Control-Allow-*` headers, and `POST /api/auth/refresh` rotates the cookie and returns a fresh access token. See the post for what each piece demonstrates.
