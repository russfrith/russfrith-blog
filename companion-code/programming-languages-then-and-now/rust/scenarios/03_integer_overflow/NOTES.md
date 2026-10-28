Deviation from the original broken.rs: a plain `let x: i32 = i32::MAX; x + 1` is
rejected at COMPILE time on this rustc, not just at runtime -- the deny-by-default
`arithmetic_overflow` lint const-evaluates the known value and refuses to compile:

error: this arithmetic operation will overflow
 --> broken.rs:5:13
  |
5 |     let y = x + 1;
  |             ^^^^^ attempt to compute `i32::MAX + 1_i32`, which would overflow
  = note: `#[deny(arithmetic_overflow)]` on by default

std::hint::black_box(i32::MAX) hides the value from that compile-time check so the
snippet demonstrates the intended runtime behavior (panic in debug, silent wrap in
release) instead.