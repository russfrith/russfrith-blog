using Xunit;

// Integration and contract tests truncate and reseed the same real Postgres
// database. Running test collections in parallel would let one collection's
// truncate race another's insert, so the whole assembly runs sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
