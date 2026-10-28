// Scenario 5 fixed: one direction of the cycle uses Weak instead of Rc, so it
// doesn't contribute to the strong count. When the last strong Rc drops, the
// chain unwinds and Drop runs for both nodes, deterministically.
use std::cell::RefCell;
use std::rc::{Rc, Weak};

struct Node {
    name: String,
    other: RefCell<Option<Weak<Node>>>,
}

impl Drop for Node {
    fn drop(&mut self) {
        println!("dropping {}", self.name);
    }
}

fn main() {
    let a = Rc::new(Node { name: "a".into(), other: RefCell::new(None) });
    let b = Rc::new(Node { name: "b".into(), other: RefCell::new(None) });
    *a.other.borrow_mut() = Some(Rc::downgrade(&b));
    *b.other.borrow_mut() = Some(Rc::downgrade(&a)); // weak reference: no cycle

    println!("a strong_count = {}", Rc::strong_count(&a));
    println!("b strong_count = {}", Rc::strong_count(&b));
    println!("end of main -- both should drop below");
}
