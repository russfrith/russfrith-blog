using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;
using RepositoryPattern.Infrastructure.Persistence;

namespace RepositoryPattern.Infrastructure;

// The only public surface of this project that a caller needs. Api and
// Comparisons depend on this method and on Application's interfaces; nobody
// references OrderingDbContext or EfRepository<T> directly.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());

        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
