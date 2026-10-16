using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using RecipeBox.Api;
using RecipeBox.Api.Endpoints;
using RecipeBox.Api.Images;
using RecipeBox.Api.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Comfortably above 5 MB + multipart overhead, so a 5 MB upload is rejected
// by the application's own check (400), not Kestrel's transport limit (413).
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAntiforgery();

// Resolved from DI (not read into a local here) so that test-host
// configuration overrides, which only land after the host is built,
// are the values actually used — reading builder.Configuration directly
// at this point would freeze in the pre-override defaults.
builder.Services.AddDbContext<RecipeDbContext>((sp, o) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    o.UseSqlite(config["ConnectionStrings:RecipeBox"] ?? "Data Source=/data/recipebox.db");
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new SeedPaths(
        config["Storage:SeedRoot"] ?? "/app/seed",
        config["Storage:ImageRoot"] ?? "/data/images");
});
builder.Services.AddSingleton<ImageStore>();

var app = builder.Build();

var seedPaths = app.Services.GetRequiredService<SeedPaths>();

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Recipes.AnyAsync())
        await Seeder.SeedAsync(db, seedPaths, default);
}

app.MapOpenApi();

Directory.CreateDirectory(seedPaths.ImageRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(seedPaths.ImageRoot),
    RequestPath = "/api/images",
});

app.MapRecipeEndpoints();
app.MapImageEndpoints();
app.MapTagEndpoints();
app.MapTestEndpoints();

app.Run();

public partial class Program;
