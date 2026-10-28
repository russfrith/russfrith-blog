// Fixed: split_at_mut proves disjointness to the compiler by construction, so both
// mutable borrows can coexist safely.
fn main() {
    let mut v = vec![1, 2, 3];
    let (left, right) = v.split_at_mut(1);
    let a = &mut left[0];
    let b = &mut right[0];
    *a += 1;
    *b += 1;
    println!("{v:?}");
}
