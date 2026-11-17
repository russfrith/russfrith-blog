using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace RepositoryPattern.ArchitectureTests;

internal static class Architectures
{
    // Loaded once and shared by every rule below. Api is a top-level-statement
    // minimal API with no public type of its own to anchor a `typeof`, so it's
    // loaded by assembly name instead.
    public static readonly Architecture Full = new ArchLoader()
        .LoadAssemblies(
            typeof(RepositoryPattern.Domain.IAggregateRoot).Assembly,
            typeof(RepositoryPattern.Application.Abstractions.IRepository<>).Assembly,
            typeof(RepositoryPattern.Infrastructure.ServiceCollectionExtensions).Assembly,
            typeof(RepositoryPattern.Comparisons.Shared.ComparisonDbContext).Assembly,
            Assembly.Load("RepositoryPattern.Api"),
            // Loaded explicitly so EF Core's own types are full members of the
            // model - otherwise a "depends on Microsoft.EntityFrameworkCore"
            // rule has nothing concrete to match against.
            typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly)
        .Build();
}
