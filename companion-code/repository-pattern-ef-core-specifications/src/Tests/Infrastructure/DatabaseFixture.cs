using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepositoryPattern.Infrastructure;
using RepositoryPattern.Infrastructure.Persistence;
using Xunit;

namespace RepositoryPattern.Tests.Infrastructure;

// Shared across every test in the "Database" collection: builds the DI
// container once, applies migrations once, and truncates the tables before
// each test gets its own isolated slate. Relies on InternalsVisibleTo to
// reach OrderingDbContext - tests are allowed to see it, nothing else is.
public sealed class DatabaseFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestDatabase.ConnectionString);
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE order_lines, orders CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
