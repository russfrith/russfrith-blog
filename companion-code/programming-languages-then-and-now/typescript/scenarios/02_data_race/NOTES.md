Note: ordinary JS/TS objects can't race (single-threaded); this uses worker_threads
+ SharedArrayBuffer, the one place real shared-memory concurrency exists in Node.
Unlike the Python/Rust tight-loop cases, this raced reliably on the first run with
no artificial delay needed -- the two workers run genuinely concurrently on separate
OS threads, not cooperatively scheduled on one.
