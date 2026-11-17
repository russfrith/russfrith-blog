using System.Reflection;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;
using RepositoryPattern.Infrastructure;
using Xunit;

namespace RepositoryPattern.ArchitectureTests;

// Rules about generic constraints, access modifiers and member shape aren't
// naturally expressed as ArchUnitNET dependency rules, so these use plain
// reflection instead. See FINDINGS.md for why both styles are used.
public class StructuralRuleTests
{
    [Theory]
    [InlineData(typeof(IReadRepository<>))]
    [InlineData(typeof(IRepository<>))]
    public void Repository_interfaces_constrain_T_to_aggregate_roots(Type repositoryInterface)
    {
        var typeParameter = repositoryInterface.GetGenericArguments().Single();
        var constraints = typeParameter.GetGenericParameterConstraints();

        Assert.Contains(typeof(IAggregateRoot), constraints);
    }

    [Theory]
    [InlineData(typeof(IReadRepository<>))]
    [InlineData(typeof(IRepository<>))]
    [InlineData(typeof(IOrderRepository))]
    public void Repository_interfaces_expose_no_IQueryable_members(Type repositoryInterface)
    {
        foreach (var method in repositoryInterface.GetMethods())
        {
            Assert.DoesNotContain("IQueryable", method.ReturnType.Name);

            foreach (var parameter in method.GetParameters())
                Assert.DoesNotContain("IQueryable", parameter.ParameterType.Name);
        }
    }

    [Fact]
    public void Repository_implementations_are_internal()
    {
        var infrastructureAssembly = typeof(ServiceCollectionExtensions).Assembly;

        var repositoryTypes = infrastructureAssembly.GetTypes()
            .Where(type => type.GetInterfaces().Any(IsGenericReadRepository))
            .ToList();

        Assert.NotEmpty(repositoryTypes);
        Assert.All(repositoryTypes, type => Assert.True(type.IsNotPublic, $"{type.Name} should not be public."));
    }

    [Fact]
    public void EfRepository_exposes_no_DbSet_or_DbContext_as_a_protected_member_and_only_one_IQueryable_returning_helper()
    {
        var infrastructureAssembly = typeof(ServiceCollectionExtensions).Assembly;
        var efRepositoryType = infrastructureAssembly.GetType("RepositoryPattern.Infrastructure.Persistence.EfRepository`1")
            ?? throw new InvalidOperationException("EfRepository<T> was not found - did it get renamed or moved?");

        var protectedMembers = efRepositoryType
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.IsFamily)
            .ToList();

        Assert.All(protectedMembers, member =>
        {
            Assert.DoesNotContain("DbSet", member.ReturnType.Name);
            Assert.DoesNotContain("DbContext", member.ReturnType.Name);
        });

        var queryReturningMembers = protectedMembers.Where(member => member.ReturnType.Name.Contains("IQueryable")).ToList();

        // The one narrow exception this post allows: ComposeWithRawSql, which
        // composes a hand-written SQL fragment with an optional spec and
        // returns a query, for a derived named-method repository to finish
        // with Include()/ToListAsync(). Nothing else protected is allowed to
        // hand back a query.
        Assert.Single(queryReturningMembers);
        Assert.Equal("ComposeWithRawSql", queryReturningMembers[0].Name);
    }

    private static bool IsGenericReadRepository(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadRepository<>);
}
