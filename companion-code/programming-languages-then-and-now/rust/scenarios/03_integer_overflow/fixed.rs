// Scenario 3 fixed: checked_add makes the overflow an explicit, handled case in
// every build profile, not something that depends on debug vs. release.
fn main() {
    let x: i32 = i32::MAX;
    match x.checked_add(1) {
        Some(y) => println!("i32::MAX + 1 = {y}"),
        None => println!("overflow detected: would exceed i32 range"),
    }
}
