// Scenario 8, Rust, counterpoint: #[serde(default)] makes the missing-field case
// silently zero-fill, mirroring Go's default behavior -- a deliberate opt-in, not
// a strictness failure. The wrong-type case should still fail: `default` only
// fills in an ABSENT field, it doesn't relax the type of a field that IS present.
use serde::Deserialize;

#[derive(Deserialize, Debug)]
struct Item {
    #[allow(dead_code)]
    id: i64,
    #[allow(dead_code)]
    label: String,
    #[serde(default)]
    count: i64,
}

fn fetch_and_decode(url: &str) {
    let body = ureq::get(url).call().unwrap().into_string().unwrap();
    match serde_json::from_str::<Item>(&body) {
        Ok(item) => println!("{url}: {item:?} (no error, no way to tell count=0 from a real zero)"),
        Err(e) => println!("{url}: decode error: {e}"),
    }
}

fn main() {
    fetch_and_decode("http://mockserver:8080/json/missing-field");
    fetch_and_decode("http://mockserver:8080/json/wrong-type");
}
