// Scenario 2 fixed: back in safe Rust with an Arc<Mutex<_>>, which the compiler
// requires you to lock before touching the data -- there is no way to "forget" to.
use std::sync::{Arc, Mutex};
use std::thread;

const ITERATIONS: i64 = 200_000;

fn main() {
    let counter = Arc::new(Mutex::new(0i64));
    let mut handles = vec![];
    for _ in 0..2 {
        let counter = Arc::clone(&counter);
        handles.push(thread::spawn(move || {
            for _ in 0..ITERATIONS {
                let mut c = counter.lock().unwrap();
                *c += 1;
            }
        }));
    }
    for h in handles {
        h.join().unwrap();
    }
    println!("counter = {} (expected {})", *counter.lock().unwrap(), 2 * ITERATIONS);
}
