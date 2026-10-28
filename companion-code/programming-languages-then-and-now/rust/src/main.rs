//! wordfreq --top N --concurrency N --timeout SECONDS urls.txt
//!
//! Fetches URLs concurrently (bounded concurrency), tokenizes on non-ASCII-letter
//! boundaries, lowercases, and prints the top N words by count (desc), then
//! alphabetically, to stdout. Timing and per-URL failures go to stderr.
//!
//! Exit codes: 0 all succeeded, 1 partial failure, 2 bad arguments / missing input file.
use clap::Parser;
use std::collections::HashMap;
use std::fs;
use std::process::ExitCode;
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::{Duration, Instant};

#[derive(Parser)]
#[command(name = "wordfreq")]
struct Args {
    #[arg(long)]
    top: usize,
    #[arg(long)]
    concurrency: usize,
    #[arg(long)]
    timeout: f64,
    urls_file: String,
}

fn tokenize(text: &str) -> Vec<String> {
    text.to_lowercase()
        .split(|c: char| !c.is_ascii_alphabetic())
        .filter(|w| !w.is_empty())
        .map(|w| w.to_string())
        .collect()
}

fn main() -> ExitCode {
    let args = Args::parse(); // clap exits(2) on bad arguments by default

    let contents = match fs::read_to_string(&args.urls_file) {
        Ok(c) => c,
        Err(e) => {
            eprintln!("wordfreq: cannot read {}: {}", args.urls_file, e);
            return ExitCode::from(2);
        }
    };
    let urls: Vec<String> = contents
        .lines()
        .map(|l| l.trim().to_string())
        .filter(|l| !l.is_empty())
        .collect();

    let timeout = Duration::from_secs_f64(args.timeout);
    let counts: Arc<Mutex<HashMap<String, u64>>> = Arc::new(Mutex::new(HashMap::new()));
    let had_failure = Arc::new(Mutex::new(false));
    let start = Instant::now();

    // Bounded concurrency via chunked thread batches of at most `concurrency` workers.
    for chunk in urls.chunks(args.concurrency.max(1)) {
        let mut handles = Vec::new();
        for url in chunk {
            let url = url.clone();
            let counts = Arc::clone(&counts);
            let had_failure = Arc::clone(&had_failure);
            handles.push(thread::spawn(move || {
                let agent = ureq::AgentBuilder::new().timeout(timeout).build();
                match agent.get(&url).call() {
                    Ok(resp) => match resp.into_string() {
                        Ok(body) => {
                            let words = tokenize(&body);
                            let mut counts = counts.lock().unwrap();
                            for w in words {
                                *counts.entry(w).or_insert(0) += 1;
                            }
                        }
                        Err(e) => {
                            *had_failure.lock().unwrap() = true;
                            eprintln!("wordfreq: fetch failed: {}: {}", url, e);
                        }
                    },
                    Err(e) => {
                        *had_failure.lock().unwrap() = true;
                        eprintln!("wordfreq: fetch failed: {}: {}", url, e);
                    }
                }
            }));
        }
        for h in handles {
            h.join().unwrap();
        }
    }

    eprintln!(
        "wordfreq: fetched {} url(s) in {:.3?}",
        urls.len(),
        start.elapsed()
    );

    let counts = counts.lock().unwrap();
    let mut sorted: Vec<(&String, &u64)> = counts.iter().collect();
    sorted.sort_by(|a, b| b.1.cmp(a.1).then_with(|| a.0.cmp(b.0)));
    for (word, count) in sorted.into_iter().take(args.top) {
        println!("{} {}", word, count);
    }

    if *had_failure.lock().unwrap() {
        ExitCode::from(1)
    } else {
        ExitCode::SUCCESS
    }
}
