using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Benchmarks;

internal static class BenchmarkEnvironment
{
    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ORDERS_DB_CONNECTION")
        ?? "Host=localhost;Port=55432;Database=orders;Username=postgres;Password=postgres";

    public static AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention();
        return new AppDbContext(optionsBuilder.Options);
    }

    public static IDbConnectionFactory CreateConnectionFactory() => new NpgsqlConnectionFactory(ConnectionString);
}
