using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shop.Onion.Infrastructure;

namespace Shop.Onion.Tests.Integration;

/// <summary>
/// Keeps a single open connection alive for the lifetime of the test so the
/// in-memory SQLite database isn't dropped between uses.
/// </summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
