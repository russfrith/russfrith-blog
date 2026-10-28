// Scenario 7, Rust, variant (a): a CPU-bound call placed directly inside an async
// task, run on a SINGLE-THREADED (current_thread) tokio runtime. Only one OS
// thread services every task, so a blocking call on that thread stalls everything
// else, including the heartbeat -- same qualitative failure as Node/Python's
// broken case.
use std::time::Instant;
use tokio::time::{sleep, Duration};

const ITERATIONS: u64 = 300_000_000; // same workload across every language in this scenario

fn cpu_bound(iterations: u64) -> f64 {
    let mut total = 0.0;
    for i in 0..iterations {
        total += (i as f64).sqrt();
    }
    total
}

#[tokio::main(flavor = "current_thread")]
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

    println!("starting CPU-bound work directly inside an async task (current_thread runtime)...");
    let start = Instant::now();
    let result = cpu_bound(ITERATIONS);
    let elapsed = start.elapsed();
    println!(
        "CPU-bound work finished after {:.3?}, heartbeat ticks seen: {}, result: {}",
        elapsed,
        ticks.load(std::sync::atomic::Ordering::SeqCst),
        result
    );
}
