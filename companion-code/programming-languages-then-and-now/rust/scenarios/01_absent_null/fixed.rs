// Scenario 1 fixed: .get() returns Option<&V>, forcing the absent case to be handled.
use std::collections::HashMap;

fn main() {
    let mut record: HashMap<&str, i64> = HashMap::new();
    record.insert("id", 1);
    match record.get("count") {
        Some(count) => println!("count = {count}"),
        None => println!("count absent"),
    }
}
