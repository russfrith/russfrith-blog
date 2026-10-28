Note: `get_mut(&mut v, 0)` and `get_mut(&mut v, 1)` touch disjoint elements and would
never alias at runtime, but the borrow checker cannot see through the function
boundary to prove that, so it rejects genuinely correct code. split_at_mut proves
disjointness by construction (it's implemented with carefully audited unsafe code
inside the standard library) and the compiler accepts it.