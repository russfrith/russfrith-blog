// Scenario 8, Rust, strict: serde_json with a plain (non-Option) `count: i64`
// field. serde requires every field to be present and correctly typed by
// default -- both the missing-field and wrong-type payloads should fail to
// deserialize.
use serde::Deserialize;

#[derive(Deserialize, Debug)]
struct Item {
    id: i64,
    label: String,
    count: i64,
}

fn fetch_and_decode(url: &str) {
    let body = ureq::get(url).call().unwrap().into_string().unwrap();
    match serde_json::from_str::<Item>(&body) {
        Ok(item) => println!("{url}: {item:?}"),
        Err(e) => println!("{url}: decode error (correctly caught): {e}"),
    }
}

fn main() {
    fetch_and_decode("http://mockserver:8080/json/missing-field");
    fetch_and_decode("http://mockserver:8080/json/wrong-type");
}
