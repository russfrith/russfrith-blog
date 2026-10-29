Confirms the prediction: the heartbeat goroutine keeps ticking on schedule (every
~100ms) WHILE the CPU-bound loop runs on the main goroutine, with no explicit
offloading needed.

Investigation note -- an order-of-magnitude discrepancy found and corrected:
the original version of this file called `cpuBound(2_000_000_000)` and
discarded the return value, printing only the elapsed time. That version's
originally-reported ~332ms turned out to be measuring an empty loop, not the
sqrt workload.

Real compiler and timing evidence for this (H3, Phase 1h): see
`dead_code_evidence_output.txt` in this same folder, captured by
`scripts/run-scenarios.ps1 -Update` from a real `go build -a -gcflags=-S` run
against `unused_result_evidence.go` (the reconstructed original mistake, also
in this folder) -- not hand-pasted. It shows the compiler inlining the call and
reducing the loop body to a bare counter (`XORL`/`INCQ`/`CMPQ`/`JLT`, no
`SQRTSD`/`ADDSD`), and the real elapsed time for running that empty loop.

This is genuine Go compiler optimization (inlining + dead-store elimination)
doing exactly what it's supposed to do with a value nothing reads -- not a
measurement bug, and not unique to this one run. It was NOT a genuine ~8x
Go-vs-Node speed advantage for the sqrt workload itself; once both print the
result (so the compiler can't prove it's unused), Go and Node land within a few
percent of each other for the same 300,000,000-iteration workload (see
output.txt in this folder for the current numbers). Kotlin coroutines on
Dispatchers.Default and Rust async tasks using spawn_blocking behave the same
qualitative way (heartbeat keeps ticking without explicit offloading) as this Go
counterexample; neither was run as a full separate timed demo here, per the rule
of showing full detail only where something notable happens -- the "notable"
thing for Rust/Kotlin in this scenario is the scheduling behavior, not a timing
comparison.