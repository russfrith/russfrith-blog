No free-threaded build was run, see VERSIONS.md.
Confirms the prediction: 0 heartbeat ticks while the CPU-bound loop runs directly
on the event loop task with no await points; 128 ticks once the same work moves to
run_in_executor's thread pool. Tick interval drifts slightly above the nominal
100ms (e.g. tick 1 at 842.977, tick 2 at 843.082 -- about 105ms apart) -- the
executor thread still contends for the GIL with the event loop thread in this
build, so even the "fixed" version isn't perfectly on schedule.
Python took 14.3s/13.6s for the same 300,000,000-iteration workload Go and Node
finished in ~0.4s -- about 33-35x slower, consistent with an interpreted loop
versus a JIT-compiled (V8) or natively compiled (Go) one. This is a real, expected
difference in this micro-workload, not a general benchmark claim about Python.