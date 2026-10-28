Confirms half the prediction, revises the other half: the current_thread runtime
stalls completely (0 ticks), matching Node/Python's broken case exactly. The
multi-thread runtime did NOT stall in this run (4 ticks, same count as the fixed
spawn_blocking version) -- this machine's default worker-thread count (one per
core) was enough to schedule the heartbeat task on a different worker thread than
the one running the blocking CPU-bound call. This is NOT evidence that blocking a
worker thread on tokio's multi-thread runtime is safe in general: tokio's own
documentation warns that CPU-bound or blocking work should use spawn_blocking
precisely because it can starve other tasks once enough worker threads are
simultaneously occupied by blocking calls -- a single blocking call with spare
worker threads available simply doesn't demonstrate that failure mode. Mismatch
with the original prediction ("may still stall... UNLESS worker threads get
occupied") recorded here rather than papered over.