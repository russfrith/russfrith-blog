using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderLightCqrs.Infrastructure.Write;

/// <summary>
/// Design-time only, used by `dotnet ef migrations add`. The real connection string comes
/// from configuration at runtime (see ServiceCollectionExtensions); this one only needs to
/// be valid enough for EF to build the model.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Database=orders;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}
