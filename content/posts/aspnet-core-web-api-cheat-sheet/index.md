---
title: "ASP.NET Core Web API Cheat Sheet (.NET 10 LTS, with .NET 11 Changes)"
description: "A scannable, copy-pasteable reference for building ASP.NET Core Web APIs on .NET 10, with the .NET 11 preview changes called out separately"
date: 2026-10-12T00:00:00-04:00
draft: true
aliases:
  - /posts/aspnet-cheat-sheet/
cover:
  image: christina-wocintechchat-com-glRqyWJgUeY-unsplash.jpg
  alt: "Woman standing next to server room"
  caption: "Photo by [Christina @ wocintechchat.com](https://unsplash.com/@wocintechchat?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/@wocintechchat?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyTex)"
ShowToc: true
tags: ["aspnetcore", "aspnet", "csharp", "dotnet"]
categories: ["programming"]
---

**Last updated:** October 12, 2026.

{{< aside >}}
**Version legend.** [10] marks something stable and shipped in .NET 10 (LTS, supported through November 2028). [11p] marks a .NET 11 preview feature; .NET 11 is scheduled to reach general availability on November 10, 2026, as a Standard Term Support (STS) release. Anything tagged [11p] could still change before GA, so re-verify it once .NET 11 ships. Entries with no tag apply equally to both.
{{< /aside >}}

This is a reference, not a tutorial: short, copy-pasteable snippets with a one-line note each, organized so you can scan to the part you need. Every snippet here was checked against a real `dotnet new webapi` project on the .NET 10 SDK; where I couldn't verify a .NET 11 detail against the official release notes, it's left out and listed under "Unverified" rather than guessed at.

## 1. Pick your style: controllers vs. Minimal APIs

Both are first-class in ASP.NET Core. Neither one is going away. The choice is about which tradeoffs you want.

| | Controllers | Minimal APIs |
|---|---|---|
| Structure | One class per resource, action methods, model binding via attributes | Route handlers registered directly on `WebApplication`/`RouteGroupBuilder` |
| Filters | Action filters, resource filters, the full MVC filter pipeline | Endpoint filters (`IEndpointFilter`), fewer filter types but less ceremony |
| Testing | `WebApplicationFactory<T>` + MVC-aware test helpers; action methods are directly callable | `WebApplicationFactory<T>`; handlers are often plain functions you can unit test without HTTP at all |
| Validation | `[ApiController]` gives you automatic 400s for free | Opt in with `AddValidation()` [10] |
| AOT / trimming | Not supported with Native AOT | Fully supported; this is the main reason to pick Minimal APIs if startup time and image size matter |
| Best fit | Large APIs with lots of shared conventions, existing MVC investment, teams that want the filter pipeline | Small-to-medium APIs, microservices, anything targeting Native AOT or a minimal container image |

If you don't have a specific reason to pick one, and you're not shipping Native AOT, controllers still give you more batteries-included structure for a larger API. If startup latency and image size matter (serverless, high-density containers), Minimal APIs are the better default.

## 2. Endpoint basics, side by side

Same resource, both styles.

**GET**

```csharp
// Controllers
[ApiController]
[Route("api/widgets")]
public class WidgetsController : ControllerBase
{
    [HttpGet("{id:int}")]
    public ActionResult<WidgetDto> Get(int id)
    {
        if (id <= 0) return NotFound();
        return Ok(new WidgetDto(id, "Widget"));
    }
}
```

```csharp
// Minimal APIs
app.MapGet("/api/widgets/{id:int}", (int id) =>
    id <= 0 ? Results.NotFound() : Results.Ok(new WidgetDto(id, "Widget")));
```

**POST**

```csharp
// Controllers
[HttpPost]
public ActionResult<WidgetDto> Create([FromBody] CreateWidgetRequest request)
{
    var created = new WidgetDto(1, request.Name);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
}
```

```csharp
// Minimal APIs
app.MapPost("/api/widgets", (CreateWidgetRequest request) =>
{
    var created = new WidgetDto(1, request.Name);
    return Results.Created($"/api/widgets/{created.Id}", created);
});
```

**PUT**

```csharp
// Controllers
[HttpPut("{id:int}")]
public IActionResult Update(int id, [FromBody] UpdateWidgetRequest request)
{
    if (id <= 0) return NotFound();
    return NoContent();
}
```

```csharp
// Minimal APIs
app.MapPut("/api/widgets/{id:int}", (int id, UpdateWidgetRequest request) =>
    id <= 0 ? Results.NotFound() : Results.NoContent());
```

**DELETE**

```csharp
// Controllers
[HttpDelete("{id:int}")]
public IActionResult Delete(int id) => NoContent();
```

```csharp
// Minimal APIs
app.MapDelete("/api/widgets/{id:int}", (int id) => Results.NoContent());
```

## 3. Routing and binding

Explicit binding source attributes, when the inference rules aren't enough or you want to be unambiguous:

```csharp
app.MapGet("/api/widgets/search", ([FromQuery] string? q, [FromHeader(Name = "X-Tenant-Id")] string? tenantId) =>
    Results.Ok(new { q, tenantId }));

app.MapPost("/api/widgets/{id:int}/notes", ([FromRoute] int id, [FromBody] NoteRequest note) =>
    Results.Ok(new { id, note.Text }));
```

Group related endpoints with `MapGroup`, which lets you apply a shared prefix, tag, and policies in one place instead of repeating them on every route:

```csharp
var widgets = app.MapGroup("/api/widgets")
    .WithTags("Widgets")
    .RequireAuthorization();

widgets.MapGet("/{id:int}", (int id) => Results.Ok(new WidgetDto(id, "Widget")));
widgets.MapPost("/", (CreateWidgetRequest request) => Results.Ok(request));
```

Route constraints narrow what a segment matches, so `/api/widgets/abc` 404s instead of reaching a handler that then has to reject it:

```csharp
app.MapGet("/api/widgets/{id:int}", (int id) => Results.Ok(id));            // numeric only
app.MapGet("/api/widgets/{sku:alpha}", (string sku) => Results.Ok(sku));    // letters only
app.MapGet("/api/widgets/{id:int:min(1)}", (int id) => Results.Ok(id));     // numeric, >= 1
app.MapGet("/api/widgets/{date:datetime}", (DateTime date) => Results.Ok(date));
app.MapGet("/api/widgets/{sku:regex(^[A-Z]{{3}}-\\d{{4}}$)}", (string sku) => Results.Ok(sku)); // custom pattern
```

An optional route segment with a default takes a `?` and a parameter default in the handler:

```csharp
app.MapGet("/api/widgets/{id:int}/{page:int?}", (int id, int page = 1) => Results.Ok(new { id, page }));
```

**File uploads** bind an `IFormFile` (or `IFormFileCollection` for multiple) directly as a parameter; both styles need `multipart/form-data`, and Minimal API endpoints that accept form data need `.DisableAntiforgery()` unless you've wired up the newer automatic protection described in [Auth](#8-auth):

```csharp
// Controllers
[HttpPost("{id:int}/photo")]
public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
{
    using var stream = file.OpenReadStream();
    // ... save the stream somewhere
    return Ok(new { id, file.FileName, file.Length });
}
```

```csharp
// Minimal APIs
app.MapPost("/api/widgets/{id:int}/photo", async (int id, IFormFile file) =>
{
    using var stream = file.OpenReadStream();
    // ... save the stream somewhere
    return Results.Ok(new { id, file.FileName, file.Length });
}).DisableAntiforgery();
```

## 4. Validation

**Controllers:** `[ApiController]` gives you automatic model-state validation and a 400 response for free, no extra setup. Decorate your model with `DataAnnotations` attributes and you're done:

```csharp
public record CreateWidgetRequest(
    [Required] string Name,
    [Range(0, 1000)] int Quantity);

[ApiController]
[Route("api/widgets")]
public class WidgetsController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(CreateWidgetRequest request) => Ok(request); // never runs if validation fails
}
```

**Minimal APIs:** opt in with `AddValidation()` [10]. It validates query, header, route, and body parameters using the same `DataAnnotations` attributes, and returns a `ProblemDetails` 400 before your handler runs:

```csharp
builder.Services.AddValidation();

app.MapPost("/api/widgets", (CreateWidgetRequest request) => Results.Ok(request));
```

Skip validation for a specific endpoint with `.DisableValidation()`:

```csharp
app.MapPost("/api/widgets/import", (CreateWidgetRequest request) => Results.Ok(request))
    .DisableValidation();
```

Namespace: `Microsoft.Extensions.Validation`. On .NET 10, the attributes that drive this (`[ValidatableType]`, `[SkipValidation]`) are marked experimental; as of .NET 11 [11p] they're stable, and validation gains async support via `AsyncValidationAttribute` and `IAsyncValidatableObject` for rules that need to hit a database or remote service.

## 5. Responses, errors, pagination, and concurrency

**`ActionResult<T>` (controllers) vs. `TypedResults` (Minimal APIs):** both give you strongly-typed, discoverable responses instead of untyped `IActionResult`/`IResult`:

```csharp
// Controllers
[HttpGet("{id:int}")]
public ActionResult<WidgetDto> Get(int id) =>
    id <= 0 ? NotFound() : Ok(new WidgetDto(id, "Widget"));
```

```csharp
// Minimal APIs
app.MapGet("/api/widgets/{id:int}", Results<Ok<WidgetDto>, NotFound> (int id) =>
    id <= 0 ? TypedResults.NotFound() : TypedResults.Ok(new WidgetDto(id, "Widget")));
```

The `Results<T1, T2, ...>` union return type documents every possible response shape right in the method signature, and it flows into OpenAPI generation automatically.

**`ProblemDetails`** is the standard error shape (RFC 9457) and both styles produce it the same way:

```csharp
app.MapGet("/api/widgets/unavailable", () =>
    TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Temporarily unavailable"));
```

**Status code quick reference:**

| Code | When |
|---|---|
| 200 | Successful GET/PUT with a body to return |
| 201 | Successful POST that created a resource; include `Location` |
| 204 | Successful request, nothing to return (PUT/DELETE) |
| 400 | Validation failure, malformed request |
| 401 | Not authenticated |
| 403 | Authenticated, not authorized |
| 404 | Resource doesn't exist |
| 409 | Conflict (e.g., concurrency token mismatch) |
| 422 | Semantically invalid, syntactically fine (less common than 400 in practice) |
| 503 | Dependency unavailable, try again later |

**Server-Sent Events** got a first-class, typed helper in .NET 10 [10]: `TypedResults.ServerSentEvents` streams an `IAsyncEnumerable<SseItem<T>>` as `text/event-stream`, which is the natural fit for progress updates or live ticks without the overhead of a full WebSocket:

```csharp
app.MapGet("/api/widgets/stream", (CancellationToken cancellationToken) =>
{
    async IAsyncEnumerable<SseItem<WidgetDto>> Stream()
    {
        var i = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new SseItem<WidgetDto>(new WidgetDto(i, "tick"), "widget-update");
            i++;
            await Task.Delay(2000, cancellationToken);
        }
    }

    return TypedResults.ServerSentEvents(Stream(), eventType: "widget-update");
});
```

**Pagination** for collection endpoints, so a client gets a page plus enough metadata to request the next one, instead of the whole table:

```csharp
public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount);

app.MapGet("/api/widgets", async (AppDbContext db, int page = 1, int pageSize = 20) =>
{
    var query = db.Widgets.OrderBy(w => w.Id);
    var totalCount = await query.CountAsync();
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    return Results.Ok(new PagedResult<WidgetDto>(items, page, pageSize, totalCount));
});
```

**Concurrency and ETags**, so a client can't silently overwrite a change it never saw. Produce an `ETag` from something that changes whenever the row does (a `rowversion` column, a hash, a timestamp), and check `If-Match` against it on writes:

```csharp
app.MapPut("/api/widgets/{id:int}", (int id, WidgetDto widget, HttpRequest request) =>
{
    var currentETag = $"\"{GetCurrentETagFor(id)}\"";

    if (request.Headers.IfMatch.Count > 0 && request.Headers.IfMatch[0] != currentETag)
    {
        return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
    }

    // ... apply the update, which also changes the ETag for next time
    return Results.NoContent();
});
```

A 412 here means "someone else changed this since you last read it," which is a more honest signal to the client than a silent overwrite or a generic 409.

## 6. JSON serialization

Controllers and Minimal APIs configure `System.Text.Json` through two different options types that happen to share a name, which is worth knowing before you spend ten minutes on an ambiguous-reference error (see [Gotchas](#13-gotchas)):

```csharp
// Controllers: Microsoft.AspNetCore.Mvc.JsonOptions
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

```csharp
// Minimal APIs: Microsoft.AspNetCore.Http.Json.JsonOptions
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

Both ultimately configure a `JsonSerializerOptions` instance; the wrapper type and the property name (`JsonSerializerOptions` vs. `SerializerOptions`) are just where controllers and Minimal APIs diverged historically. If your app mixes both styles, set both, or they'll serialize differently.

## 7. Global exception handling

`IExceptionHandler` [10, since .NET 8] centralizes the "turn any uncaught exception into a `ProblemDetails` response" logic that otherwise ends up copy-pasted across try/catch blocks. Register it once, and it applies to both controllers and Minimal APIs:

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
        }, cancellationToken);

        return true; // true = handled; false lets the next registered handler try
    }
}
```

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
```

Register multiple handlers if you want type-specific handling (a `NotFoundException` maps to 404, everything else falls through to a generic 500); they run in registration order, and the first one to return `true` wins. `AddProblemDetails()` is what makes the built-in 4xx/5xx responses (validation failures, routing 404s, etc.) also come back as `ProblemDetails`, so your whole API has one consistent error shape instead of two.

## 8. Auth

**JWT bearer**, the standard setup for an API that validates tokens issued elsewhere:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-issuer.example.com";
        options.Audience = "your-api";
    });

builder.Services.AddAuthorization();
```

**Policies** let you express authorization rules once and reuse them with `[Authorize(Policy = "...")]` or `.RequireAuthorization("...")`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MinimumAge", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => c.Type == "age" && int.Parse(c.Value) >= 18)));
});
```

Applying a policy looks the same in spirit on both styles, just different syntax:

```csharp
// Controllers
[Authorize(Policy = "AdminOnly")]
[HttpDelete("{id:int}")]
public IActionResult Delete(int id) => NoContent();
```

```csharp
// Minimal APIs
app.MapDelete("/api/widgets/{id:int}", (int id) => Results.NoContent())
    .RequireAuthorization("AdminOnly");
```

**Cookie-authenticated API endpoints no longer redirect to login** [10]. Previously, an unauthenticated request to a cookie-protected endpoint got a 302 redirect to the login page, which makes no sense for an API client. ASP.NET Core 10 detects API-shaped endpoints (anything with `[ApiController]`, a Minimal API with a JSON request/response, `TypedResults`, or SignalR) via `IApiEndpointMetadata`, and returns a plain 401/403 instead. If you genuinely want the old redirect behavior somewhere, you can still opt back in:

```csharp
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
```

**Automatic cross-origin protection for cookie-authenticated endpoints** [11p]. .NET 11 adds a lightweight CSRF defense based on Fetch Metadata request headers (`Sec-Fetch-Site`, `Origin`) rather than the older synchronizer-token pattern. It's on by default for apps built with `WebApplication.CreateBuilder`, runs against unsafe verbs on form-posting, cookie-authenticated requests, and doesn't apply to JSON APIs or non-browser clients, so most Web API endpoints are unaffected either way. If you're using the older antiforgery middleware and want to fully adopt the new approach, remove your `UseAntiforgery()` calls; the existing `[IgnoreAntiforgeryToken]`/`DisableAntiforgery()` opt-outs are honored the same way under the new implementation.

## 9. OpenAPI

Built-in OpenAPI document generation needs no third-party package for document generation itself:

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

Namespace: `Microsoft.AspNetCore.OpenApi`. `AddOpenApi()` registers document generation; `MapOpenApi()` exposes the generated document at `/openapi/{documentName}.json`.

**OpenAPI 3.1 is the default** in .NET 10 [10], bringing full JSON Schema 2020-12 support (nullable types render as `type: ["string", "null"]` instead of a separate `nullable: true` flag). You can pin to an explicit version:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
});
```

**The `Microsoft.OpenApi` 2.0 breaking change** [10] matters if you write custom document/operation transformers: `OpenApiAny`-family types (`OpenApiString`, `OpenApiInteger`, `OpenApiObject`) are gone, replaced by plain `System.Text.Json` types (`JsonNode`, `JsonObject`, and literals):

```csharp
// Before (Microsoft.OpenApi 1.x)
schema.Example = new OpenApiObject
{
    ["temperatureC"] = new OpenApiInteger(0)
};

// After (Microsoft.OpenApi 2.x)
schema.Example = new JsonObject
{
    ["temperatureC"] = 0
};
```

Schema and operation interfaces also moved to `IOpenApiSchema`/`IOpenApiOperation`, and `OpenApiSchema.Nullable` is gone in favor of checking `Type` against `JsonSchemaType.Null`. If you maintain a transformer, budget time for this when upgrading.

**OpenAPI 3.2 becomes the default** [11p] in .NET 11, alongside an updated `Microsoft.OpenApi` dependency. A few schema-generation details land alongside it:

- OpenAPI descriptions for binary file responses: a `FileContentResult`-shaped response now documents itself as `type: string, format: binary` instead of an undocumented blob.
- The HTTP `QUERY` method (a newer, still-draft safe method that carries a body) is recognized and documented, which requires OpenAPI 3.2's schema support for it.

**An interactive UI** isn't included; `AddOpenApi()`/`MapOpenApi()` only generate and serve the raw document. [Scalar](https://scalar.com/), via the `Scalar.AspNetCore` package, is a lightweight option that reads the document you're already generating:

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // browse to /scalar
}
```

Swashbuckle's Swagger UI (`Swashbuckle.AspNetCore`) is the older, equally common choice if you'd rather stick with it; both read the same generated OpenAPI document, so switching between them later doesn't touch your endpoints.

## 10. Cross-cutting concerns

**DI lifetimes:**

```csharp
builder.Services.AddSingleton<IWidgetCache, WidgetCache>();   // one instance, app lifetime
builder.Services.AddScoped<IWidgetService, WidgetService>();  // one instance per request
builder.Services.AddTransient<IWidgetValidator, WidgetValidator>(); // new instance every resolution
```

**Middleware order** matters; a common, correct ordering looks like:

```csharp
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.MapControllers();
```

**CORS:**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpa", policy =>
        policy.WithOrigins("https://app.example.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

app.UseCors("AllowSpa");
```

**Rate limiting:**

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 5;
    });
});

app.UseRateLimiter();

app.MapGet("/api/widgets", () => Results.Ok())
    .RequireRateLimiting("fixed");
```

**Output caching:**

```csharp
builder.Services.AddOutputCache();

app.UseOutputCache();

app.MapGet("/api/widgets/{id:int}", (int id) => Results.Ok(new WidgetDto(id, "Widget")))
    .CacheOutput();
```

**API versioning** (via the `Asp.Versioning.Http` package, not built in). Setup, then actually versioning a group of endpoints:

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
});
```

```csharp
var widgetsV1 = app.MapGroup("/api/widgets")
    .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
    .HasApiVersion(1.0);

widgetsV1.MapGet("/{id:int}", (int id) => Results.Ok(new WidgetDto(id, "Widget")));
```

**Health checks:**

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

app.MapHealthChecks("/health");
```

**Calling downstream HTTP services** via `IHttpClientFactory`, so you get pooled connections and a named/typed client instead of a `HttpClient` you manage lifetime for yourself:

```csharp
builder.Services.AddHttpClient("widgets-upstream", client =>
{
    client.BaseAddress = new Uri("https://upstream.example.com");
});

app.MapGet("/api/widgets/remote/{id:int}",
    async (int id, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
    {
        var client = httpClientFactory.CreateClient("widgets-upstream");
        var response = await client.GetAsync($"/widgets/{id}", cancellationToken);
        return Results.Ok(response.StatusCode);
    });
```

**Native AOT**, if startup time and image size matter enough to take on its constraints (reflection-based serialization and most DI container tricks are off the table; Minimal APIs only, no controllers):

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
```

## 11. Config and options

**`appsettings` layering**, lowest precedence first:

```text
appsettings.json                   # base
appsettings.{Environment}.json     # e.g. appsettings.Development.json, overrides the base
environment variables              # override both files
command-line arguments             # override everything else
```

`{Environment}` comes from `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT`, commonly `Development`, `Staging`, or `Production`.

**User secrets** for local development, so credentials never land in source control:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=.;Database=Widgets;Trusted_Connection=True"
```

**`IOptions<T>` variants**, pick based on whether you need change tracking:

```csharp
builder.Services.Configure<WidgetOptions>(builder.Configuration.GetSection("Widgets"));
```

| Interface | Use when |
|---|---|
| `IOptions<T>` | Singleton consumers; snapshot taken once, never refreshed |
| `IOptionsSnapshot<T>` | Scoped consumers that want fresh values reloaded per-request on config change |
| `IOptionsMonitor<T>` | Singleton consumers that need live updates and/or a change-notification callback |

## 12. Testing

`WebApplicationFactory<T>` spins up your app in-memory for integration tests, no real network socket needed:

```csharp
public class WidgetsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WidgetsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_returns_200_for_valid_id()
    {
        var response = await _client.GetAsync("/api/widgets/1");
        response.EnsureSuccessStatusCode();
    }
}
```

Override services (a fake database, a stubbed external client) with `WithWebHostBuilder`:

```csharp
var factory = baseFactory.WithWebHostBuilder(builder =>
{
    builder.ConfigureServices(services =>
    {
        services.RemoveAll<IWidgetService>();
        services.AddSingleton<IWidgetService, FakeWidgetService>();
    });
});
```

## 13. Gotchas

- **Middleware order is load-bearing.** `UseAuthentication()` must come before `UseAuthorization()`, full stop; swap them and every authorized endpoint silently 403s because there's no authenticated user yet to check.
- **Captive dependencies.** Injecting a scoped or transient service into a singleton captures it for the app's entire lifetime, defeating its lifetime and potentially leaking a `DbContext` or similar across requests. Resolve scoped services inside a request scope (`IServiceScopeFactory.CreateScope()`), not at singleton construction time.
- **`async`/`await` and `CancellationToken`.** Accept a `CancellationToken` parameter in handlers that do I/O and pass it all the way down; a request that's been canceled client-side should stop doing work server-side, not run to completion into a response nobody reads.
- **Minimal API lambdas capturing scoped services incorrectly.** A `static` lambda can't capture instance state, which is a feature: it forces you to take dependencies as parameters (resolved via DI per-invocation) instead of accidentally closing over something with the wrong lifetime.
- **Two `JsonOptions` types, same name.** `Microsoft.AspNetCore.Mvc.JsonOptions` (controllers) and `Microsoft.AspNetCore.Http.Json.JsonOptions` (Minimal APIs) are different types that happen to share a name; `using` both namespaces in the same file gives you `CS0104: 'JsonOptions' is an ambiguous reference`. Fully qualify whichever one you mean, or configure both in separate files.

## 14. The .NET 11 delta

Everything here is [11p]: a .NET 11 preview feature, unconfirmed to be final until GA.

- **C# unions as JSON request/response bodies**, reflected in OpenAPI as an `anyOf` schema. A Minimal API endpoint can return a union type directly and have both the JSON shape and the OpenAPI document reflect each case.
- **OpenAPI descriptions for binary file responses**, so a file-download endpoint documents itself as `type: string, format: binary` instead of leaving the response body undocumented.
- **The HTTP `QUERY` method is recognized in OpenAPI**, documenting a safe, body-carrying alternative to `GET` for complex query payloads.
- **`IOutputCachePolicyProvider`** for selecting an output-cache policy dynamically at runtime, following the same provider pattern as `IAuthorizationPolicyProvider` and `ICorsPolicyProvider`, instead of only choosing from policies registered at startup.

## 15. Upgrade checklist

**.NET 10 → .NET 11** (once .NET 11 reaches GA):

- Re-verify every item in the ".NET 11 delta" section above against the final release notes; preview behavior can and does change before GA.
- If you maintain custom OpenAPI transformers, check for further `Microsoft.OpenApi` changes beyond the 2.0 migration you already did for .NET 10.
- If you rely on cookie authentication with form posts, read up on the automatic cross-origin protection before upgrading; it's on by default and changes what gets rejected.
- If you use `[ValidatableType]`/`[SkipValidation]`, drop any `#pragma warning disable ASP0029` or `<NoWarn>` suppressions you added for .NET 10's experimental-API warning; both attributes are stable in .NET 11.

**.NET 8 or .NET 9 → .NET 10:**

- Both .NET 8 and .NET 9 reach end of support on November 10, 2026, so this isn't optional on any particular timeline; .NET 10 is an LTS release supported through November 2028.
- Adopt `AddValidation()` for Minimal APIs if you were hand-rolling validation or relying on nothing at all.
- Pick up automatic 401/403 responses for cookie-authenticated API endpoints; if you depended on the old redirect-to-login behavior for an API client, you'll need the `OnRedirectToLogin` override shown above.
- If you have custom OpenAPI transformers, do the `Microsoft.OpenApi` 2.0 migration (`OpenApiAny` types → `JsonNode`/plain values) before you upgrade the package reference, not after.
- Re-run your test suite against OpenAPI 3.1 output if anything downstream (codegen, a client generator, a contract test) parses the generated document and expects 3.0's `nullable: true` convention.

## Unverified

A few things mentioned in early .NET 11 coverage that I couldn't confirm against official, current release notes, so they're deliberately left out of the sections above:

- Any claim that ASP.NET Core Identity broadly uses `TimeProvider` for all time-related operations as of .NET 11; what I could confirm elsewhere is narrower than that (see my [other post on this](/posts/aspnet-core-dropped-its-clock-wrapper/) for the specifics).
- The exact final shape of the `[JsonUnion]`/structural classifier API for disambiguating union cases with identical JSON shapes; early preview coverage describes it but I didn't find it nailed down in release notes I'd cite with confidence.
- Precise GA behavior of the automatic cross-origin protection feature; preview 6 behavior is described above, but this is exactly the kind of security-surface feature likely to see adjustments before release.
