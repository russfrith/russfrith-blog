using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RepositoryPattern.Infrastructure.Persistence;

// Lets `dotnet ef migrations add` construct a context without going through
// the application's own DI container or appsettings.
internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql("Host=localhost;Database=repository_pattern_design;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new OrderingDbContext(optionsBuilder.Options);
    }
}
