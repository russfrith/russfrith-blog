---
title: "ASP.NET Core Dropped Its Own Clock Wrapper. You Should Too."
description: "(Unless It Means Something): why pass-through IClock wrappers are redundant now that TimeProvider is wired into ASP.NET Core Identity, and exactly how far that wiring actually goes as of .NET 11 RC1."
date: 2026-10-11T00:00:00-04:00
draft: true
cover:
  image: pexels-alexasfotos-34067773.jpg
  alt: "Colorful retro alarm clocks lined up in a row"
  caption: "Photo by [Alexas Fotos](https://www.pexels.com/@alexasfotos/) on [Pexels](https://www.pexels.com/photo/colorful-retro-alarm-clocks-in-a-row-34067773/)"
ShowToc: true
tags: ["dotnet", "csharp", "aspnetcore"]
categories: ["programming"]
---

{{< aside >}}
.NET 11 is currently in Release Candidate 1 (shipped September 8, 2026), with general availability expected at .NET Conf in November 2026. Everything below was built and tested against the .NET 11.0.100-rc.1.26425.128 SDK, running inside the official `mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1` Docker image. Some of this could still change before GA; see the [companion code](https://github.com/russfrith/russfrith-blog/tree/master/companion-code/aspnet-core-dropped-its-clock-wrapper) for the runnable version of every test referenced here.
{{< /aside >}}

Here's a test that doesn't sleep:

```csharp
// TimeProviderDemo.Tests.LockoutTests.Lockout_clears_once_fake_time_advances_past_lockout_window
[Fact]
public async Task Lockout_clears_once_fake_time_advances_past_lockout_window()
{
    using var fixture = new IdentityFixture();
    var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

    var user = new AppUser { UserName = "unlocks@example.com", Email = "unlocks@example.com" };
    await userManager.CreateAsync(user);

    for (var i = 0; i < 3; i++)
    {
        await userManager.AccessFailedAsync(user);
    }

    Assert.True(await userManager.IsLockedOutAsync(user));

    // No Thread.Sleep: advance the fake clock past the 5-minute lockout window.
    fixture.TimeProvider.Advance(TimeSpan.FromMinutes(6));

    Assert.False(await userManager.IsLockedOutAsync(user));
}
```

Lock a user out, advance a fake clock six minutes, assert they're unlocked. No `Thread.Sleep`, no wall-clock wait, no flaky CI run because the lockout window happened to be five minutes and the test needed six. That's `UserManager.IsLockedOutAsync` and `AccessFailedAsync`, ASP.NET Core Identity's actual lockout check, reading time from a `TimeProvider` I registered in the test's DI container.

I want to tell you this has worked since .NET 8, when `TimeProvider` first showed up in ASP.NET Core. It hasn't. I checked the previous three releases directly in the source, .NET 8, 9, and 10[[4](#references)], and in all three, `IsLockedOutAsync` and `AccessFailedAsync` call `DateTimeOffset.UtcNow` directly. No hook, no DI, nothing to fake. The test above only passes on .NET 11 RC1, where that lockout code got a private `UtcNow()` helper that checks the DI container for a `TimeProvider` before falling back to the real clock. I went looking expecting the source to just confirm what I already believed, and it didn't.

That's the real news, and it's narrower than it sounds. We'll get back to exactly how narrow, after a detour through what `TimeProvider` gives you outside Identity entirely.

{{< aside >}}
Many people did not start using `TimeProvider` when it shipped with .NET 8, and I may or may not be one of them. If an `IClock` wrapper already worked, there was no forcing function to rip it out for a BCL type that did the same job with extra ceremony. The forcing function, as it turns out, was never going to come from my own code. My `IClock` could wrap anything I'd written myself, but it was never going to reach into `UserManager` and change what `DateTimeOffset.UtcNow` meant inside the framework.
{{< /aside >}}

## Why everyone wrote one of these

Before `TimeProvider`, there wasn't a shared, testable abstraction for "now" in the BCL. `DateTime.UtcNow` is a static property; you can't substitute it, can't advance it, can't make a test assert "five minutes later" without an actual five minutes or a mocking framework reaching for something it has no business touching. So everyone wrote the same ten lines: an `IClock` or `ISystemClock` or `IDateTimeProvider` interface, one real implementation, one fake. ASP.NET Core itself did this (`Microsoft.AspNetCore.Authentication.ISystemClock`) for exactly the same reason, years before anyone outside the framework team needed to care.

{{< aside >}}
**A short, accurate timeline.** ASP.NET Core marked `ISystemClock` (and `SystemClock`, and the authentication handler constructors that took one) `[Obsolete]` in **.NET 8 Preview 5**[[1](#references)]. That's "obsoleted," not "removed." It still compiles, it still sits in the DI container, it just warns. The same breaking-change doc says Microsoft "may" remove it in a future version; as of the .NET 11 RC1 source I checked, it's still there. Don't read anything stronger into this than what actually happened: one framework-internal interface got a compiler warning, and a new BCL type, `System.TimeProvider`, became the thing ASP.NET Core actually reads time from in several, not all, of its time-dependent components.
{{< /aside >}}

## The deep dive: TimeProvider, the general case

`TimeProvider` is the **general-purpose clock abstraction**: anywhere you'd otherwise call `DateTime.UtcNow` or `DateTimeOffset.UtcNow`, from business logic to timers, `TimeProvider` is the injectable, fakeable replacement. It's worth knowing the whole surface because it's smaller than you'd expect, and it quietly plugs into async APIs you already use.

Timestamps, the obvious part:

```csharp
// TimeProviderDemo.Tests.TimeProviderSurfaceTests.GetUtcNow_reflects_the_starting_instant_and_subsequent_advances
var start = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
var timeProvider = new FakeTimeProvider(start);

Assert.Equal(start, timeProvider.GetUtcNow());

timeProvider.Advance(TimeSpan.FromHours(3));

Assert.Equal(start + TimeSpan.FromHours(3), timeProvider.GetUtcNow());
```

Less obvious: `Task.Delay`, `Task.WaitAsync`, `CancellationTokenSource`, and `PeriodicTimer` all grew `TimeProvider` overloads, specifically so tests could drive them without a real clock:

```csharp
// TimeProviderDemo.Tests.TimeProviderSurfaceTests.Task_Delay_with_a_fake_time_provider_completes_once_advanced_past_the_delay
var timeProvider = new FakeTimeProvider();
var delayTask = Task.Delay(TimeSpan.FromMinutes(5), timeProvider);

Assert.False(delayTask.IsCompleted);

timeProvider.Advance(TimeSpan.FromMinutes(5));

await delayTask.WaitAsync(TimeSpan.FromSeconds(5));
Assert.True(delayTask.IsCompleted);
```

```csharp
// TimeProviderDemo.Tests.TimeProviderSurfaceTests.CancellationTokenSource_with_a_fake_time_provider_cancels_once_advanced_past_the_delay
var timeProvider = new FakeTimeProvider();
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10), timeProvider);

var tcs = new TaskCompletionSource();
cts.Token.Register(() => tcs.TrySetResult());

timeProvider.Advance(TimeSpan.FromSeconds(10));

await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
Assert.True(cts.Token.IsCancellationRequested);
```

That's a retry policy, a timeout, a debounce, or a background poller, all testable in milliseconds of real wall-clock time instead of the seconds-to-minutes your production timeouts actually need.

That's `TimeProvider` in general. Now back to Identity, where this post started: how far does that general wiring actually reach?

## What's actually testable, by version

Here's what's actually testable with a fake clock, as of .NET 11 RC1, grouped by status. Some of these are backed by a `FakeTimeProvider`-driven test that ran green in Docker; the rest come from reading the source directly in .NET 8, 9, 10, and 11 RC1[[4](#references)], since the behavior in question is either "calls `TimeProvider.GetUtcNow()`" or "calls `DateTimeOffset.UtcNow`." I've marked which is which below.

**Already worked since .NET 8, and still does on RC1:**

- Cookie / OAuth / OIDC / JWT auth expiration, because `AuthenticationSchemeOptions` got a settable `TimeProvider` property. (Source-verified; not covered by a test in the companion code.)
- Security-stamp re-validation (cookie renew/reject), because `SecurityStampValidatorOptions.TimeProvider` did too, auto-populated from DI since 8.0 GA. (Tested.)
- Identity bearer tokens from `MapIdentityApi`, for the same reason as cookie auth above. (Source-verified only; I tested this directly in an earlier pass with a full in-process web host, confirmed it, then cut that test because it added a lot of weight for a claim that reduces to the same mechanism as cookie auth above.)

**New in .NET 11:**

- Identity lockout (`IsLockedOutAsync` / `AccessFailedAsync`). (Tested.) I was wrong when I first assumed this had worked since .NET 8, until I checked the actual source at each release and found the DI-aware `UtcNow()` helper only exists starting at .NET 11 RC1[[4](#references)], not before.

**Still a gap, even on RC1:**

- Password reset, email confirmation, and change-email tokens. (Tested.) `DataProtectorTokenProvider` (the class behind `GeneratePasswordResetTokenAsync`, `GenerateEmailConfirmationTokenAsync`, and `GenerateChangeEmailTokenAsync`) calls `DateTimeOffset.UtcNow` directly, with no `TimeProvider` hook anywhere in it.
- TOTP 2FA codes (authenticator apps). (Source-verified only, for the same reason as bearer tokens above: confirming it meant reimplementing RFC 6238 from scratch, since Identity's own TOTP class is `internal`, and that reimplementation taught the reader nothing about `TimeProvider`.) No `TimeProvider` hook exists anywhere in the class that computes them.

Here's the first of those gaps, proven rather than asserted:

```csharp
// TimeProviderDemo.Tests.TokenExpiryGapTests.Password_reset_token_ignores_fake_time_advancing_past_its_lifespan
[Fact]
public async Task Password_reset_token_ignores_fake_time_advancing_past_its_lifespan()
{
    using var fixture = new IdentityFixture();
    var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

    var user = new AppUser { UserName = "reset-token@example.com", Email = "reset-token@example.com" };
    await userManager.CreateAsync(user);

    var token = await userManager.GeneratePasswordResetTokenAsync(user);

    // Default token lifespan is 1 day. Advance the FAKE clock 10 days.
    fixture.TimeProvider.Advance(TimeSpan.FromDays(10));

    var isValid = await userManager.VerifyUserTokenAsync(
        user, TokenOptions.DefaultProvider, "ResetPassword", token);

    // Still valid: the provider reads the real wall clock, not our fake one.
    Assert.True(isValid);
}
```

That assertion is `True` after advancing the fake clock ten days past the token's one-day lifespan. If `DataProtectorTokenProvider` honored `TimeProvider`, this would be `False`. It isn't, because it can't be: there's nothing in that class to fake. Same story for TOTP 2FA codes: `Rfc6238AuthenticationService`, the internal class behind authenticator-app codes, is static and reads `DateTimeOffset.UtcNow` with no parameter or hook of any kind.

**Reconciling this with the docs.** The .NET 11 "what's new" page doesn't mention Identity or `TimeProvider` at all, and the ASP.NET Core 11 release notes have no Identity section as of RC1[[2](#references)]. So there's no official claim to contradict here. I went looking for one and didn't find it. If you've seen a doc or blog post claiming Identity now uses `TimeProvider` for "all" time-related operations, I'd treat that as aspirational or describing a different, narrower change (lockout) rather than literal, and I'd want to see which specific API it means before repeating it. What I can tell you, dated: as of .NET 11 RC1, lockout is testable with a fake clock, token expiration and TOTP are not, and I'm not promising any of this changes by GA.

## The gap is real, but it's not unworkable

`DataProtectorTokenProvider` is what actually produces the string you see in a password reset link or an email confirmation callback: a payload (the user's ID, plus the timestamp it was created) encrypted with ASP.NET Core's data protection APIs, which Identity later decrypts and checks against `Options.TokenLifespan` to decide whether it's still good. That timestamp check is where `DateTimeOffset.UtcNow` gets called directly, and the class has no seam, no point where you can swap in different behavior without touching its own source, so there's nothing to inject a `TimeProvider` into.

I needed a password-reset-style token I could actually test, so I wrote one. `TimeProviderTokenProvider<TUser>` isn't a replacement for `DataProtectorTokenProvider`; it's registered under its own name as an additional provider, so anything already using the default password-reset or email-confirmation flow keeps working exactly as before. This is the wrapper that earns its keep, not because it makes testing easier in the abstract, but because it closes a specific, source-verified hole:

```csharp
// TimeProviderDemo.Tests.TimeProviderTokenProviderTests.Token_expires_once_fake_time_advances_past_its_lifespan
[Fact]
public async Task Token_expires_once_fake_time_advances_past_its_lifespan()
{
    using var fixture = new IdentityFixture();
    var userManager = fixture.GetRequiredService<UserManager<AppUser>>();

    var user = new AppUser { UserName = "custom-token-expired@example.com", Email = "custom-token-expired@example.com" };
    await userManager.CreateAsync(user);

    var token = await userManager.GenerateUserTokenAsync(
        user, IdentityFixture.TimeProviderTokenProviderName, "test-purpose");

    // Default lifespan in TimeProviderTokenProvider is 3 hours.
    fixture.TimeProvider.Advance(TimeSpan.FromHours(4));

    var isValid = await userManager.VerifyUserTokenAsync(
        user, IdentityFixture.TimeProviderTokenProviderName, "test-purpose", token);

    // Unlike the built-in provider, this one DOES honor the fake clock: it's expired.
    Assert.False(isValid);
}
```

`TimeProviderTokenProvider<TUser>` implements `IUserTwoFactorTokenProvider<TUser>` directly rather than subclassing the sealed-in-practice `DataProtectorTokenProvider`. It protects a payload (user ID plus an issue timestamp from `TimeProvider.GetUtcNow()`) with `IDataProtectionProvider`, and checks that timestamp against `TimeProvider.GetUtcNow()` again on validation. Register it with `.AddTokenProvider<TimeProviderTokenProvider<AppUser>>(name)` on the `IdentityBuilder`, and `GenerateUserTokenAsync`/`VerifyUserTokenAsync` route to it like any other provider. It's maybe sixty lines. I'd rather maintain that than wait for a framework fix that may or may not land.

## Side by side: the wrapper you don't need

Here's the same trial-subscription check, tested the old way and the new way:

```csharp
// TimeProviderDemo.Tests.PassThroughVsTimeProviderTests.SubscriptionWithClock_expires_once_the_fake_clock_passes_the_trial_length
var clock = new FakeClock { UtcNow = DateTimeOffset.Parse("2026-01-01T00:00:00Z") };
var subscription = new SubscriptionWithClock(clock, clock.UtcNow, TimeSpan.FromDays(14));

clock.UtcNow = clock.UtcNow.AddDays(15);

Assert.True(subscription.IsTrialExpired());
```

```csharp
// TimeProviderDemo.Tests.PassThroughVsTimeProviderTests.SubscriptionWithTimeProvider_expires_once_fake_time_passes_the_trial_length
var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
var subscription = new SubscriptionWithTimeProvider(timeProvider, timeProvider.GetUtcNow(), TimeSpan.FromDays(14));

timeProvider.Advance(TimeSpan.FromDays(15));

Assert.True(subscription.IsTrialExpired());
```

Same assertion, same shape, but the second version didn't need me to write `FakeClock`. `FakeTimeProvider` is a NuGet package, maintained by the same team that maintains the thing it's faking, with `Advance`, `SetUtcNow`, `AutoAdvanceAmount`, and timer integration already built. My `IClock` gave me one property and nothing else.

## And the wrapper that does

`BusinessCalendar` wraps `TimeProvider` for a reason `TimeProvider` can't do itself: it carries a tenant's time zone and a definition of "business day."

The extension method it's built on is the smallest useful example of adding meaning on top of `TimeProvider` instead of just forwarding it:

```csharp
// TimeProviderExtensions.cs
public static DateTimeOffset GetLocalNow(this TimeProvider timeProvider, TimeZoneInfo timeZone) =>
    TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
```

```csharp
// TimeProviderDemo.Tests.BusinessCalendarTests.NowInTenantTime_converts_fake_utc_time_into_the_tenants_zone
[Fact]
public void NowInTenantTime_converts_fake_utc_time_into_the_tenants_zone()
{
    // 2026-06-15T02:30:00Z is 2026-06-14T22:30:00 Eastern (EDT, UTC-4) the evening before.
    var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-06-15T02:30:00Z"));
    var calendar = new BusinessCalendar(timeProvider, Eastern);

    var tenantNow = calendar.NowInTenantTime();

    Assert.Equal(new DateTime(2026, 6, 14, 22, 30, 0), tenantNow.DateTime);
}
```

`GetLocalNow` by itself is a one-liner. `BusinessCalendar` builds on it to answer a real domain question `TimeProvider` has no concept of, which day comes next for invoicing or scheduling purposes:

```csharp
// TimeProviderDemo.Tests.BusinessCalendarTests.NextBusinessDay_skips_the_weekend
[Fact]
public void NextBusinessDay_skips_the_weekend()
{
    // Friday 2026-06-12, late in the Eastern day.
    var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-06-12T23:00:00Z"));
    var calendar = new BusinessCalendar(timeProvider, Eastern);

    var next = calendar.NextBusinessDay();

    Assert.Equal(DayOfWeek.Monday, next.DayOfWeek);
}
```

`TimeProvider.LocalTimeZone` is whatever zone the machine (or container) is set to. In most CI runners and server deployments, that's UTC, full stop. It has no idea what "local" means to a tenant in another time zone, and no idea Saturday and Sunday aren't business days for your invoicing logic. Run the verdict's own test on it: delete `BusinessCalendar` and inline `TimeProvider` everywhere it's used, and `NextBusinessDay` has nowhere to live, no tenant zone to convert into, no weekend definition to check against. That's a wrapper worth keeping.

## Pros, cons, and one thing the analyzer catches

The case for deleting pass-through wrappers is everything above: `FakeTimeProvider` is maintained for you, it plugs into `Task.Delay` and friends for free, and the framework itself increasingly reads from the same abstraction.

The honest downsides:

- **`TimeProvider` is abstract with real surface area**, not a one-member interface. A minimal subclass only has to override `GetUtcNow()`, but `LocalTimeZone`, `GetTimestamp()`, `TimestampFrequency`, and `CreateTimer()` all come along for free whether you think about them or not.
- **`LocalTimeZone` defaults to the machine's zone**, confirmed by `Assert.Equal(TimeZoneInfo.Local, TimeProvider.System.LocalTimeZone)` passing. That's not a business concept, and code that assumes otherwise will behave differently on a developer's laptop than in a UTC container.
- **`PeriodicTimer` coalesces catch-up ticks.** Advance a `FakeTimeProvider` by more than one period, and you get one tick, not one per elapsed period:

```csharp
// TimeProviderDemo.Tests.TimeProviderSurfaceTests.PeriodicTimer_with_a_fake_time_provider_ticks_once_per_period_advanced (excerpt)
timeProvider.Advance(TimeSpan.FromMilliseconds(500)); // less than one period: no tick yet
Assert.False(secondTick.IsCompleted);

timeProvider.Advance(TimeSpan.FromSeconds(2)); // more than one period: exactly one tick, not two
Assert.True(await secondTick);
```

And the pass-through wrapper itself doesn't fully disappear. If you have one left for something genuinely domain-shaped, you still need exactly one call to the real clock inside it, which means exactly one spot a banned-API analyzer has to tolerate:

```csharp
// SystemClock.cs: the one legitimate call site, suppressed deliberately
public sealed class SystemClock : IClock
{
#pragma warning disable RS0030
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
#pragma warning restore RS0030
}
```

`Microsoft.CodeAnalysis.BannedApiAnalyzers`, pointed at a `BannedSymbols.txt` banning `DateTime.UtcNow` and `DateTimeOffset.UtcNow`, catches every other call site in the codebase, including, when I first wired it up, inside my own pass-through wrapper's implementation, which is exactly the kind of mistake it's there to catch everywhere except the one place it has to exist.

## Verdict

Delete the pass-through wrapper. If all it ever did was forward `DateTimeOffset.UtcNow` so a test could fake it, `TimeProvider` and `FakeTimeProvider` do that job better, with more framework support, for free. Keep the wrapper that means something: a business calendar, a tenant's time zone, a token provider that closes a gap the framework hasn't closed yet. The test for which category a wrapper falls into is simple: if you deleted it and inlined `TimeProvider` everywhere it's used, would anything about your domain logic get harder to read or wrong to write? If no, delete it. If yes, that's the wrapper worth keeping.

## Migration checklist

- Replace `DateTime.UtcNow` / `DateTimeOffset.UtcNow` call sites with an injected `TimeProvider`, starting with anything already covered by a test.
- Add `Microsoft.CodeAnalysis.BannedApiAnalyzers` with a `BannedSymbols.txt` banning both, so new call sites can't sneak back in.
- Delete any `IClock`/`ISystemClock`-style interface whose only job was forwarding to the real clock for testability, unless it's also carrying real domain meaning (time zone, business calendar, a token provider's expiry logic), in which case keep it and build it on `TimeProvider` rather than `DateTimeOffset.UtcNow` directly.
- If you're on Identity and need lockout testable with a fake clock, that requires .NET 11; confirm your target framework before promising it to a test author on an older version.
- If you need password-reset, email-confirmation, or TOTP tokens testable with a fake clock on any current version, you'll need your own token provider. `TimeProviderTokenProvider<TUser>` in the companion code is a working starting point, not Identity's built-in one.
- Don't assume `TimeProvider.LocalTimeZone` means anything about your users; wrap it if you need tenant-aware time zones.

## References

1. [Breaking change: ISystemClock is obsolete](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/isystemclock-obsolete): Microsoft Learn, ASP.NET Core breaking changes for .NET 8.
2. [.NET 11, What's new](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview) and the [ASP.NET Core 11 release notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11): checked as of RC1, no Identity/TimeProvider section present.
3. [`SecurityStampValidatorOptions.TimeProvider`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.securitystampvalidatoroptions.timeprovider): Microsoft Learn API reference.
4. [`dotnet/aspnetcore` source](https://github.com/dotnet/aspnetcore): `UserManager.cs`, `SecurityStampValidator.cs`, `DataProtectorTokenProvider.cs`, and `Rfc6238AuthenticationService.cs`, compared at the `release/8.0`, `release/9.0`, `release/10.0`, and `v11.0.0-rc.1.26425.128` tags.
5. [.NET 11 RC1 release](https://visualstudiomagazine.com/articles/2026/09/14/net-11-rc1-gets-go-live-support-ahead-of-november-launch.aspx): Visual Studio Magazine, on RC1's go-live support window and expected GA.
