using Microsoft.EntityFrameworkCore;
using Shop.Onion.Domain;

namespace Shop.Onion.Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<DiscountCode>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).ValueGeneratedNever();
            b.Property(d => d.Code).IsRequired().HasMaxLength(32);
            b.HasIndex(d => d.Code).IsUnique();
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).ValueGeneratedNever();
            b.Property(o => o.CustomerId).IsRequired();
            b.Ignore(o => o.Subtotal);
            b.Ignore(o => o.DiscountTotal);
            b.Ignore(o => o.Total);

            b.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

            b.HasMany(o => o.AppliedDiscounts)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(o => o.AppliedDiscounts).HasField("_appliedDiscounts").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderLine>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Id).ValueGeneratedNever();
            b.Property(l => l.ProductId).IsRequired();
            b.Property(l => l.Quantity).IsRequired();
            b.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            b.Ignore(l => l.LineTotal);
            b.Property<Guid>("OrderId");
            b.HasIndex("OrderId");
        });

        modelBuilder.Entity<AppliedDiscount>(b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd();
            b.HasKey("Id");
            b.Property(d => d.Code).IsRequired().HasMaxLength(32);
            b.Property<Guid>("OrderId");
            b.HasIndex("OrderId");
        });
    }
}
