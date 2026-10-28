// Scenario 2, Rust, compiler-rejection (b): sharing a plain Rc<RefCell<i64>>
// (rather than Arc<Mutex<i64>>) across threads. Rc is explicitly NOT Send -- it's
// not thread-safe by design (its refcount updates aren't atomic) -- so this must
// fail to compile with a trait-bound error, not a runtime race.
use std::cell::RefCell;
use std::rc::Rc;
use std::thread;

fn main() {
    let counter = Rc::new(RefCell::new(0i64));
    let mut handles = vec![];
    for _ in 0..2 {
        let counter = Rc::clone(&counter);
        handles.push(thread::spawn(move || {
            for _ in 0..200_000 {
                *counter.borrow_mut() += 1; // ERROR: Rc<RefCell<i64>> is not Send
            }
        }));
    }
    for h in handles {
        h.join().unwrap();
    }
    println!("counter = {}", counter.borrow());
}
