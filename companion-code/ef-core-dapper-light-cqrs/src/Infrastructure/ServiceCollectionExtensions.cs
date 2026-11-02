using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderLightCqrs.Application;
using OrderLightCqrs.Application.Commands;
using OrderLightCqrs.Application.Queries;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;
using OrderLightCqrs.Infrastructure.WriteExceptions;

namespace OrderLightCqrs.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderLightCqrsInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

        // Write side.
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IOrderAuditWriter, OrderAuditWriter>();
        services.AddScoped<ICreateOrderHandler, CreateOrderHandler>();
        services.AddScoped<ISubmitOrderHandler, SubmitOrderHandler>();
        services.AddScoped<IBulkMarkOrdersShippedHandler, BulkMarkOrdersShippedHandler>();

        // Read side.
        services.AddScoped<IOrderQueries, OrderQueries>();

        return services;
    }
}
