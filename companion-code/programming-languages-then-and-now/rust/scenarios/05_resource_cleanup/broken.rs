// Scenario 5: resource cleanup and leaks. An Rc reference cycle (parent <-> child,
// each holding a strong Rc to the other) means neither's refcount ever reaches
// zero, so neither's Drop ever runs. This leaks memory -- but it is NOT a memory
// safety violation. Rust still guarantees no use-after-free and deterministic Drop
// for everything that *does* drop; leak-freedom from reference cycles is simply not
// part of that guarantee (see the Rust book, ch. 15.6, "Rc<T> ... Can Leak Memory").
use std::cell::RefCell;
use std::rc::Rc;

struct Node {
    #[allow(dead_code)]
    name: String,
    other: RefCell<Option<Rc<Node>>>,
}

impl Drop for Node {
    fn drop(&mut self) {
        println!("dropping {}", self.name);
    }
}

fn main() {
    let a = Rc::new(Node { name: "a".into(), other: RefCell::new(None) });
    let b = Rc::new(Node { name: "b".into(), other: RefCell::new(None) });
    *a.other.borrow_mut() = Some(Rc::clone(&b));
    *b.other.borrow_mut() = Some(Rc::clone(&a)); // cycle: a -> b -> a

    println!("a strong_count = {}", Rc::strong_count(&a));
    println!("b strong_count = {}", Rc::strong_count(&b));
    println!("end of main -- watch for (missing) \"dropping a\"/\"dropping b\" below");
}
