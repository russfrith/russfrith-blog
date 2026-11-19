using Microsoft.EntityFrameworkCore;

namespace Shop.VerticalSlice.Data;

public class AppDbContext : DbContext
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<DiscountCodeEntity> DiscountCodes => Set<DiscountCodeEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<DiscountCodeEntity>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).ValueGeneratedNever();
            b.Property(d => d.Code).IsRequired().HasMaxLength(32);
            b.HasIndex(d => d.Code).IsUnique();
        });

        modelBuilder.Entity<OrderEntity>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).ValueGeneratedNever();
            b.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(o => o.AppliedDiscounts).WithOne().HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLineEntity>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Id).ValueGeneratedNever();
            b.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<AppliedDiscountEntity>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).ValueGeneratedNever();
            b.Property(d => d.Code).IsRequired().HasMaxLength(32);
        });
    }
}
