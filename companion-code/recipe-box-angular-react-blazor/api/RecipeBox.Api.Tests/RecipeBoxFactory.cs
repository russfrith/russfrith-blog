using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RecipeBox.Api.Tests;

// Each instance gets its own temp SQLite file and temp image folder, so
// tests never share state. Program.cs's own startup (migrate + seed)
// runs unmodified against these temp paths.
public class RecipeBoxFactory(string environment = "Testing") : WebApplicationFactory<Program>
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "recipebox-tests-" + Guid.NewGuid());

    public string ImageRoot => Path.Combine(_tempRoot, "images");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_tempRoot);
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RecipeBox"] = $"Data Source={Path.Combine(_tempRoot, "recipebox.db")}",
                ["Storage:SeedRoot"] = FindSeedRoot(),
                ["Storage:ImageRoot"] = Path.Combine(_tempRoot, "images"),
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private static string FindSeedRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "seed");
            if (File.Exists(Path.Combine(candidate, "recipes.json")))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find seed/recipes.json above the test assembly.");
    }
}
