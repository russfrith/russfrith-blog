// Scenario 1: absent value. Indexing a HashMap directly panics on a missing key --
// loud, but a full process abort for what might be routine missing data.
use std::collections::HashMap;

fn main() {
    let mut record: HashMap<&str, i64> = HashMap::new();
    record.insert("id", 1);
    // "count" is absent
    let count = record["count"]; // panics: key not found
    println!("count = {count}");
}
