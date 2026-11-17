using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Shared;

// A plain, public DbContext mapping the same orders/order_lines tables that
// Infrastructure's (internal) OrderingDbContext maps, so options 1-4 below
// can run against the same seeded data without reaching into Infrastructure's
// internals.
public class ComparisonDbContext(DbContextOptions<ComparisonDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Status).HasConversion<int>();
            order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
            order.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.ToTable("order_lines");
            line.HasKey(l => l.Id);
        });
    }
}
