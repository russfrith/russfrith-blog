using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shop.Onion.Application;
using Shop.Onion.Application.Orders;
using Shop.Onion.Application.Products;
using Shop.Onion.Application.Products.Queries;
using Shop.Onion.Infrastructure.Orders;
using Shop.Onion.Infrastructure.Products;

namespace Shop.Onion.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnionInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
