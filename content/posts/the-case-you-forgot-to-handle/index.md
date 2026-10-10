---
title: "The Case You Forgot to Handle: How C# 15 Unions Fix Your Switch"
description: "C# 15's union types close the gap that object and interface switches leave open, turning a forgotten case into a compiler error instead of a 2 a.m. page"
date: 2026-10-09T17:41:19-04:00
draft: true
cover:
  image: photorealistic-night-pro-2842606.jpg
  alt: "A developer working alone at a desk late at night"
  caption: "Image created by author using [Flux 2 Pro](https://bfl.ai/) by Black Forest Labs"
tags: ["csharp", "dotnet"]
categories: ["programming"]
---

Somewhere in your codebase there is a `switch` with a `_ =>` arm that throws for anything it doesn't recognize, because the type it's switching on never told the compiler what "everything" meant. This is a story about that arm, and the C# 15 feature that retires it.

## 2:03 a.m.

The pager goes off. Finance has noticed that bank transfers, the thing your team shipped last sprint, are being processed for exactly zero dollars in fees. Not a discount. Not a promotion. Just free, quietly, for every customer who picked that option.

You open the code expecting something exotic. It is a `switch`. It has been there for two years. It has never been wrong, right up until the day a new payment type showed up and nothing in the compiler said a word.

## The crime scene

Here is the code, lightly anonymized to protect the guilty:

```csharp
record Card(decimal Amount);
record PayPal(decimal Amount);
record BankTransfer(decimal Amount);   // added last sprint

decimal Fee(object payment) => payment switch
{
    Card c   => c.Amount * 0.029m,
    PayPal p => p.Amount * 0.034m,
    _        => throw new ArgumentOutOfRangeException(nameof(payment)),
};
```

That's not lazy code. Whoever wrote it knew better than to guess at a fee for something it didn't recognize, so it throws. It just never got the chance to: a few layers up, the code that calls `Fee` wraps the whole transfer in a handler that catches anything thrown, logs a warning, and lets the transfer through with no fee rather than block a customer's money over a bug. That's a defensible call when the only thing that could throw was a genuine edge case. `BankTransfer` wasn't an edge case, though, and nobody was reading that warning in the logs.

The real culprit is the parameter type. `Card`, `PayPal` and `BankTransfer` share no base class, so `object` was the only type that fit all three. And `object` has no idea which types you meant to allow, so it can't tell you when you forgot one.

## Same bug, C# 15

C# 15 adds a `union` declaration: a name, and the closed list of types a value is allowed to be. The case types don't need to be related.

```csharp
public union Payment(Card, PayPal, BankTransfer);

decimal Fee(Payment payment) => payment switch
{
    Card c         => c.Amount * 0.029m,
    PayPal p       => p.Amount * 0.034m,
    BankTransfer b => b.Amount * 0.008m,
};
```

There is no catch-all, because there is nothing left to catch. Delete the `BankTransfer` arm and the compiler says so, at build time, by name:

```
warning CS8509: The switch expression does not handle all possible values of its input type 'Payment' (it is not exhaustive). For example, the pattern 'BankTransfer' is not covered.
```

Better still, the next time someone adds a fourth payment type to the union's list, every `switch` over `Payment` in the codebase raises that same warning at build time, long before a pager does. (More on turning that warning into a hard stop below.)

Passing values in stays simple, too. A `Card` goes where a `Payment` is expected, so call sites read the way they always did.

## Making the compiler your night shift

Exhaustive matching means the compiler knows every case a union can hold, so it can check your `switch` against that list. That check is a warning by default, so make it loud:

```xml
<PropertyGroup>
  <WarningsAsErrors>CS8509</WarningsAsErrors>
</PropertyGroup>
```

`CS8509` is the existing "switch expression is not exhaustive" diagnostic. Check that your SDK reports it for unions the same way. With it promoted to an error, a forgotten case fails the build, not the 2 a.m. shift.

There is one habit to drop. A discard arm (`_ =>`), even one that throws, tells the compiler you've handled everything, which switches the check off. That's exactly the arm that let `BankTransfer` slip past silently further up the call chain. Resist adding one to a switch over a union, unless you truly want a fallback.

If you want shared logic without a switch at every call site, a union can also carry a body with helper members, so the one `switch` lives next to the type it describes.

## When the cases already share a base class

`Payment` needed a union because `Card`, `PayPal` and `BankTransfer` share no common ancestor. Plenty of designs don't have that problem; they're already an inheritance hierarchy, and the gap is the same one: nothing stops someone from adding a subclass without updating every switch over the base type.

That's what `closed` is for. Mark the base class `closed` and the compiler tracks every subclass declared in the same file, the same way it tracks a union's case list:

```csharp
public closed class Shape;
public class Circle(double Radius) : Shape;
public class Square(double Side) : Shape;

double Area(Shape shape) => shape switch
{
    Circle c => Math.PI * c.Radius * c.Radius,
    Square s => s.Side * s.Side,
};
```

Add a `Triangle : Shape` anywhere else in the file and forget to extend `Area`, and you get the same `CS8509` warning `Payment` would have given you, for the same reason: the compiler now knows the full list of subtypes and checks your switch against it. Everything from here down is identical to the union story, promote `CS8509` to an error, don't add a discard arm, and the forgotten case fails the build instead of the on-call shift.

The difference is purely about what your types look like going in. Unrelated types that need a common label: union. Subclasses of a base you already have: `closed`.

## Fine print

- **It's preview.** C# 15 ships with .NET 11 in November, but both unions and `closed` classes are still preview features, so syntax could shift. Install the .NET 11 SDK and set the language version to preview. Preview 5 or later includes the runtime types.
- **It boxes, for now.** A union compiles to a struct holding a single nullable `object`, so value-type cases like `int` are boxed by default. A non-boxing pattern exists for custom union types, if you need it.
- **It's a type union, not an F# union.** A value is a `Card` or a `PayPal`. Cases don't have names of their own, so two cases of the same type can't be told apart.
- **Your libraries still work.** OneOf, ErrorOr and friends don't have to be rewritten in the new syntax to benefit.
- **`closed` only sees its own file.** Subclasses have to be declared in the same file as the closed base, so the compiler can actually enumerate them.

## Sources

- [What's new in C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15), Microsoft Learn
- [Explore union types in C# 15](https://devblogs.microsoft.com/dotnet/csharp-15-union-types/), .NET Blog, April 2, 2026
- [Explore new features available in C# 15 preview](https://devblogs.microsoft.com/dotnet/explore-csharp-15), .NET Blog, August 24, 2026
- [Union Types Are Finally Coming to C#](https://milanjovanovic.tech/blog/union-types-are-finally-coming-to-csharp), Milan Jovanovic

The payment scenario is invented for illustration, and the code is based on the published preview syntax. Check it against the SDK you install.
