# Companion code conventions

C# style rules for companion code across posts. These apply on top of whatever a given post's companion code needs architecturally; this file is about syntax-level style, not architecture.

## `var` vs. an explicit type

Use `var` when the right-hand side already names the type, so nothing is lost by hiding it:

```csharp
var order = Order.Create(customerId);          // "Order.Create" names the type
var connection = CreateConnection();            // "Connection" is in the method name
var command = new CommandDefinition(...);       // "new CommandDefinition" names the type
var header = await multi.ReadSingleOrDefaultAsync<OrderHeaderRow>(); // generic argument names it
```

Use the explicit type when the call doesn't name it, so a reader isn't left guessing what a repository, service, or other non-obvious call returns:

```csharp
Order order = await _repository.GetByIdAsync(command.OrderId, cancellationToken)
    ?? throw new DomainException($"Order {command.OrderId} was not found.");
```

`_repository.GetByIdAsync` doesn't say "Order" anywhere in the call, so `var` forces a reader to go look up the method signature to know what `order` is. The explicit type answers that on the spot.

This is about readability, not about banning `var`. Ordinary LINQ chains, test helper calls, and other cases where the surrounding code makes the type obvious within a line or two should stay as `var`; don't mechanically expand every declaration.
