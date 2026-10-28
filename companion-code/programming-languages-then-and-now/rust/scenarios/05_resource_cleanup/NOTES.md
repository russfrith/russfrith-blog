Confirms the prediction exactly: broken.rs prints neither "dropping a" nor "dropping
b" -- the cycle leaks, both nodes' memory is never freed. This is memory-safe (no
use-after-free, no double-free) but not leak-free; Rust's guarantee is deterministic
Drop for values that do get dropped, not leak-freedom for reference cycles.