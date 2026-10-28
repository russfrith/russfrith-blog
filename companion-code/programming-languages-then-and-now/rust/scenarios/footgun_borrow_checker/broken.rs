// Rust's signature footgun: logically-correct code the borrow checker still
// rejects. Borrowing two DIFFERENT elements of the same Vec via a helper function
// that takes `&mut Vec<T>` and an index can't be proven disjoint by the compiler,
// even though the two calls below touch different indices and would never alias.
fn get_mut(v: &mut Vec<i32>, i: usize) -> &mut i32 {
    &mut v[i]
}

fn main() {
    let mut v = vec![1, 2, 3];
    let a = get_mut(&mut v, 0);
    let b = get_mut(&mut v, 1); // ERROR: second mutable borrow of `v` while `a` is live
    *a += 1;
    *b += 1;
    println!("{v:?}");
}
