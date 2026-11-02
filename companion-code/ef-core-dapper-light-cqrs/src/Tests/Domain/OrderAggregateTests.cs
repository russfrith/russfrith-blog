using OrderLightCqrs.Domain;

namespace OrderLightCqrs.Tests.Domain;

public class OrderAggregateTests
{
    [Fact]
    public void Create_WithEmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty));
    }

    [Fact]
    public void AddLine_WithZeroQuantity_Throws()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), 0, 9.99m));
    }

    [Fact]
    public void AddLine_WithNegativeQuantity_Throws()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), -1, 9.99m));
    }

    [Fact]
    public void AddLine_WithNegativeUnitPrice_Throws()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), 1, -0.01m));
    }

    [Fact]
    public void Submit_WithNoLines_Throws()
    {
        var order = Order.Create(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.Submit());
    }

    [Fact]
    public void Submit_WithLines_SetsStatusAndTimestamp()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 2, 9.99m);

        order.Submit();

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.NotNull(order.SubmittedAtUtc);
    }

    [Fact]
    public void AddLine_AfterSubmit_Throws()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);
        order.Submit();

        Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), 1, 9.99m));
    }

    [Fact]
    public void ChangeLineQuantity_AfterSubmit_Throws()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);
        var lineId = order.Lines.Single().Id;
        order.Submit();

        Assert.Throws<DomainException>(() => order.ChangeLineQuantity(lineId, 2));
    }

    [Fact]
    public void RemoveLine_AfterSubmit_Throws()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);
        var lineId = order.Lines.Single().Id;
        order.Submit();

        Assert.Throws<DomainException>(() => order.RemoveLine(lineId));
    }

    [Fact]
    public void Submit_Twice_Throws()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);
        order.Submit();

        Assert.Throws<DomainException>(() => order.Submit());
    }

    [Fact]
    public void MarkShipped_FromDraft_Throws()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);

        Assert.Throws<DomainException>(() => order.MarkShipped());
    }

    [Fact]
    public void MarkShipped_FromSubmitted_Succeeds()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);
        order.Submit();

        order.MarkShipped();

        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Cancel_AfterShipped_Throws()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 9.99m);
        order.Submit();
        order.MarkShipped();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Cancel_FromDraft_Succeeds()
    {
        var order = Order.Create(Guid.NewGuid());

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Total_SumsLineTotals()
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 2, 10m);
        order.AddLine(Guid.NewGuid(), 3, 5m);

        Assert.Equal(35m, order.Total);
    }
}
