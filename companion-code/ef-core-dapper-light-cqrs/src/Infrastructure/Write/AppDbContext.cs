using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Reference;

namespace OrderLightCqrs.Infrastructure.Write;

public class AppDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<OrderAuditLogEntry> OrderAuditLog => Set<OrderAuditLogEntry>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("customers");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).HasMaxLength(200).IsRequired();
            b.Property(p => p.Category).HasMaxLength(100).IsRequired();
            b.HasIndex(p => p.Category);
        });

        modelBuilder.Entity<OrderAuditLogEntry>(b =>
        {
            b.ToTable("order_audit_log");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedOnAdd();
            b.Property(a => a.EventName).HasMaxLength(100).IsRequired();
            b.HasIndex(a => a.OrderId);
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerId).IsRequired();
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(o => o.CreatedAtUtc).IsRequired();
            b.Property(o => o.SubmittedAtUtc);
            b.Ignore(o => o.Total);

            // Realistic indexes for the order-list screen: filter by status and date range,
            // sort by date; and a lookup index for the customer-history export.
            b.HasIndex(o => new { o.Status, o.CreatedAtUtc });
            b.HasIndex(o => o.CustomerId);

            b.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderLine>(b =>
        {
            b.ToTable("order_lines");
            b.HasKey(l => l.Id);
            b.Property(l => l.ProductId).IsRequired();
            b.Property(l => l.Quantity).IsRequired();
            b.Property(l => l.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();
            b.Ignore(l => l.LineTotal);
            b.Property<Guid>("OrderId");
            b.HasIndex("OrderId");
            b.HasIndex(l => l.ProductId);
        });
    }
}
