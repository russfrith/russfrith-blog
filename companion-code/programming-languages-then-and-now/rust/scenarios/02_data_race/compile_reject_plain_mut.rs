// Scenario 2, Rust, compiler-rejection (a): a plain mutable counter captured by
// reference in two thread::spawn closures. This is the naive C/Go-style mistake,
// attempted in safe Rust with no unsafe and no Arc/Mutex -- it must fail to
// compile, because the closures could outlive the stack frame the counter lives
// on, and the borrow checker can't prove the two threads won't race even if they
// didn't outlive it.
use std::thread;

fn main() {
    let mut counter = 0i64;
    let mut handles = vec![];
    for _ in 0..2 {
        handles.push(thread::spawn(|| {
            for _ in 0..200_000 {
                counter += 1; // ERROR: closure may outlive the current function
            }
        }));
    }
    for h in handles {
        h.join().unwrap();
    }
    println!("counter = {counter}");
}
