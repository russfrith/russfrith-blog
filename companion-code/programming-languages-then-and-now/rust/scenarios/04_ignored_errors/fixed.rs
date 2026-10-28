// Scenario 4 fixed: match (or ? inside a function that returns Result) handles it.
use std::fs;

fn main() {
    match fs::read("/no/such/path.txt") {
        Ok(data) => println!("read {} bytes", data.len()),
        Err(e) => println!("read failed: {e}"),
    }
}
