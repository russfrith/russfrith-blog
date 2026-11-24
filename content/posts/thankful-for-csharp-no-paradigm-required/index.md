---
title: "Thankful for C#: It Doesn't Make You Pick a Paradigm"
description: "A Thanksgiving-week thank-you to C#: seven paradigms in one strongly typed language, and the small features that make each one pleasant."
date: 2026-11-23T22:02:34-05:00
draft: true
cover:
  image: ulleo-corn-3663086_1920.jpg
  alt: "Colorful Indian corn cobs in a rustic autumn harvest display."
  caption: "Image by [Leopictures](https://pixabay.com/users/ulleo-1834854/?utm_source=link-attribution&utm_medium=referral&utm_campaign=image&utm_content=3663086) from [Pixabay](https://pixabay.com//?utm_source=link-attribution&utm_medium=referral&utm_campaign=image&utm_content=3663086)"
ShowToc: true
tags: ["dotnet", "csharp"]
categories: ["programming"]
---

## Introduction
Thanksgiving week is when we are all supposed to say what we are thankful for, usually just before eating too much. The most important answers belong at the dinner table. This blog gets the nerdier one, which is that I am thankful for C#.

The short version is that C# does not make you pick a paradigm. In one ordinary project you can write object-oriented domain models, functional data pipelines, declarative queries, generic algorithms, asynchronous workflows, code that writes code at compile time, and tight loops that never touch the heap. All of it lives in a strongly typed, garbage-collected language that runs on Windows, Linux, and macOS, and the pieces are integrated enough that you can mix them in the same method.

I do not want to suggest that C# is my favorite language or the best one. Other languages cover much of this list, and I checked before saying so. Swift has macros, async/await, and actors, and Swift 6.2 added a `Span` type. Kotlin has coroutines and Flow, plus a symbol-processing API for generating code. Scala 3 and Rust each have their own macro systems, and F# brings functional programming and computation expressions to the .NET runtime. Java now has records, sealed classes, pattern matching for `switch`, and virtual threads, with value classes still a preview feature.[[1](#references)]

So no single item in this post is unique to C#, and a few languages are arguably stronger in individual areas, such as Rust at low-level control or F# and Scala at functional programming. What I find unusual is how many of them arrive together in one mainstream, garbage-collected language, in one SDK. That last part is a judgment call, not something I can measure.

This post is organized by paradigm, not by release, because the paradigms change slowly and the releases do not. Each section has a larger idea that has been around for a while, and then a few small features that made that style nicer to write. The small ones are the real Thanksgiving list. They do not headline conference keynotes, and none of them has a logo. They just remove a few lines that I would otherwise have to write, read, or get slightly wrong.

I should say which of these I actually use. I regularly use nullable reference types, guard clauses of some kind, null-conditional assignment, records, LINQ, and async streams and channels. .NET 11 only became generally available on November 10, so I have not put many of its features into production yet. For those, and for anything else here that I have not used regularly, I am going on the documentation and the test projects I ran.

A note on versions. .NET 11 reached general availability on November 10, and nothing in this post is a preview feature.[[2](#references)] Only .NET 10 and .NET 11 are currently supported by Microsoft, so the "available since" versions tell you when something arrived, not what you should be running.[[2](#references)] The table near the end collects them all. I compiled and ran the C# snippets in a Linux container built from the official .NET 11 SDK image (`mcr.microsoft.com/dotnet/sdk:11.0`, SDK 11.0.100).

## Correctness: Let the Compiler Check
### Nullable Reference Types
The `NullReferenceException` is the guest nobody invites to Thanksgiving dinner, and it shows up anyway. C# 8 added nullable reference types, which let a reference type say whether null is allowed.[[3](#references)] With the feature enabled, `string` means "never null" and `string?` means "might be," and the compiler tracks which one you are holding:

```csharp
string? nickname = GetNickname();

Console.WriteLine(nickname.Length);        // warning CS8602: Dereference of a possibly null reference

if (nickname is not null)
    Console.WriteLine(nickname.Length);    // fine, the compiler saw the check

string name = null;                        // warning CS8600: Converting null literal or possible null value to non-nullable type
```

Those two warnings are what the compiler printed for this snippet. They are warnings, not errors, unless you configure otherwise, so a build that treats warnings as errors is where the feature earns its keep. The compiler checks the code you wrote. It cannot check what a caller from older code, reflection, or a project with the feature turned off decides to pass you, and that is the gap the next small feature fills. The one after it makes the null handling you do write shorter.

### Guard Clauses That Write Themselves
Many constructors and public methods open with the same ritual. Check the argument, throw the right exception, remember to pass the parameter name as a string. Here is the long way:

```csharp
public sealed class Order
{
    public Order(string customerId, IReadOnlyList<string> lines, int quantity)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(customerId));

        if (lines == null)
            throw new ArgumentNullException(nameof(lines));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");

        CustomerId = customerId;
        LineCount = lines.Count;
        Quantity = quantity;
    }

    public string CustomerId { get; }
    public int LineCount { get; }
    public int Quantity { get; }
}
```

None of that is wrong. It is just six lines of ceremony to say three things. The throw helpers that have accumulated on the exception types since .NET 6 say them directly:

```csharp
public Order(string customerId, IReadOnlyList<string> lines, int quantity)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
    ArgumentNullException.ThrowIfNull(lines);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

    CustomerId = customerId;
    LineCount = lines.Count;
    Quantity = quantity;
}
```

You never type the parameter name. The helpers capture the argument expression at the call site (that is `CallerArgumentExpression` doing the work), so the exception names the right parameter and keeps doing so if you rename it. Here is what each one produces:

```
ArgumentNullException: Value cannot be null. (Parameter 'customerId')
ArgumentException: The value cannot be an empty string or composed entirely of whitespace. (Parameter 'customerId')
ArgumentNullException: Value cannot be null. (Parameter 'lines')
ArgumentOutOfRangeException: quantity ('0') must be a non-negative and non-zero value. (Parameter 'quantity') Actual value was 0.
```

Notice that the out of range message includes the offending value, which my hand-written version only did because I remembered to ask for it.

The same idea extends to disposed objects. `ObjectDisposedException.ThrowIf` takes a condition and the instance, and builds the message from the type name:

```csharp
public void Ship()
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    // ...
}
```

That produces `ObjectDisposedException: Cannot access a disposed object. Object name: 'Order'.`

The versions are spread out, which is the only annoying part. `ArgumentNullException.ThrowIfNull` arrived in .NET 6. `ArgumentException.ThrowIfNullOrEmpty` and `ObjectDisposedException.ThrowIf` came in .NET 7. `ThrowIfNullOrWhiteSpace` and the `ArgumentOutOfRangeException` family (`ThrowIfNegativeOrZero`, `ThrowIfGreaterThan`, and friends) came in .NET 8. I confirmed those minimums by compiling each call against `net6.0` through `net11.0` in the .NET 11 SDK container. Since only .NET 10 and .NET 11 are supported now, you have all of them on any supported runtime, and the version list mostly matters if you maintain a library that still targets something older.

{{< aside >}}
If these helpers look like what guard clause libraries have offered for years, that is not an accident. The dotnet/runtime proposal behind them (issue #48573) observed that many libraries, including the framework's own, already wrote private throw helpers, and suggested building the pattern into the exception types themselves.[[4](#references)] Its stated motivation was code size and the cost of just-in-time (JIT) compilation. The proposal started as `ArgumentNullException.Throw` and was later revised to the `ThrowIfNull` shape with `[CallerArgumentExpression]` that shipped. If you want richer or fluent checks than the framework provides, community libraries such as [Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses) and [Throw](https://github.com/amantinband/throw) still fill that role. Neither is where the framework helpers came from. Throw's repository, for one, was created in 2022, after `ThrowIfNull` had shipped in .NET 6.
{{< /aside >}}

One caution. These are for programmer errors, meaning arguments that should never be wrong if the caller is behaving. They throw exceptions, so they are not a substitute for validating user input, where a bad value is expected and deserves a friendly response rather than a stack trace.

### Why Not Just Use a Library?
Fair question, since libraries did this first. I ran the same three checks through the built-in helpers, Ardalis.GuardClauses 5.0.0, and Throw 1.4.0 on .NET 11, and here is why I would start with the built-in ones.

- **No dependency.** The built-in helpers ship with the runtime. Throw 1.4.0 brings in a transitive package (OneOf), which `dotnet list package --include-transitive` showed when I restored each library in its own project. Ardalis.GuardClauses 5.0.0 brought in none.
- **Licensing and upkeep.** The Ardalis.GuardClauses README says the library "is moving to a commercial license," and NimbleTools announced a commercial release of select Ardalis packages on October 29, 2026.[[4](#references)] The 5.0.0 package I tested still contains an MIT license file, the same text as 4.6.0, and the repository's license file is MIT as well. I could not find which future versions the commercial terms will cover, so read the announcement before you upgrade. Throw is MIT licensed, but the last push to its repository was in August 2024. A framework feature does not change license or maintainers underneath you.
- **The exceptions you would expect.** Given a quantity of zero, the built-in helper throws `ArgumentOutOfRangeException` and includes the actual value in the message. Ardalis.GuardClauses throws a plain `ArgumentException` ("Required input quantity cannot be zero or negative."). Throw throws `ArgumentOutOfRangeException` with its own wording ("Value should not be less than or equal to 0."). If callers or tests catch a specific exception type, those differences matter.
- **Tooling already points here.** The analyzer rules CA1510 through CA1513 recommend the built-in helpers for `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`, and `ObjectDisposedException`, and they are on by default as suggestions in .NET 10.[[4](#references)]
- **Nothing to pay on the happy path.** I benchmarked all three checks together with valid arguments, which is what runs almost every time:

Approach | Time per call | Allocated
---------|---------------|----------
Built-in helpers | 2.3 ns | none
Ardalis.GuardClauses | 12.3 ns | 24 B
Throw | 20.8 ns | 56 B

Those are nanoseconds, so this is not a reason to avoid a library in most code. The point is only that the built-in version costs nothing, while the libraries allocate on a path that is supposed to be free. (Short-run job, in-process toolchain, same container as the benchmark below.)

The libraries still win in a few places, and it is fair to say so:

- **They return the value.** `_name = Guard.Against.NullOrWhiteSpace(name)` assigns and validates in one expression, and Throw's `ThrowIfNull()` does the same. The built-in helpers return `void`.
- **They have more rules.** Ardalis.GuardClauses includes guards such as `InvalidFormat`, `EnumOutOfRange`, and `OutOfSQLDateRange`, plus an extension point for your own. Throw offers fluent chains and checks on nested properties.
- **They customize the exception.** Throw lets you supply your own message or exception type per call.

My default would be the built-in helpers, and I would add a library only when I need a rule the framework does not have. If you do, check the license first.

### Null-Conditional Assignment
This one is C# 14, and it removes a pattern I would put in the "tiny but constant" category. You have a reference that might be null, and you want to set a property on it only if it exists:

```csharp
if (settings is not null)
{
    settings.Timeout = GetDefaultTimeout();
}
```

The `?.` operator can now appear on the left side of an assignment:

```csharp
settings?.Timeout = GetDefaultTimeout();
settings?.Retries += 1;
```

If `settings` is null, nothing happens. More usefully, the right side is not evaluated either, so `GetDefaultTimeout()` is never called. I checked that with a counter. With a null `settings` the call count stayed at zero, and with a real object it went to one. Compound assignment such as `+=` works, but the documentation says increment and decrement (`++` and `--`) are not allowed.[[5](#references)]

One thing to watch. With the .NET 11 SDK (and with 10.0.300 before it), dereferencing a local variable after a `?.` assignment produced a nullable warning (CS8602), even when I had just assigned that local a new object. Reading the value back with `?.` instead of `.` avoided the warning. I would treat that as an observation rather than a rule, but if your build treats warnings as errors, you may run into it.

> I am thankful for nullable reference types, guard clauses, and `?.` on the left side of an assignment. Between them, they catch nulls early, which is when they are cheapest to fix.

## Object-Oriented: Types That Say What They Mean
C# started as an object-oriented language, and it still is one. Classes, interfaces, and inheritance are all there. What has changed is how little ceremony the common cases need.

### Records
Many of the types in a domain model are values. Two `Money` objects with the same amount and currency are the same money. Records, added in C# 9, give you that behavior in one line.[[6](#references)] You get value equality, a readable `ToString`, deconstruction, and non-destructive mutation with `with`:

```csharp
public record Money(decimal Amount, string Currency);
public record OrderLine(string Sku, int Quantity, Money UnitPrice);

var price = new Money(10m, "USD");
var raised = price with { Amount = 12.50m };

Console.WriteLine(raised);                                    // Money { Amount = 12.50, Currency = USD }
Console.WriteLine(price == new Money(10m, "USD"));            // True
Console.WriteLine(ReferenceEquals(price, new Money(10m, "USD"))); // False

var (amount, currency) = raised;
```

The positional properties of a `record` are immutable, so `with` makes a copy instead of changing the original. A `record` is a reference type, and a `record struct` (added in C# 10) is the value type version.[[6](#references)] If you want to see value objects modeled seriously with EF Core, the complex types section of [Upgrading to EF Core 11](/posts/upgrading-to-ef-core-11/) covers it.

### Extension Members
The name is not an accident, so start with what you already know. Extension methods, added in C# 3, let you add behavior to a type you do not own by writing a static method whose first parameter is marked `this`. They are how LINQ's `Where` and `Select` appear on every sequence. C# 14 adds extension members, a new `extension` block that can declare more than methods. It can also declare properties, indexers, and operators, and members that look like statics on the type.[[7](#references)] Here are the two side by side:

```csharp
// The extension method you already know
public static class OrderLineMethods
{
    public static decimal GetTotal(this IEnumerable<OrderLine> lines) =>
        lines.Sum(l => l.Quantity * l.UnitPrice.Amount);
}

// The C# 14 extension block: same receiver, but now it can be a property
public static class OrderLineMembers
{
    extension(IEnumerable<OrderLine> lines)
    {
        public decimal Total => lines.Sum(l => l.Quantity * l.UnitPrice.Amount);
        public bool IsEmpty => !lines.Any();
    }

    extension(Money)
    {
        public static Money Zero => new(0m, "USD");
    }
}

OrderLine[] lines = [new("A-1", 2, new Money(5m, "USD")), new("B-2", 1, new Money(0.5m, "USD"))];

Console.WriteLine(lines.GetTotal());   // 10.5, the method you already know
Console.WriteLine(lines.Total);        // 10.5, a property
Console.WriteLine(lines.IsEmpty);      // False
Console.WriteLine(Money.Zero);         // Money { Amount = 0, Currency = USD }
```

{{< aside >}}
Extension members do not replace extension methods, and nothing about extension methods is deprecated. The documentation says that both forms of extension method generate the same IL (intermediate language, the compiled form that .NET runs), so callers cannot tell which one you wrote, and that you can convert existing extension methods to the new syntax whenever you like.[[7](#references)] What the new block adds is the ability to declare things a method cannot be, such as `lines.Total` as a property or `Money.Zero` as a static member, and a single place to group the members that share a receiver.
{{< /aside >}}

This needs C# 14, so the .NET 10 SDK or later. C# 15 adds extension indexers, which I cover in [When 11 Breaks 8](/posts/dotnet-8-to-dotnet-11-breaking-changes/).

### The `field` Keyword
Everyone has written this property, and then thought about it for a moment too long:

```csharp
private string _email = string.Empty;

public string Email
{
    get => _email;
    set => _email = value?.Trim().ToLowerInvariant() ?? string.Empty;
}
```

You wanted to normalize a string on the way in. To do that you had to declare a backing field, give it a name, and make sure nothing else in the class touches it by accident. C# 14 lets the compiler supply the backing field, and you refer to it with the `field` keyword:[[5](#references)]

```csharp
public string Email
{
    get;
    set => field = value?.Trim().ToLowerInvariant() ?? string.Empty;
} = string.Empty;
```

The getter stays automatic, the setter does the work, and the backing field has no name for anyone else to misuse. Setting `Email` to `"  Russ@Example.COM "` stores `russ@example.com`.

Lazy initialization gets the same treatment:

```csharp
public List<string> Tags { get => field ??= []; }
```

The list is created the first time someone reads `Tags`, and not before.

Two notes. First, if a class already has a member named `field`, the keyword will collide with it. The documentation says to disambiguate with `@field` or `this.field`, or to rename the existing member.[[5](#references)] Second, this is a compiler feature, so it does not depend on the runtime. The snippet compiled against `net6.0` through `net11.0` with `<LangVersion>14</LangVersion>`, and it needs the .NET 10 SDK or later (the C# 14 compiler) to do so. I ran the code on .NET 11, so I would not call the older targets proven beyond "it compiles."

> I am thankful for records. One line gives me value equality, `with`, deconstruction, and a readable `ToString`, and that is boilerplate I do not have to write.

## Functional: Data In, Data Out
You do not have to give up objects to write functional code in C#. Functions are values, lambdas are everywhere, and records make immutable data cheap. Three big features carry most of the weight, and one small addition from .NET 9 earns a section of its own.

### LINQ
It is hard to overstate what a game changer LINQ was. The official history of C# calls the query expression the killer feature of C# 3.0, and describes that release as laying the groundwork for turning C# into a hybrid object-oriented and functional language.[[8](#references)] That sounds like a slogan until you look at the code it replaced. Filtering, sorting, and projecting a collection meant loops, temporary lists, and comparison callbacks, and a query over a database was a string of SQL that the compiler could not see into. LINQ gave C# one query vocabulary that the compiler understands and checks, and the same vocabulary works over in-memory collections, XML, and databases.

Here is a small task, the numbers of the shipped orders with the biggest total first. This is the code before LINQ:

```csharp
var shipped = new List<OrderSummary>();
foreach (var o in orders)
{
    if (o.Status == "Shipped")
        shipped.Add(o);
}
shipped.Sort((x, y) => y.Total.CompareTo(x.Total));

var numbers = new List<string>();
foreach (var o in shipped)
    numbers.Add(o.Number);
```

And this is the same task with LINQ, which produced the same result when I compared them:

```csharp
var bigShipped = from o in orders
                 where o.Status == "Shipped"
                 orderby o.Total descending
                 select o.Number;

// A1, A3
```

The second version says what you want. The first says how to get it, and every line is a place to get the sort direction or the filter wrong. The queries use the `OrderSummary` record from the counting section below.

LINQ arrived with C# 3.0 in late 2007,[[8](#references)] and Entity Framework did not ship until 2008,[[9](#references)] so LINQ to SQL and LINQ to Objects were how I first met it. I thought it was amazing. It felt like typed SQL, the same shape of query but with the compiler checking my work.

LINQ works on any sequence, so the same shape applies to lists, files, and (as the declarative section below shows) databases. The newer operators keep the same style. .NET 11 added `FullJoin`, and [Upgrading to EF Core 11](/posts/upgrading-to-ef-core-11/) shows what EF Core does with it.

### Pattern Matching
Pattern matching lets you test the shape of data and take it apart in one expression. It has grown for years. Property patterns came with C# 8, relational patterns with C# 9, and list patterns with C# 11.[[8](#references)][[10](#references)] A `switch` expression is an expression, so it returns a value, which is why it feels functional:

```csharp
static string Describe(OrderLine line) => line switch
{
    { Quantity: 0 } => "empty",
    { Quantity: > 100 } => "bulk",
    { UnitPrice: { Currency: "USD", Amount: < 1m } } => "cheap USD item",
    _ => "regular"
};

static string Shape(int[] xs) => xs switch
{
    [] => "none",
    [var only] => $"one: {only}",
    [var first, .., var last] => $"{first} to {last}",
};

// Shape([]) is "none", Shape([7]) is "one: 7", Shape([1, 2, 3, 4]) is "1 to 4"
```

### Unions and Closed Hierarchies
The piece that was missing from all that matching is a way for the compiler to know you handled every case. C# 15 adds union types and closed hierarchies for that.[[11](#references)] Declare the cases once, and a `switch` without a default arm becomes a compile-time check:

```csharp
public record class Card(string Last4);
public record class BankTransfer(string Iban);
public record class Cash;

public union PaymentMethod(Card, BankTransfer, Cash);

static string Pay(PaymentMethod method) => method switch
{
    Card c => $"card ending {c.Last4}",
    BankTransfer b => $"transfer to {b.Iban}",
    Cash => "cash",
};
```

Add a fourth case to the union and forget to handle it, and the compiler tells you. This needs C# 15 (.NET 11), and it is generally available. I made the longer case for it, including the 2 a.m. page, in [The Case You Forgot to Handle](/posts/the-case-you-forgot-to-handle/), and closed hierarchies are in [When 11 Breaks 8](/posts/dotnet-8-to-dotnet-11-breaking-changes/).

### Counting Things in Memory Without GroupBy
Counting how many items fall into each bucket tends to get written the same way every time:

```csharp
var counts = orders
    .GroupBy(o => o.Status)
    .ToDictionary(g => g.Key, g => g.Count());
```

.NET 9 added `CountBy` and `AggregateBy` to LINQ for exactly this, but one point matters more than the syntax. These are LINQ to Objects methods. They run in your process, over a collection that is already in memory. If your data lives in a database, calling `CountBy` means you first loaded every row to count them, which is rarely what you want. A `GroupBy` with a `Count` inside an EF Core query can be translated to a SQL `GROUP BY` that sends back only the totals. I have not tested whether any EF Core provider translates `CountBy`, so do not assume it does. Everything in this section is about counting things you already hold in memory.

For that case, the official documentation describes the new methods as a way to aggregate state by key without allocating the intermediate groupings that `GroupBy` creates.[[5](#references)] Here is `CountBy`:

```csharp
public record OrderSummary(string Number, string Status, decimal Total, string Email);

OrderSummary[] orders =
[
    new("A1", "Shipped", 10m, "A@x.com"),
    new("A2", "Pending", 20m, "a@x.com"),
    new("A3", "Shipped", 5m, "b@x.com"),
];

foreach (var (status, count) in orders.CountBy(o => o.Status))
    Console.WriteLine($"{status}: {count}");

// Shipped: 2
// Pending: 1
```

When you need something other than a count, `AggregateBy` takes a seed and an accumulator. Here is the total value per status:

```csharp
foreach (var (status, total) in orders.AggregateBy(o => o.Status, 0m, (sum, o) => sum + o.Total))
    Console.WriteLine($"{status}: {total}");

// Shipped: 15
// Pending: 20
```

Both return a sequence of key and value pairs, which is why the deconstruction works. If you want a dictionary, call `ToDictionary()` on the result.

### Is It Actually Faster?
It is not just syntax, so it is worth measuring. This was a BenchmarkDotNet run (version 0.15.8, short-run job) inside the .NET 11 SDK container on Linux, using the .NET 11.0.0 runtime on a laptop with an Intel Core i7-13700H (20 logical cores). BenchmarkDotNet did not yet recognize `net11.0` for its normal separate-process runs, so I used its in-process toolchain. The benchmark counted a `List` of order records in memory at three sizes and two key shapes. One shape is five distinct keys, like an order status. The other has one key per ten items, like a customer ID. Each cell shows time and memory allocated (KB and MB are decimal).

Items | Distinct keys | `GroupBy` + `Count` | `CountBy` | Hand-written dictionary loop
------|---------------|---------------------|-----------|-----------------------------
1,000 | 5 | 17.5 µs, 22.5 KB | 9.5 µs, 1.1 KB | 18.2 µs (noisy), 0.5 KB
1,000 | 100 | 23.6 µs, 49.4 KB | 8.9 µs, 14.9 KB | 6.1 µs, 7.4 KB
100,000 | 5 | 3.38 ms, 2.6 MB | 2.34 ms, 1.1 KB | 3.09 ms, 0.5 KB
100,000 | 10,000 | 10.6 ms, 4.7 MB | 1.35 ms, 1.3 MB | 1.06 ms, 0.7 MB
1,000,000 | 5 | 36.5 ms, 21.0 MB | 24.2 ms, 1.2 KB | 32.0 ms, 0.6 KB
1,000,000 | 100,000 | 202 ms, 46.3 MB | 20.5 ms, 12.1 MB | 15.7 ms, 6.0 MB

`AggregateBy` showed the same pattern against `GroupBy` followed by `Sum`. With a million items and 100,000 keys it took 32.6 ms and 21.7 MB, compared with 239 ms and 54.3 MB. With five keys it took 27.8 ms and 1.4 KB, compared with 50.1 ms and 21.0 MB.

On speed alone:

- `CountBy` was faster than `GroupBy` plus `Count` in every row, between roughly 1.5 and 10 times faster. The gap was widest with many distinct keys.
- It landed close to a hand-written dictionary loop, which is about what you would hope for from a method that does the same job.
- On small collections the absolute difference is microseconds, so readability is the better reason to pick it.

### Collection Size Matters More Than Speed
The allocation column tells the more important story. Look at the five-key rows as the collection grows. `GroupBy` allocated 22.5 KB at 1,000 items, 2.6 MB at 100,000, and 21.0 MB at a million. `CountBy` allocated about 1.1 KB at every size. The first grows with the number of items. The second grows only with the number of distinct keys.

That makes sense once you look at what each one does. `GroupBy` has to keep every element, sorted into its group, until you are finished with it, even though you only wanted a count. `CountBy` needs one counter per key and nothing else. The consequences show up in the garbage collector. At 100,000 and 1,000,000 items the `GroupBy` runs triggered generation 2 collections (the Gen2 column in the BenchmarkDotNet output was non-zero), while `CountBy` with five keys triggered no collections of any generation. If your service runs in a container with a memory limit, transient memory that scales with the size of the input is the kind of thing that eventually becomes an incident, and this is memory you were allocating only to throw it away.

There is a limit. `CountBy` still needs an entry for each distinct key, which is why it allocated 12.1 MB in the million-item, 100,000-key row. Its advantage narrows as the number of distinct keys approaches the number of items. I did not test that extreme, so treat it as reasoning, not a measurement.

Treat all of this as directional. It comes from a single machine, a short job (a few iterations, so some rows have wide error margins, for example the million-item `GroupBy` row with 100,000 keys), simple objects, and cheap key selectors. Your data and hardware will differ, so measure your own hot path before you rewrite anything.

### When I Would Use It
- Counting or totaling a collection you already have in memory, such as a cached list, a parsed file, a batch import, or an API response.
- A large collection (hundreds of thousands of items or more), where the allocation savings are visible, especially in memory-limited containers.
- Anywhere you would otherwise write `GroupBy(...).ToDictionary(g => g.Key, g => g.Count())` and the result is the only thing you need.

### When I Would Not
- Anywhere the data is still in the database. Count it there, with a query that returns only the totals.
- When you need the items in each group as well as the count. `GroupBy` is still the right tool, because it hands you the groups.
- When the collection is small and the code is not hot. At that point it is a matter of taste, not performance. Size is what moves the needle.

> I am thankful for LINQ, which I still think of as typed SQL for anything I can enumerate.

## Declarative: Say What, Not How
A declarative style states the result and leaves the method to someone else. The LINQ query above is one example. The interesting part is what C# does with a lambda when you ask it to keep the lambda as data.

### Expression Trees
Assign a lambda to a `Func<T, bool>` and the compiler produces code. Assign the same lambda to an `Expression<Func<T, bool>>` and the compiler produces a tree describing the code, which your program can inspect:

```csharp
Expression<Func<OrderSummary, bool>> big = o => o.Total > 100m && o.Status == "Shipped";

Console.WriteLine(big);
// o => ((o.Total > 100) AndAlso (o.Status == "Shipped"))

var body = (BinaryExpression)big.Body;
Console.WriteLine(body.NodeType);   // AndAlso
Console.WriteLine(body.Left);       // (o.Total > 100)
Console.WriteLine(body.Right);      // (o.Status == "Shipped")

Func<OrderSummary, bool> compiled = big.Compile();   // or turn it into a delegate when you want to run it
```

That is how a library can read your `Where` clause and do something other than run it, such as translate it to SQL. EF Core works this way, which is why a `Where` in an EF query can become a `WHERE` in the database. If you want EF Core in practice, [Upgrading to EF Core 11](/posts/upgrading-to-ef-core-11/) is the place to start.

One interaction with a later section is worth knowing now. Native AOT, which compiles a program to native machine code when you publish it instead of while it runs, gets its own section below. Its documentation says that `System.Linq.Expressions` always uses its interpreted form in a Native AOT app, which is slower than runtime-generated compiled code.[[12](#references)] Expression trees still work there. They just run in a different way.

> I am thankful that a lambda can be data as well as code, which is the trick that lets EF Core read a `Where` instead of just running it.

## Generic: Write It Once
Generics arrived in C# 2, and the runtime keeps the type arguments, so a `List<int>` and a `List<string>` are different types while the program runs, not an erased `List` with casts behind it. That one decision helps explain why so much of the framework feels typed.

### Generic Math
For years there was one thing generics could not do, which was arithmetic. A method that added up numbers needed an overload per numeric type, because nothing said that `int` and `decimal` both support `+`. .NET 7 and C# 11 fixed that by adding interfaces for numeric operations, built on static abstract members.[[13](#references)] Constrain a type parameter to `INumber<T>` and you can write the algorithm once:

```csharp
static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
{
    var total = T.Zero;
    foreach (var value in values) total += value;
    return total;
}

Sum([1, 2, 3]);        // 6
Sum([1.5, 2.5]);       // 4
Sum([1.10m, 2.20m]);   // 3.30
```

Notice `T.Zero`. The interface members are static, so you call them on the type parameter itself.

### `EqualityComparer<T>.Create`
This one is new in .NET 11. Sooner or later, some API wants an `IEqualityComparer<T>`, and you want to compare by one property. For example, a `HashSet` of orders that should treat two orders with the same email address as duplicates, ignoring case. Until now that meant writing a class:

```csharp
public sealed class OrderEmailComparer : IEqualityComparer<OrderSummary>
{
    public bool Equals(OrderSummary? x, OrderSummary? y) =>
        string.Equals(x?.Email, y?.Email, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode(OrderSummary obj) =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Email);
}

var unique = new HashSet<OrderSummary>(orders, new OrderEmailComparer());
```

.NET 11 adds a factory method that builds the comparer from a key selector, and accepts an optional comparer for the key itself:[[5](#references)]

```csharp
var byEmail = EqualityComparer<OrderSummary>.Create(o => o?.Email, StringComparer.OrdinalIgnoreCase);

var unique = new HashSet<OrderSummary>(orders, byEmail);
```

The class, the null handling, and the hash code you have to keep consistent with `Equals` all go away. LINQ already has `DistinctBy` for the one-off case. This is for the places where an API takes a comparer, such as the `HashSet` and `Dictionary` constructors.

One detail if you have nullable reference types enabled. The key selector receives a parameter that is annotated as possibly null, because a comparer can be handed a null item. Writing `o => o.Email` produced a nullable warning (CS8602) with the .NET 11 SDK, and `o => o?.Email` compiled cleanly. The docs example uses a tuple, a value type, so it never runs into this. Leaving out the second argument works too, but then the keys are compared with their default comparer, so `A@x.com` and `a@x.com` count as different. In a test run on .NET 11, the case-insensitive comparer produced two unique orders from three, and the default produced three.

> I am thankful for generics, because I get to write an algorithm once and still have the compiler check every type that uses it.

## Asynchronous and Concurrent: Waiting Without Blocking
C# 5 baked `async` and `await` into the language, and the official version history is not shy about calling them the stars of that release.[[8](#references)] The scalability argument is simple. A thread that is waiting on I/O is a thread doing nothing, and `await` lets it go do something else. Used badly, it costs real money, which is the subject of [Async/Await and What It Actually Costs You in the Cloud](/posts/async-await-web-api-cloud-costs/). The language gives you the mechanism, and you still have to hold it correctly.

### Async Streams and Channels
`async` handles a single result that arrives later. Many real workloads produce a sequence of results over time. C# 8 added `IAsyncEnumerable<T>` and `await foreach`, backed by .NET Core 3.0 and .NET Standard 2.1.[[14](#references)] An async iterator looks just like a normal one:

```csharp
static async IAsyncEnumerable<int> ReadPagesAsync([EnumeratorCancellation] CancellationToken ct = default)
{
    for (var page = 1; page <= 3; page++)
    {
        await Task.Delay(10, ct);   // stand-in for a real request
        yield return page;
    }
}

await foreach (var page in ReadPagesAsync())
    Console.Write($"page {page}; ");

// page 1; page 2; page 3;
```

When producers and consumers run at different speeds, `System.Threading.Channels` sits between them. A bounded channel has a maximum size, and with `FullMode = Wait` a producer that gets too far ahead is made to wait, so memory stays bounded.[[15](#references)] The reader exposes the channel as an async stream:

```csharp
var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(2) { FullMode = BoundedChannelFullMode.Wait });

var producer = Task.Run(async () =>
{
    for (var i = 1; i <= 5; i++) await channel.Writer.WriteAsync(i);
    channel.Writer.Complete();
});

await foreach (var item in channel.Reader.ReadAllAsync())
    Console.Write($"item {item}; ");

await producer;

// item 1; item 2; item 3; item 4; item 5;
```

Channels ship in the framework from .NET Core 3.0 on, and as a NuGet package for older targets.[[15](#references)] I ran this one and checked the output. I did not measure the waiting, so the backpressure behavior here is the documented behavior, not something I timed.

### `Task.WhenEach`
Suppose you start several asynchronous calls and want to act on each result as soon as it arrives, rather than waiting for the slowest one. The traditional answer is a loop around `Task.WhenAny` with a list you keep trimming:

```csharp
var pending = new List<Task<string>>(tasks);

while (pending.Count > 0)
{
    var finished = await Task.WhenAny(pending);
    pending.Remove(finished);

    try { Console.WriteLine($"done: {await finished}"); }
    catch (Exception ex) { Console.WriteLine($"failed: {ex.Message}"); }
}
```

It is not hard, but it is bookkeeping, and bookkeeping is where bugs like to live. .NET 9 added `Task.WhenEach`, which hands you the tasks in completion order as an `IAsyncEnumerable`:[[5](#references)]

```csharp
async Task<string> CheckWarehouseAsync(string name, int delayMs)
{
    await Task.Delay(delayMs); // stand-in for a real HTTP call
    if (name == "bad") throw new InvalidOperationException("boom");
    return name;
}

var tasks = new[]
{
    CheckWarehouseAsync("slow", 300),
    CheckWarehouseAsync("fast", 50),
    CheckWarehouseAsync("bad", 150),
};

await foreach (var finished in Task.WhenEach(tasks))
{
    try { Console.WriteLine($"done: {await finished}"); }
    catch (Exception ex) { Console.WriteLine($"failed: {ex.Message}"); }
}

// done: fast
// failed: boom
// done: slow
```

There is no list to maintain and no `Remove` to forget. One detail worth knowing is that a faulted task does not throw out of the enumeration. The loop keeps going, and the exception surfaces when you `await` that individual task, which is why the `try` block lives inside the loop. That is the behavior I observed in the run above, and it is usually what you want.

Before you swap every `WhenAny` loop for it, here is what it does and does not do. I checked each of these in a test project on .NET 11.

- **It returns tasks in completion order, not input order.** With tasks that took 200, 20, and 100 milliseconds, the loop saw them in the order 20, 100, 200. If you need to know which input produced which result, return that identity as part of the result.
- **It does not limit concurrency.** It takes tasks that are already running, so it has no say in how many run at once. To cap that, use something like `Parallel.ForEachAsync` or a `SemaphoreSlim` when you start the work.
- **Leaving the loop early does not cancel anything.** After a `break`, only the first task had finished, and 400 milliseconds later the other two had finished anyway. Pass a `CancellationToken` into the work if you want it to stop.
- **The set of tasks is fixed when you start.** I added a task to the list while enumerating. Nothing threw, the new task ran to completion, and it was never yielded.
- **It needs .NET 9.**

The upside is more than tidiness. A `WhenAny` loop looks at the whole pending list on every iteration and removes from the middle of it, so its cost should grow with the square of the number of tasks, which fits what happened here. These are rough timings for tasks that had already completed, which is the best case for the new method. Each is the median of five runs with a `Stopwatch`, not a BenchmarkDotNet run:

Tasks | `WhenAny` loop | `Task.WhenEach`
------|----------------|----------------
1,000 | 6.7 ms | 0.2 ms
10,000 | 351 ms | 0.7 ms
20,000 | 1,631 ms | 1.4 ms

With three or four tasks none of this matters, and you would choose it for readability. With thousands of tasks, the old loop becomes the thing you are waiting on.

> I am thankful for async streams and channels, which let producers and consumers work at their own speed without anyone hand-building the plumbing.

## Metaprogramming: Code That Writes Code
Metaprogramming in .NET used to mean reflection, which inspects types while the program runs and costs you at runtime. C# 9 added the pieces that let a compiler plug-in generate code at compile time instead, which is what source generators do.[[8](#references)]

### Source Generators
A source generator is a compiler plug-in. At compile time it reads your code, plus any additional files you give it, and adds new source files to the compilation.[[16](#references)] You mark a `partial` type or method, and the generator writes the other half.

The built-in example most of us can use today is the regular expression generator. .NET 7 introduced it, and the documentation recommends it over `RegexOptions.Compiled` where possible because it can help an app start faster, run more quickly, and be more trimmable.[[17](#references)] You write a partial method and mark it, and the compiler fills in the body:

```csharp
public static partial class Validators
{
    [GeneratedRegex(@"^[A-Z]{2}\d{4}$", RegexOptions.CultureInvariant)]
    public static partial Regex OrderNumber();
}

Validators.OrderNumber().IsMatch("AB1234");   // True
Validators.OrderNumber().IsMatch("ab-12");    // False
```

Compared with `new Regex(...)`, the parsing and optimization work happens at build time, and what ships is ordinary C# you can read in your IDE.

### Reading the Generated Code
Generated code is not hidden. Add two properties to the project file and the compiler writes each generator's output to disk:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
```

For the one-line order number regex above, the regex generator wrote a 149-line file. It opens with a comment that explains the pattern in plain English, and this is that comment with the markup trimmed:

```
Pattern:  ^[A-Z]{2}\d{4}$
Options:  RegexOptions.CultureInvariant
Explanation:
○ Match if at the beginning of the string.
○ Match a character in the set [A-Z] exactly 2 times.
○ Match a Unicode digit exactly 4 times.
○ Match if at the end of the string or if before an ending newline.
```

Below that is a class derived from `Regex` with the matching logic written out as C#. Because it is a file you can open, you can read it, and you can compare it between versions to see exactly what the generator changed.

### Mistakes Fail the Build
A bad pattern passed to `new Regex(...)` throws an exception when that line runs. The same mistake passed to `[GeneratedRegex]` stops the build, with an error that says what is wrong:

```
error SYSLIB1042: The specified regex is invalid. 'Invalid pattern '^(unclosed' at offset 10. Not enough )'s.'
```

The compiler also reports a second error (CS8795), because the method was left without a body. Moving a failure from a user's session to your build is the quiet payoff of generating code at compile time.

### More Generators in the Box
Regular expressions are not the only generator you already have. `System.Text.Json` uses reflection by default, and the documentation warns that a basic serialization call can break a Native AOT app, which does not support all the reflection APIs it needs. Source generation is the supported way around that.[[18](#references)] You declare a context and ask for the types you need:

```csharp
[JsonSerializable(typeof(OrderSummary))]
internal partial class AppJsonContext : JsonSerializerContext;

var order = new OrderSummary("AB1234", "Shipped", 150m, "a@x.com");
var json = JsonSerializer.Serialize(order, AppJsonContext.Default.OrderSummary);
// {"Number":"AB1234","Status":"Shipped","Total":150,"Email":"a@x.com"}
```

The logging library has one as well. You write a partial method with a message template, and the generator writes the implementation and reports mistakes in the template as compile-time diagnostics:[[19](#references)]

```csharp
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderNumber} shipped to {City}")]
    public static partial void OrderShipped(this ILogger logger, string orderNumber, string city);
}

logger.OrderShipped("AB1234", "Newark");   // logs "Order AB1234 shipped to Newark" at Information level
```

Both of these compiled and ran in the .NET 11 SDK container with warnings treated as errors. For that small project, the JSON generator wrote six files and the logging generator wrote one.

There are trade-offs worth knowing before you reach for a generator:

- **The type or method must be `partial`.** The generator needs somewhere to put its half.
- **The inputs must be known at compile time.** A regex pattern assembled at run time from user input cannot use the generator.
- **You get files you did not write.** They are readable, and you should not edit them, so the fix for a surprising result is always in the attribute or the code that feeds it.
- **Writing your own generator is a project, not a snippet.** The documentation points to incremental generators and a cookbook for that.[[16](#references)] I did not write one for this post.

That is the quiet connection between this section and the next one. Native AOT cannot support every reflection API, so a generator does the reflection-style work at build time instead, and you keep the convenience.

> I am thankful that a typo in a regex pattern now fails the build instead of waiting to surprise someone at run time.

## Low-Level: Down to the Metal Without Leaving
Managed does not have to mean slow or distant from the hardware. C# lets you drop down a level when you need to, and it lets the runtime or the compiler do work for you when you do not.

### `Span<T>` and `Memory<T>`
A `Span<T>` is a window onto memory, whether that is an array, a stack buffer, or part of a string, with no copy and no allocation. `Span<T>` is a `ref struct`, a feature added in C# 7.2, which means a span can only live on the stack.[[8](#references)] Parsing a comma-separated row is a good test. Here is the version that allocates, and then the one that does not:

```csharp
// Allocates an array, a string per field, and LINQ iterators
static int SumCsvSplit(string line) => line.Split(',').Select(int.Parse).Sum();

// Allocates nothing
static int SumCsv(ReadOnlySpan<char> line)
{
    var total = 0;
    while (!line.IsEmpty)
    {
        var comma = line.IndexOf(',');
        var field = comma < 0 ? line : line[..comma];
        total += int.Parse(field);
        line = comma < 0 ? default : line[(comma + 1)..];
    }
    return total;
}
```

I measured the allocations with `GC.GetAllocatedBytesForCurrentThread` over 1,000 calls on a ten-field row. The span version allocated 0 bytes. The `Split` version allocated 472,000 bytes, or 472 bytes per call, and that includes the LINQ overhead, so a hand-written loop over `Split` would land somewhere between. The span version is also more code and more to get wrong, so this is a tool for hot paths, not a default. C# 14 added more implicit conversions to make spans easier to pass around, which I cover in [Spanning the Gap with an Array of Options](/posts/spanning-the-gap-with-an-array-of-options/).

### Native AOT
Normally .NET compiles your code to IL, and a just-in-time compiler turns it into machine code while the program runs. Native AOT compiles to machine code at publish time instead. The documentation says Native AOT apps start faster and use less memory, they do not use a JIT at run time, and they can run on machines with no .NET runtime installed.[[12](#references)] You turn it on with one property:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
```

I published a small console app that uses the generated regex from the previous section this way, in the .NET 11 SDK container. It needed `clang` installed first, which the documentation lists as a Linux prerequisite (I also installed `zlib1g-dev`, and I did not test without it). The result was a single 1.2 MB native executable that ran the regex correctly, and `RuntimeFeature.IsDynamicCodeSupported` was `False` at run time. I did not measure startup time, so I am leaning on the documentation for the startup and memory claims.

The trade is real. The documentation lists no dynamic loading, no runtime code generation such as `System.Reflection.Emit`, and a requirement that the code be trimmable, plus the interpreted expression trees mentioned earlier.[[12](#references)] Native AOT has been available since .NET 7, and .NET 8 added ASP.NET Core support.[[12](#references)] Code that avoids reflection, such as the generated regex from the previous section, is what makes it a reasonable target.

{{< aside >}}
**Libraries.** A Native AOT app is only as compatible as the libraries it uses. If you write a library, set `<IsAotCompatible>true</IsAotCompatible>` in its project file. That turns on the trim, single-file, and AOT analyzers, and the AOT warnings need `net8.0` or later.[[12](#references)] In a test library on .NET 11, a plain `JsonSerializer.Serialize(order)` raised `IL2026` and `IL3050`, and `Type.GetType(name)` raised `IL2057`. Those are the same reflection patterns the source generator sections avoid. Setting `VerifyReferenceAotCompatibility` to `true` also warns about referenced assemblies that have not been annotated.[[12](#references)]
{{< /aside >}}

### JIT and Dynamic PGO
The other side of that trade is the JIT, which has been getting smarter. Dynamic profile-guided optimization (PGO) lets the runtime observe how your code behaves while it runs and recompile the hot paths with that knowledge. It has been enabled by default since .NET 8. The .NET 8 documentation says it improves performance by about 15 percent on average, and that in a suite of roughly 4,600 benchmarks, 23 percent saw improvements of 20 percent or more.[[5](#references)] I did not benchmark it myself, so those figures are Microsoft's. If you ever need to turn it off, the setting is `DOTNET_TieredPGO=0` or `<TieredPGO>false</TieredPGO>` in the project.

Put the last two together and you get the point of this whole section. The same C# can be published as JIT-compiled code that tunes itself while it runs, or as a native binary that starts quickly and has no JIT at all. That is a deployment decision, made at publish time, not a choice of language.

> I am thankful that I can drop down to spans or native compilation when a problem calls for it, and ignore both the rest of the time.

## The Ecosystem Around It
A language is only as pleasant as the tools around it, and the tooling is part of what makes the mix above work on any operating system. The C# snippets in this post were compiled and run in a Linux container from the official .NET 11 SDK image, using the same `dotnet` command line you would use anywhere else. One small improvement to that command line landed in .NET 11.

### `dotnet run -e`
Sometimes you just want to run a project with one environment variable set, and how you do that depends on your shell. In PowerShell you set the variable first, and it stays set for the rest of the session:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

In bash you put the assignment in front of the command:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

You can also edit `launchSettings.json`, and then you have to remember to undo the edit. .NET 11 adds a switch to `dotnet run` that takes the variable directly:[[5](#references)]

```bash
dotnet run -e ASPNETCORE_ENVIRONMENT=Development -e LOG_LEVEL=Debug
```

That works the same in any shell and leaves nothing set afterward. A small program that prints the two variables showed `Development` and `Debug` when launched this way, and nothing when launched with a plain `dotnet run`. It is a small thing, and it is the kind of small thing worth being thankful for.

## Where Everything Landed
Here is every feature in this post, grouped by the paradigm it belongs to. All of them are generally available. Only .NET 10 and .NET 11 are currently supported by Microsoft, so some rows describe things you have had for years and some describe things you have had for two weeks.

Feature | Paradigm | Available Since
--------|----------|-----------------
Nullable reference types | Correctness | C# 8
Guard clause helpers | Correctness | .NET 6 for `ThrowIfNull`, .NET 7 for `ThrowIfNullOrEmpty` and `ObjectDisposedException.ThrowIf`, .NET 8 for the rest
Null-conditional assignment | Correctness | C# 14 (.NET 10 SDK)
Records | Object-oriented | C# 9 (`record struct` in C# 10)
Extension members | Object-oriented | C# 14 (.NET 10 SDK)
The `field` keyword | Object-oriented | C# 14 (.NET 10 SDK)
LINQ | Functional | C# 3
Pattern matching | Functional | C# 7 onward (list patterns in C# 11)
Union types and closed hierarchies | Functional | C# 15 (.NET 11)
`CountBy` and `AggregateBy` | Functional | .NET 9
Expression trees | Declarative | C# 3
Generics | Generic | C# 2
Generic math | Generic | C# 11 and .NET 7
`EqualityComparer<T>.Create` | Generic | .NET 11
`async` and `await` | Asynchronous | C# 5
Async streams | Asynchronous | C# 8, .NET Core 3.0
Channels | Asynchronous | .NET Core 3.0 (NuGet package for older targets)
`Task.WhenEach` | Asynchronous | .NET 9
Source generators | Metaprogramming | C# 9 (regex generator in .NET 7)
`Span<T>` and `Memory<T>` | Low-level | C# 7.2 (`ref struct`)
Native AOT | Low-level | .NET 7 (ASP.NET Core support in .NET 8)
Dynamic PGO | Low-level | On by default in .NET 8
`dotnet run -e` | Tooling | .NET 11 SDK

The C# 14 features are compiler features, so they do not require a .NET 10 runtime. I compiled the `field` and null-conditional assignment snippets against every target from `net6.0` through `net11.0` with `<LangVersion>14</LangVersion>`, and they needed the C# 14 compiler, meaning the .NET 10 SDK or later, and nothing else. The library features need the version listed, because they are new APIs.

## Wrapping Up
The paradigms in this post are not new, and most of them are not unique to C#. What I am thankful for is that I do not have to leave the language, the type system, or the toolchain to change style. In a single modern C# method you might guard the arguments with a throw helper, model the data with a record, filter it with LINQ, branch on its shape with a pattern, and `await` a channel, and the compiler checks the seams between all of them.

The small features are what keep that pleasant. If you want a short list to act on:

- Replace hand-written argument checks with the built-in throw helpers (.NET 6 to 8, depending on the helper).
- Use `?.` on the left side of an assignment where you were writing a null check (C# 14).
- Let `field` replace backing fields whose only job is to transform a value (C# 14).
- Replace in-memory `GroupBy(...).ToDictionary(...)` counting with `CountBy` (.NET 9), but not database queries.
- Reach for `EqualityComparer<T>.Create` before writing another comparer class (.NET 11).
- Replace `Task.WhenAny` loops with `Task.WhenEach` (.NET 9).
- Use `dotnet run -e` when you need a one-off environment variable (.NET 11 SDK).

C# is an amazingly expressive language. It is object-oriented at its core, and it also lets you mix paradigms as the problem demands. It is not the hot or cool language, but it is the language that many clients demand for their production applications. That is a good combination to be thankful for.

## References
1. Other languages named in the introduction: Swift [macros](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/macros/) and the [Swift 6.2 release](https://www.swift.org/blog/swift-6.2-released/) (`Span`); Kotlin [coroutines](https://kotlinlang.org/docs/coroutines-overview.html) and the [Symbol Processing API](https://kotlinlang.org/docs/ksp-overview.html); [Scala 3 macros](https://docs.scala-lang.org/scala3/guides/macros/macros.html); [Rust macros](https://doc.rust-lang.org/book/ch20-05-macros.html); F# [computation expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions); and the OpenJDK JEPs for [records](https://openjdk.org/jeps/395), [sealed classes](https://openjdk.org/jeps/409), [pattern matching for switch](https://openjdk.org/jeps/441), [virtual threads](https://openjdk.org/jeps/444), and [value objects (preview)](https://openjdk.org/jeps/401).
2. Microsoft. [What's new in .NET 11](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview), _Microsoft Learn_, describes .NET 11 as generally available. The release date of November 10, 2026 is from the [.NET 11 release announcement](https://github.com/dotnet/core/discussions/10711), _dotnet/core_. The support status (.NET 10 and .NET 11 active, .NET 8 and .NET 9 end of life as of November 10, 2026) is from the [.NET release index](https://github.com/dotnet/core/blob/main/release-notes/releases-index.json), _dotnet/core_.
3. Microsoft. [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-reference-types), _Microsoft Learn_.
4. Guard clause sources. The origin of the built-in helpers is [dotnet/runtime issue #48573](https://github.com/dotnet/runtime/issues/48573). The Ardalis.GuardClauses licensing announcement is [NimbleTools, October 29, 2026](https://nimbletools.io/news/ardalis-nuget-packages/), and the library is at [github.com/ardalis/GuardClauses](https://github.com/ardalis/GuardClauses), with the [5.0.0 package on NuGet](https://www.nuget.org/packages/Ardalis.GuardClauses/5.0.0). The analyzer rules are [CA1510](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1510) through CA1513 on Microsoft Learn.
5. Microsoft Learn, "What's new" pages. [C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) covers the `field` keyword and null-conditional assignment. [.NET 8 runtime](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/runtime) covers dynamic PGO. [.NET 9 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/libraries) covers `CountBy`, `AggregateBy`, and `Task.WhenEach`. [.NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) covers `EqualityComparer<T>.Create`. [.NET 11 SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk) covers `dotnet run -e`.
6. Microsoft. [Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record), _Microsoft Learn_.
7. Microsoft. [Extension declaration](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/extension), _Microsoft Learn_.
8. Microsoft. [The history of C#](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history), _Microsoft Learn_. Source for which C# version introduced generics, LINQ, `async`, nullable reference types, async streams, records, `record struct`, source generator support, list patterns, and generic math.
9. Microsoft. [Past releases of Entity Framework](https://learn.microsoft.com/en-us/ef/ef6/what-is-new/past-releases), _Microsoft Learn_. The first version of Entity Framework shipped in 2008 with .NET Framework 3.5 SP1 and Visual Studio 2008 SP1.
10. Microsoft. [Patterns](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns), _Microsoft Learn_.
11. Microsoft. [Union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union), _Microsoft Learn_.
12. Microsoft. [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/), _Microsoft Learn_.
13. Microsoft. [Generic math](https://learn.microsoft.com/en-us/dotnet/standard/generics/math), _Microsoft Learn_.
14. Microsoft. [Generate and consume asynchronous streams](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream), _Microsoft Learn_.
15. Microsoft. [System.Threading.Channels library](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels), _Microsoft Learn_.
16. Microsoft. [.NET Compiler Platform SDK](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/), _Microsoft Learn_. Describes source generators as compile-time metaprogramming and points to the incremental generator cookbook.
17. Microsoft. [.NET regular expression source generators](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-source-generators), _Microsoft Learn_.
18. Microsoft. [How to use source generation in System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation), _Microsoft Learn_.
19. Microsoft. [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation), _Microsoft Learn_.
