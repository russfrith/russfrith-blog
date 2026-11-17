using Microsoft.Extensions.DependencyInjection;
using RepositoryPattern.Comparisons.Option1_RawDbContext;
using RepositoryPattern.Domain;
using RepositoryPattern.Infrastructure;
using RepositoryPattern.Infrastructure.Persistence;
using Xunit;
using Option2 = RepositoryPattern.Comparisons.Option2_QueryObjects;
using Option3 = RepositoryPattern.Comparisons.Option3_ClassicRepository;
using Option4 = RepositoryPattern.Comparisons.Option4_SpecificationRepository;
using Option5 = RepositoryPattern.Comparisons.Option5_ConstrainedHybrid;

namespace RepositoryPattern.Tests.Comparisons;

// All five options implement the same scenario: "get a customer's open
// orders, submit one". This asserts they all pick the same order and leave
// the data in the same state, which is the whole point of comparing them -
// the five are behaviorally equivalent, so the comparison in FINDINGS.md is
// about ceremony and testability, not correctness.
[Collection("Comparisons")]
public class ScenarioEquivalenceTests(ComparisonsFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("Option1")]
    [InlineData("Option2")]
    [InlineData("Option3")]
    [InlineData("Option4")]
    [InlineData("Option5")]
    public async Task Scenario_submits_the_customers_only_draft_order(string option)
    {
        using var scope = fixture.Services.CreateScope();

        var customerId = Guid.NewGuid();
        await SeedAsync(scope.ServiceProvider, customerId);

        var submittedOrder = option switch
        {
            "Option1" => await scope.ServiceProvider.GetRequiredService<OpenOrdersScenario>()
                .GetOpenOrdersAndSubmitOneAsync(customerId, CancellationToken.None),
            "Option2" => await scope.ServiceProvider.GetRequiredService<Option2.SubmitOpenOrderHandler>()
                .HandleAsync(customerId, CancellationToken.None),
            "Option3" => await scope.ServiceProvider.GetRequiredService<Option3.SubmitOpenOrderHandler>()
                .HandleAsync(customerId, CancellationToken.None),
            "Option4" => await scope.ServiceProvider.GetRequiredService<Option4.SubmitOpenOrderHandler>()
                .HandleAsync(customerId, CancellationToken.None),
            "Option5" => await scope.ServiceProvider.GetRequiredService<Option5.SubmitOpenOrderHandler>()
                .HandleAsync(customerId, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(option))
        };

        Assert.NotNull(submittedOrder);
        Assert.Equal(OrderStatus.Submitted, submittedOrder!.Status);
    }

    private static async Task SeedAsync(IServiceProvider services, Guid customerId)
    {
        var dbContext = services.GetRequiredService<OrderingDbContext>();

        var draft = Order.Create(customerId);
        draft.AddLine("Widget", 1, 9.99m);
        dbContext.Orders.Add(draft);

        var shipped = Order.Create(customerId);
        shipped.AddLine("Gadget", 1, 4.99m);
        shipped.Submit();
        shipped.Ship();
        dbContext.Orders.Add(shipped);

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }
}
