using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace RepositoryPattern.ArchitectureTests;

public class DependencyRuleTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = Architectures.Full;

    [Fact]
    public void Domain_depends_on_nothing_else_in_the_solution()
    {
        Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Domain")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Application"))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Infrastructure"))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore"))
            .Check(Architecture);
    }

    [Fact]
    public void Application_does_not_depend_on_entity_framework_core()
    {
        Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Application")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore"))
            .Check(Architecture);
    }

    [Fact]
    public void Api_does_not_depend_on_entity_framework_core()
    {
        // Comparisons is the one project exempt from this rule - see
        // RepositoryPattern.Comparisons.csproj and FINDINGS.md for why.
        // Api is selected by assembly, not namespace: its Program.cs is
        // top-level statements, so its generated Program/OrderResponse types
        // sit in the global namespace rather than RepositoryPattern.Api.
        Types().That().ResideInAssemblyMatching(".*RepositoryPattern.Api.*")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore"))
            .Check(Architecture);
    }

    [Fact]
    public void Nothing_outside_infrastructure_depends_on_infrastructures_persistence_types()
    {
        Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Application")
            .Or().ResideInNamespaceOrBelow("RepositoryPattern.Api")
            .Or().ResideInNamespaceOrBelow("RepositoryPattern.Comparisons")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Infrastructure.Persistence"))
            .Check(Architecture);
    }
}
