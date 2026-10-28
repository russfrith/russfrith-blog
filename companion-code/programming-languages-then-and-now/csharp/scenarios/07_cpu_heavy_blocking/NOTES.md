Workload: 300,000,000 sqrt additions, same as every other language in this
scenario; result printed.

Confirms the prediction exactly: .NET has no single-threaded event loop. The
heartbeat (a separate `Task.Run` with its own thread-pool worker thread) keeps
ticking on schedule WHILE the CPU-bound loop runs synchronously on the thread
that called it directly -- because that thread is a different one than the
heartbeat's. This pattern-matches Go/Rust's multi-thread-runtime/Kotlin's
Dispatchers.Default result (heartbeat unaffected), not Node/Python's
single-threaded stall (0 ticks). There is effectively no behavioral difference
between calling `CpuBound()` directly and wrapping it in `Task.Run` here, because
neither one shares a thread with the heartbeat task in this program's shape --
`Task.Run` only matters when the caller needs ITS OWN thread free for other work,
which this minimal demo doesn't exercise.

Build-configuration finding: the original run used `dotnet run` with no `-c`
flag, which defaults to Debug -- ~1000ms for the workload, versus Go/Node's
~400ms. That gap was NOT a genuine C#-vs-Go/Node difference: it was comparing an
unoptimized Debug build of C# against release-mode Go and Node. Re-running with
`dotnet run -c Release` dropped the time to ~402-407ms, matching Go (404ms) and
Node (411ms) closely. The heartbeat-tick behavior (4 ticks either way) was
identical in both configurations -- only the raw timing changed.
