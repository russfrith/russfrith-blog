// Scenario 3: integer overflow. In a debug build, Rust panics on overflow. In a
// release build (overflow-checks off by default), it silently wraps like Go/C.
fn main() {
    // std::hint::black_box hides the value from the compiler's constant folder, so
    // this demonstrates the runtime behavior instead of a compile-time lint error
    // (a plain `let x: i32 = i32::MAX; x + 1` is now rejected at compile time by
    // rustc's deny-by-default `arithmetic_overflow` lint -- see output.txt).
    let x: i32 = std::hint::black_box(i32::MAX);
    let y = x + 1; // debug: panics; release: wraps to i32::MIN
    println!("i32::MAX + 1 = {y}");
}
