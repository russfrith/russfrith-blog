using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;
using Xunit;

namespace RepositoryPattern.Tests.Application;

// These run against plain in-memory Order objects, with no database involved -
// a specification's criteria is just a compiled Expression<Func<T, bool>>,
// so it's testable the same way any other predicate is.
public class SpecificationTests
{
    [Fact]
    public void OpenOrdersForCustomer_criteria_matches_draft_and_submitted_only()
    {
        var customerId = Guid.NewGuid();
        var spec = new OpenOrdersForCustomer(customerId);
        var predicate = spec.Criteria!.Compile();

        var draft = Order.Create(customerId);
        draft.AddLine("Widget", 1, 9.99m);

        var submitted = Order.Create(customerId);
        submitted.AddLine("Widget", 1, 9.99m);
        submitted.Submit();

        var shipped = Order.Create(customerId);
        shipped.AddLine("Widget", 1, 9.99m);
        shipped.Submit();
        shipped.Ship();

        var otherCustomer = Order.Create(Guid.NewGuid());
        otherCustomer.AddLine("Widget", 1, 9.99m);

        Assert.True(predicate(draft));
        Assert.True(predicate(submitted));
        Assert.False(predicate(shipped));
        Assert.False(predicate(otherCustomer));
    }

    [Fact]
    public void OpenOrdersForCustomer_includes_lines_and_orders_newest_first()
    {
        var spec = new OpenOrdersForCustomer(Guid.NewGuid());

        Assert.Contains(spec.Includes, include => include.ToString().Contains("Lines"));
        Assert.NotNull(spec.OrderByDescending);
        Assert.Null(spec.OrderBy);
    }

    [Fact]
    public void Paging_requires_an_ordering_to_already_be_applied()
    {
        Assert.Throws<InvalidOperationException>(() => new UnorderedPagedSpecification());
    }

    [Fact]
    public void Paging_succeeds_once_an_ordering_is_applied()
    {
        var spec = new OrderedPagedSpecification();

        Assert.True(spec.IsPagingEnabled);
        Assert.Equal(10, spec.Skip);
        Assert.Equal(20, spec.Take);
    }

    private sealed class UnorderedPagedSpecification : Specification<Order>
    {
        public UnorderedPagedSpecification()
        {
            ApplyPaging(10, 20);
        }
    }

    private sealed class OrderedPagedSpecification : Specification<Order>
    {
        public OrderedPagedSpecification()
        {
            ApplyOrderByDescending(order => order.CreatedAt);
            ApplyPaging(10, 20);
        }
    }
}
