using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Infrastructure.Persistence;

// Internal: nothing outside this project constructs or references this type
// directly. Api wires it up only through AddInfrastructure.
internal class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
