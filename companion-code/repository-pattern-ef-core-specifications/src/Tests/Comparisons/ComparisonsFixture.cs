using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepositoryPattern.Comparisons.Shared;
using RepositoryPattern.Infrastructure;
using RepositoryPattern.Infrastructure.Persistence;
using RepositoryPattern.Tests.Infrastructure;
using Xunit;

namespace RepositoryPattern.Tests.Comparisons;

// Wires up all five options against the same Postgres tables so the
// equivalence test can run them side by side.
public sealed class ComparisonsFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestDatabase.ConnectionString);
        services.AddDbContext<ComparisonDbContext>(options =>
            options.UseNpgsql(TestDatabase.ConnectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<RepositoryPattern.Comparisons.Option1_RawDbContext.OpenOrdersScenario>();
        services.AddScoped<RepositoryPattern.Comparisons.Option2_QueryObjects.GetOpenOrdersForCustomer>();
        services.AddScoped<RepositoryPattern.Comparisons.Option2_QueryObjects.SubmitOpenOrderHandler>();
        services.AddScoped<RepositoryPattern.Comparisons.Option3_ClassicRepository.IOrderRepository,
            RepositoryPattern.Comparisons.Option3_ClassicRepository.OrderRepository>();
        services.AddScoped<RepositoryPattern.Comparisons.Option3_ClassicRepository.SubmitOpenOrderHandler>();
        services.AddScoped(typeof(RepositoryPattern.Comparisons.Option4_SpecificationRepository.IRepository<>),
            typeof(RepositoryPattern.Comparisons.Option4_SpecificationRepository.Repository<>));
        services.AddScoped<RepositoryPattern.Comparisons.Option4_SpecificationRepository.SubmitOpenOrderHandler>();
        services.AddScoped<RepositoryPattern.Comparisons.Option5_ConstrainedHybrid.SubmitOpenOrderHandler>();

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

[CollectionDefinition("Comparisons")]
public sealed class ComparisonsCollection : ICollectionFixture<ComparisonsFixture>;
