// Scenario 4: ignored errors. fs::read returns a #[must_use] Result, so the
// compiler emits an "unused `Result` that must be used" warning here -- but a
// warning is not an error, so this still compiles and runs, discarding the error.
use std::fs;

fn main() {
    fs::read("/no/such/path.txt"); // Result discarded; warning only, not an error
    println!("done");
}
