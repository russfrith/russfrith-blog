using Shop.Onion.Application.Products;
using Shop.Onion.Domain;
using Shop.Onion.Infrastructure;
using Shop.Onion.Infrastructure.Products;

namespace Shop.Onion.Tests.Integration;

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
        var repository = new ProductRepository(context);
        var unitOfWork = new EfUnitOfWork(context);

        var createHandler = new CreateProductHandler(repository, unitOfWork);
        var productId = await createHandler.HandleAsync(new CreateProductCommand("SKU-100", "Widget", 9.99m));

        var updateHandler = new UpdateProductHandler(repository, unitOfWork);
        await updateHandler.HandleAsync(new UpdateProductCommand(productId, "Widget Pro", 14.99m));

        using var verifyContext = _factory.CreateContext();
        var updated = await verifyContext.Products.FindAsync(productId);
        Assert.Equal("Widget Pro", updated!.Name);
        Assert.Equal(14.99m, updated.Price);

        var deleteHandler = new DeleteProductHandler(new ProductRepository(verifyContext), new EfUnitOfWork(verifyContext));
        await deleteHandler.HandleAsync(new DeleteProductCommand(productId));

        using var finalContext = _factory.CreateContext();
        Assert.Null(await finalContext.Products.FindAsync(productId));
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ThrowsDomainException()
    {
        using var context = _factory.CreateContext();
        var repository = new ProductRepository(context);
        var unitOfWork = new EfUnitOfWork(context);
        var handler = new CreateProductHandler(repository, unitOfWork);

        await handler.HandleAsync(new CreateProductCommand("SKU-DUP", "Widget", 9.99m));

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CreateProductCommand("SKU-DUP", "Another Widget", 5m)));
    }
}
