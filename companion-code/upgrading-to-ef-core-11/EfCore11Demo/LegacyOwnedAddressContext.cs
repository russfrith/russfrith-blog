using Microsoft.EntityFrameworkCore;

namespace EfCore11Demo;

// Reproduces the pre-EF Core 8 owned-entity-type mapping for Order's two
// Address properties, so Program.cs can show the shared-instance bug the
// post describes actually throwing. Everything else in the companion code
// uses DemoContext's complex-type mapping instead.
public class LegacyOwnedAddressContext(DbContextOptions<LegacyOwnedAddressContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().OwnsOne(o => o.BillingAddress);
        modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress);
    }
}
