// Scenario 2: data race. Safe Rust's type system (Send/Sync + the borrow checker)
// makes it a COMPILE ERROR to share a plain mutable reference across threads
// without synchronization -- there is no safe way to write the naive C/Go version.
// `unsafe` is the escape hatch that lets you reintroduce the race on purpose.
use std::thread;

static mut COUNTER: i64 = 0;
const ITERATIONS: i64 = 200_000;

fn main() {
    let mut handles = vec![];
    for _ in 0..2 {
        handles.push(thread::spawn(|| {
            for _ in 0..ITERATIONS {
                unsafe {
                    let current = std::ptr::read(&raw const COUNTER);
                    thread::yield_now(); // widen the window so the interleave is reliable, not timing luck
                    std::ptr::write(&raw mut COUNTER, current + 1);
                }
            }
        }));
    }
    for h in handles {
        h.join().unwrap();
    }
    unsafe {
        println!("counter = {COUNTER} (expected {})", 2 * ITERATIONS);
    }
}
