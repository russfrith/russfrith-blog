using NetArchTest.Rules;
using OrderLightCqrs.Application;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure;

namespace OrderLightCqrs.Tests.Architecture;

public class ArchitectureTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(IOrderRepository).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(ServiceCollectionExtensions).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnApplicationOrInfrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "OrderLightCqrs.Application",
                "OrderLightCqrs.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Dapper",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrDataAccessLibraries()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "OrderLightCqrs.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Dapper",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void DapperIsOnlyReferencedFromReadsOrDocumentedWriteExceptions()
    {
        // The rule: Dapper is for reads. The two classes in WriteExceptions are the
        // explicitly contained, tested, documented exception for writes that either join
        // an EF transaction (OrderAuditWriter) or bypass aggregates for a set-based update
        // (BulkMarkOrdersShippedHandler). Nothing else gets to add a third.
        var typesUsingDapper = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn("Dapper")
            .GetTypes()
            .ToList();

        var offenders = typesUsingDapper
            .Where(t => t.Namespace is not "OrderLightCqrs.Infrastructure.Reads"
                        and not "OrderLightCqrs.Infrastructure.WriteExceptions")
            .Select(t => t.FullName)
            .ToList();

        Assert.True(offenders.Count == 0, $"Types referencing Dapper outside Reads/WriteExceptions: {string.Join(", ", offenders)}");

        var writeExceptionTypes = typesUsingDapper
            .Where(t => t.Namespace == "OrderLightCqrs.Infrastructure.WriteExceptions")
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(new[] { "BulkMarkOrdersShippedHandler", "OrderAuditWriter" }, writeExceptionTypes);
    }

    [Fact]
    public void RepositoryMethods_NeverReturnDtos()
    {
        var repositoryTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IOrderRepository))
            .GetTypes();

        foreach (var repositoryType in repositoryTypes)
        {
            foreach (var method in repositoryType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                var returnType = UnwrapTask(method.ReturnType);
                Assert.False(
                    returnType.Name.EndsWith("Dto", StringComparison.Ordinal),
                    $"{repositoryType.Name}.{method.Name} returns a DTO ({returnType.Name}); repositories must stay aggregate-shaped.");
            }
        }
    }

    [Fact]
    public void QueryServiceMethods_NeverReturnDomainOrAggregateTypes()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("OrderLightCqrs.Infrastructure.Reads")
            .Should()
            .NotHaveDependencyOn("OrderLightCqrs.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static Type UnwrapTask(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    private static string Describe(TestResult result) =>
        result.FailingTypes is null
            ? "no detail available"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
