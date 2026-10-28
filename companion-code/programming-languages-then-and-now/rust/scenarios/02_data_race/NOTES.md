Mismatch with prediction / with Go: there is no `-race`-equivalent built into rustc,
and safe Rust makes the naive version (plain shared mutable state across threads, no
synchronization) a COMPILE ERROR, not a runtime race -- getting an actual race at all
requires opting into `unsafe`. A first attempt with plain back-to-back increments
(static mut, 200,000 iterations, no induced delay) did not reproduce a lost update,
same finding as Python's scenario 2 -- see PREDICTIONS.md. Reducing to 200
iterations and inserting a `thread::yield_now()` between the raw read and raw write
made it reliable, same technique as the Python version.