using Shop.VerticalSlice.Common;
using CreateProduct = Shop.VerticalSlice.Features.Products.CreateProduct;
using UpdateProduct = Shop.VerticalSlice.Features.Products.UpdateProduct;
using DeleteProduct = Shop.VerticalSlice.Features.Products.DeleteProduct;

namespace Shop.VerticalSlice.Tests.Products;

public class ProductHandlerTests : IClassFixture<SqliteContextFactory>
{
    private readonly SqliteContextFactory _factory;

    public ProductHandlerTests(SqliteContextFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProduct_ThenUpdate_ThenDelete_RoundTrips()
    {
        using var context = _factory.CreateContext();

        var createHandler = new CreateProduct.Handler(context);
        var productId = await createHandler.HandleAsync(new CreateProduct.Request("SKU-100", "Widget", 9.99m));

        var updateHandler = new UpdateProduct.Handler(context);
        await updateHandler.HandleAsync(productId, new UpdateProduct.Request("Widget Pro", 14.99m));

        using var verifyContext = _factory.CreateContext();
        var updated = await verifyContext.Products.FindAsync(productId);
        Assert.Equal("Widget Pro", updated!.Name);
        Assert.Equal(14.99m, updated.Price);

        var deleteHandler = new DeleteProduct.Handler(verifyContext);
        await deleteHandler.HandleAsync(productId);

        using var finalContext = _factory.CreateContext();
        Assert.Null(await finalContext.Products.FindAsync(productId));
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ThrowsDomainException()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateProduct.Handler(context);

        await handler.HandleAsync(new CreateProduct.Request("SKU-DUP", "Widget", 9.99m));

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CreateProduct.Request("SKU-DUP", "Another Widget", 5m)));
    }
}
