// Scenario 7, Rust, fixed: spawn_blocking moves the CPU-bound work onto tokio's
// dedicated blocking-task thread pool, leaving the async worker threads (and the
// heartbeat task) free.
use std::time::Instant;
use tokio::time::{sleep, Duration};

const ITERATIONS: u64 = 300_000_000;

fn cpu_bound(iterations: u64) -> f64 {
    let mut total = 0.0;
    for i in 0..iterations {
        total += (i as f64).sqrt();
    }
    total
}

#[tokio::main]
async fn main() {
    let ticks = std::sync::Arc::new(std::sync::atomic::AtomicU64::new(0));
    let ticks_clone = ticks.clone();
    tokio::spawn(async move {
        loop {
            sleep(Duration::from_millis(100)).await;
            let n = ticks_clone.fetch_add(1, std::sync::atomic::Ordering::SeqCst) + 1;
            println!("heartbeat tick {n}");
        }
    });

    println!("starting CPU-bound work via spawn_blocking...");
    let start = Instant::now();
    let result = tokio::task::spawn_blocking(move || cpu_bound(ITERATIONS))
        .await
        .unwrap();
    let elapsed = start.elapsed();
    println!(
        "CPU-bound work finished after {:.3?}, heartbeat ticks seen: {}, result: {}",
        elapsed,
        ticks.load(std::sync::atomic::Ordering::SeqCst),
        result
    );
}
