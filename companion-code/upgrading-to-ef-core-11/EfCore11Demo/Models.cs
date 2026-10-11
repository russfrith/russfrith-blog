using System.ComponentModel.DataAnnotations.Schema;

namespace EfCore11Demo;

// Models for the "What you get for the trouble" / breaking-changes examples.

public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BlogTypeId { get; set; }
    public BlogType BlogType { get; set; } = null!;
    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }
    public List<Post> Posts { get; set; } = [];
    public string? JsonData { get; set; }
}

public class BlogType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Owner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Post
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
}

// Named Sales- to avoid colliding with Customer/Person below, which are the
// "Complex Types as value objects" section's own Customer. The post's
// FullJoin example never defines a Customer/Order class of its own - it's
// the generic pair used throughout the EF Core docs - so there's no
// corresponding rename to make in the post itself.
public class SalesCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SalesOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
}

// Models for the "Complex Types as value objects" section.

[ComplexType]
public record PersonName(string First, string Last)
{
    public override string ToString() => $"{First} {Last}";
}

[ComplexType]
public record PhoneNumber(int CountryCode, string NationalNumber)
{
    public override string ToString() => $"+{CountryCode} {NationalNumber}";
}

public abstract class Person
{
    public int Id { get; set; }
    public required PersonName Name { get; set; }
    public required PhoneNumber Phone { get; set; }
}

public class Customer : Person
{
    public string Company { get; set; } = string.Empty;
}

public class Employee : Person
{
    public DateOnly HireDate { get; set; }
}

// Address is configured two different ways in Program.cs: as a complex
// type on DemoContext, and as an owned type on LegacyOwnedAddressContext,
// to reproduce the shared-instance bug the post describes. It isn't marked
// [ComplexType] here, so both configurations stay available side by side.
public record Address(string Line1, string? Line2, string City, string Country, string PostCode);

public class Order
{
    public int Id { get; set; }
    public required Address BillingAddress { get; set; }
    public required Address ShippingAddress { get; set; }
}
