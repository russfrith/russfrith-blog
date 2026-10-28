// Scenario 7, Rust, variant (b): the SAME blocking-call-inside-an-async-task
// mistake, but on tokio's default MULTI-THREAD runtime. tokio's worker threads
// can still schedule the heartbeat task on a different worker thread in
// principle, but a long blocking call parked on a worker thread without
// `spawn_blocking` is exactly the pitfall tokio's own docs warn about: it can
// still starve other tasks if the runtime happens to schedule them onto that
// same occupied worker, and it defeats tokio's work-stealing scheduler's
// assumption that tasks yield promptly.
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

    println!("starting CPU-bound work directly inside an async task (default multi-thread runtime)...");
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
