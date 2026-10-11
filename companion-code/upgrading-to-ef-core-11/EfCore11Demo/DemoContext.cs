using Microsoft.EntityFrameworkCore;

namespace EfCore11Demo;

public class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogType> BlogTypes => Set<BlogType>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<SalesCustomer> SalesCustomers => Set<SalesCustomer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>()
            .HasOne(b => b.Owner)
            .WithMany()
            .HasForeignKey(b => b.OwnerId);

        modelBuilder.Entity<Blog>()
            .HasOne(b => b.BlogType)
            .WithMany()
            .HasForeignKey(b => b.BlogTypeId);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Blog)
            .WithMany(b => b.Posts)
            .HasForeignKey(p => p.BlogId);

        // "Complex Types as value objects" - TPT inheritance, lambda-chained
        // configuration, and an index on a property nested inside a complex
        // type. All three are specifically unblocked by EF Core 11.
        modelBuilder.Entity<Person>().UseTptMappingStrategy();

        modelBuilder.Entity<Person>()
            .Property(p => p.Phone.NationalNumber)
            .HasMaxLength(15);

        modelBuilder.Entity<Person>()
            .HasIndex(p => p.Phone.NationalNumber);

        // Address as a complex type: see LegacyOwnedAddressContext for the
        // owned-type comparison.
        modelBuilder.Entity<Order>().ComplexProperty(o => o.BillingAddress);
        modelBuilder.Entity<Order>().ComplexProperty(o => o.ShippingAddress);
    }
}
