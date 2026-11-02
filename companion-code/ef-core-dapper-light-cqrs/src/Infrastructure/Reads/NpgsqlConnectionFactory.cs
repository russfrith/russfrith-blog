using Npgsql;

namespace OrderLightCqrs.Infrastructure.Reads;

public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
}

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}
