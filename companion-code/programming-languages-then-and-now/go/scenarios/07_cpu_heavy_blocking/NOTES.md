Confirms the prediction: the heartbeat goroutine keeps ticking on schedule (every
~100ms) WHILE the CPU-bound loop runs on the main goroutine, with no explicit
offloading needed.

A3 investigation note -- an order-of-magnitude discrepancy found and corrected:
the original (Phase 1) version of this file called `cpuBound(2_000_000_000)` and
discarded the return value, printing only the elapsed time. That version reported
332ms for 2,000,000,000 iterations. Re-running the SAME 2,000,000,000-iteration
workload with the result printed (so the loop can't be proven dead and removed)
instead took 2.742s -- about 8x slower than the unprinted version, and in line
with Node's 2.6s/2.7s for the same iteration count. Verbatim re-run:

$ sed 's/300_000_000/2_000_000_000/' demo.go > demo_2e9.go && go run demo_2e9.go
starting CPU-bound work on the main goroutine, heartbeat goroutine running concurrently...
heartbeat tick 1 at 2026-10-28T05:21:11.486640512Z
...
heartbeat tick 27 at 2026-10-28T05:21:14.086673199Z
CPU-bound work finished after 2.74203249s, heartbeat ticks seen: 27, result: 59628479377633.976562

Conclusion, verified against the actual compiler output (C6) rather than assumed:
the original Go result was invalid. Inspecting `go build -gcflags=-S` on a
reconstruction of the original unused-result call confirms the mechanism exactly:
the compiler inlined `cpuBound` into `main` and then proved the accumulated
`total` (the sum of square roots) was never read after the function returned, so
it deleted the floating-point work (the `SQRTSD`/`ADDSD` instructions) entirely,
leaving only the empty counting loop:

    main.main STEXT ... size=16
        XORL  AX, AX
    loop:
        INCQ  AX
        CMPQ  AX, $2000000000
        JLT   loop
        RET

That compiled binary ran the resulting empty 2-billion-iteration loop in 344ms
(`time ./unused_result_check`), consistent with the original 332ms measurement.
This is genuine Go compiler optimization (inlining + dead-store elimination)
doing exactly what it's supposed to do with a value nothing reads -- not a
measurement bug, and not unique to this one run. It was NOT a genuine ~8x
Go-vs-Node speed advantage for the sqrt workload itself; once both print the
result (so the compiler can't prove it's unused), Go (404ms) and Node (411ms) are
nearly identical for the same 300,000,000-iteration workload. Kotlin coroutines on
Dispatchers.Default and Rust async tasks using spawn_blocking behave the same
qualitative way (heartbeat keeps ticking without explicit offloading) as this Go
counterexample; neither was run as a full separate timed demo here, per the rule
of showing full detail only where something notable happens -- the "notable"
thing for Rust/Kotlin in this scenario is the scheduling behavior, not a timing
comparison.