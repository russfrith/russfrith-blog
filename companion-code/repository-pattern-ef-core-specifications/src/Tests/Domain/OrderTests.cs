using RepositoryPattern.Domain;
using Xunit;

namespace RepositoryPattern.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_requires_a_customer()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty));
    }

    [Fact]
    public void AddLine_rejects_non_positive_quantity()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AddLine("Widget", 0, 9.99m));
        Assert.Throws<DomainException>(() => order.AddLine("Widget", -1, 9.99m));
    }

    [Fact]
    public void AddLine_rejects_negative_unit_price()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AddLine("Widget", 1, -0.01m));
    }

    [Fact]
    public void Submit_requires_at_least_one_line()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.Submit());
    }

    [Fact]
    public void Submit_succeeds_with_at_least_one_line()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);

        order.Submit();

        Assert.Equal(OrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void AddLine_fails_once_the_order_is_submitted()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);
        order.Submit();

        Assert.Throws<DomainException>(() => order.AddLine("Gadget", 1, 4.99m));
    }

    [Fact]
    public void Submit_fails_once_the_order_is_already_submitted()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);
        order.Submit();

        Assert.Throws<DomainException>(() => order.Submit());
    }

    [Theory]
    [InlineData(OrderStatus.Draft, true)]
    [InlineData(OrderStatus.Submitted, true)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void IsOpen_reflects_draft_and_submitted_only(OrderStatus status, bool expectedOpen)
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);

        switch (status)
        {
            case OrderStatus.Submitted:
                order.Submit();
                break;
            case OrderStatus.Shipped:
                order.Submit();
                order.Ship();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        Assert.Equal(expectedOpen, order.IsOpen);
    }

    [Fact]
    public void Ship_requires_the_order_to_be_submitted_first()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);

        Assert.Throws<DomainException>(() => order.Ship());
    }

    [Fact]
    public void Cancel_fails_once_the_order_has_shipped()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);
        order.Submit();
        order.Ship();

        Assert.Throws<DomainException>(() => order.Cancel());
    }
}
