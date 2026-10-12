# Companion code: ASP.NET Core Dropped Its Own Clock Wrapper. You Should Too.

Runnable code behind [the post](../../content/posts/aspnet-core-dropped-its-clock-wrapper/index.md). It targets .NET 11 and was built and tested against .NET 11.0.100-rc.1.26425.128 (RC1, pre-GA).

## What's here

- `src/TimeProviderDemo/`: the production-style code. A pass-through `IClock` wrapper, the same logic rebuilt on `TimeProvider`, a `BusinessCalendar` wrapper that adds real domain meaning, a hand-rolled `TimeProviderTokenProvider<TUser>` that closes a gap in Identity's built-in token providers, and a banned-API analyzer rule (`BannedSymbols.txt`) that flags `DateTime.UtcNow` / `DateTimeOffset.UtcNow`.
- `tests/TimeProviderDemo.Tests/`: xUnit tests, all driven by `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`), covering Identity lockout, security-stamp validation, password-reset/email-confirmation/change-email tokens, and the core `TimeProvider` surface (timers, `Task.Delay`, `WaitAsync`, `CancellationTokenSource`, `PeriodicTimer`). A few claims in the post (bearer-token expiry, TOTP) are confirmed from source rather than a test here; see the post for why.

## Running it

### Docker (no .NET 11 SDK required locally)

```
docker build -t timeproviderdemo-tests .
docker run --rm timeproviderdemo-tests
```

The image is pinned to `mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1`, confirmed present on mcr.microsoft.com as of 2026-10-11. The Dockerfile restores NuGet packages in their own layer (before the rest of the source is copied in), so rebuilding after a source-only change reuses the cached restore instead of re-downloading packages.

To run a single test class or test inside the container:

```
docker run --rm timeproviderdemo-tests dotnet test --filter "FullyQualifiedName~LockoutTests"
docker run --rm timeproviderdemo-tests dotnet test --filter "FullyQualifiedName~LockoutTests.Lockout_clears_once_fake_time_advances_past_lockout_window"
```

(These override the image's default `ENTRYPOINT`, which just runs the full suite.)

### Locally, with the .NET 11 SDK installed

```
dotnet test
```

To run one class or test:

```
dotnet test --filter "FullyQualifiedName~LockoutTests"
dotnet test --filter "FullyQualifiedName~LockoutTests.Lockout_clears_once_fake_time_advances_past_lockout_window"
```

## Last known-good run

24/24 tests passed inside the pinned Docker image on 2026-10-11. See the post for what each test proves and the version caveats.
