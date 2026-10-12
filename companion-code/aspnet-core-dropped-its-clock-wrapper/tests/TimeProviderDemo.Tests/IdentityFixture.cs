using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TimeProviderDemo.Identity;

namespace TimeProviderDemo.Tests;

/// <summary>
/// Builds a real (SQLite, in-memory) Identity stack with a <see cref="FakeTimeProvider"/>
/// registered in DI, so lockout, security-stamp, and token tests exercise the actual
/// ASP.NET Core Identity code paths rather than mocks.
/// </summary>
public sealed class IdentityFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public FakeTimeProvider TimeProvider { get; }
    public ServiceProvider Services { get; }
    private readonly IServiceScope _scope;

    public IdentityFixture(Action<IdentityOptions>? configureIdentity = null, Action<IServiceCollection>? configureServices = null)
    {
        TimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddDataProtection();

        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                configureIdentity?.Invoke(options);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<TimeProviderTokenProvider<AppUser>>(TimeProviderTokenProviderName);

        configureServices?.Invoke(services);

        Services = services.BuildServiceProvider();
        _scope = Services.CreateScope();
        _scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public const string TimeProviderTokenProviderName = "TimeProviderDemo";

    public T GetRequiredService<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        _scope.Dispose();
        Services.Dispose();
        _connection.Dispose();
    }
}
