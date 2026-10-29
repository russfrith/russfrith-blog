Mismatch with prediction / with Go: there is no `-race`-equivalent built into rustc,
and safe Rust makes the naive version (plain shared mutable state across threads, no
synchronization) a COMPILE ERROR, not a runtime race -- getting an actual race at all
requires opting into `unsafe`. A first attempt with plain back-to-back increments
(static mut, 200,000 iterations, no induced delay) did not reproduce a lost update,
same finding as Python's scenario 2 -- see PREDICTIONS.md. Inserting a
`thread::yield_now()` between the raw read and raw write made it reliable, same
technique as the Python version.

broken.rs and fixed.rs both run 200,000 iterations per thread (400,000 total,
`expected 400000` in both halves of output.txt) -- an earlier version of this demo
reduced broken.rs to 200 iterations instead of adding the yield, which made the
two sides' expected totals look like a typo; they're not needed once the yield
alone is enough to make the race reliable at the same iteration count as fixed.rs.

An earlier captured version of output.txt also incorrectly showed "400" (not
400,000) for the fixed run, left over from before the two files' iteration counts
were unified; corrected against a real run.