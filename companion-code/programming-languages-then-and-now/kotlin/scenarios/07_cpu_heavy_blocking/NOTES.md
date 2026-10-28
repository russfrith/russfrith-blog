Confirms half the prediction, revises the other half -- same pattern as Rust's
tokio result (see rust/scenarios/07_cpu_heavy_blocking/output.txt): the
single-threaded dispatcher stalls completely (0 ticks), matching Node/Python's
broken case and Rust's current_thread runtime exactly. Running the SAME unfixed
code directly on Dispatchers.Default (no withContext fix applied, just a
different dispatcher) did NOT stall (4 ticks) -- this machine's default thread
pool size gave the heartbeat coroutine a free thread to run on even though the
CPU-bound coroutine occupied one. This does not mean Dispatchers.Default makes the
mistake harmless in general: Kotlin's own coroutines guide recommends
Dispatchers.Default specifically for CPU-bound work and a separate IO dispatcher
for blocking I/O, precisely because an undersized or saturated pool would show the
same stall this single demo didn't surface.