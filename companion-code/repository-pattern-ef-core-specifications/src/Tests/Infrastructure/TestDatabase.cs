namespace RepositoryPattern.Tests.Infrastructure;

internal static class TestDatabase
{
    // The docker-compose "tests" service sets this to the compose network's
    // postgres hostname; falls back to localhost for `dotnet test` against
    // `docker compose up postgres`.
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
        ?? "Host=localhost;Database=repository_pattern_test;Username=postgres;Password=postgres";
}
