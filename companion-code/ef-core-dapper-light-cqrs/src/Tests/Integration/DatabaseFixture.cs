using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Tests.Integration;

/// <summary>
/// Integration tests run against a real PostgreSQL instance - the same compose Postgres
/// used by the app, pointed at a dedicated test database - never SQLite or the EF
/// in-memory provider, since this project cares about real Postgres SQL translation and
/// Dapper's raw SQL against the same schema.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("ORDERS_TEST_DB_CONNECTION")
            ?? "Host=localhost;Port=55432;Database=orders_test;Username=postgres;Password=postgres";
    }

    public async Task InitializeAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention();

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention();
        return new AppDbContext(optionsBuilder.Options);
    }

    public IDbConnectionFactory CreateConnectionFactory() => new NpgsqlConnectionFactory(ConnectionString);

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "truncate table order_lines, orders, products, customers, order_audit_log cascade";
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
