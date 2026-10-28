Workload equalized across every language in this scenario to 300,000,000 sqrt
additions; the result is printed so V8 can't eliminate the loop as dead code.

Confirms the prediction exactly: 0 heartbeat ticks while the CPU-bound loop runs on
the main thread (the event loop cannot run anything else), 4 ticks (consistent
with ~400ms / 100ms) once the same work moves to a worker_thread.

Revision note: the original run used 2,000,000,000 iterations and
took 2626ms/2701ms, versus Go's reported 332ms for the SAME iteration count --
nearly an 8x gap that turned out NOT to be a true Node-vs-Go gap. See
go/scenarios/07_cpu_heavy_blocking/output.txt: Go's original result was invalid
because the loop's result was never used, so the compiler eliminated it as dead
code. Once both print the result and run the same 300,000,000-iteration workload,
Node (411ms) and Go (404ms) are nearly identical.
