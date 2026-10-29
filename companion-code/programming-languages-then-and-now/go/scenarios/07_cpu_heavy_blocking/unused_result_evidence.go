// Historical reconstruction, not the current demo: the ORIGINAL version of this
// scenario discarded cpuBound's return value and printed only the elapsed time.
// That let the Go compiler inline the call, prove the result was never read, and
// delete the floating-point work entirely -- it measured an empty loop, not the
// sqrt workload. See NOTES.md and dead_code_evidence_output.txt (captured by
// scripts/run-scenarios.ps1 -Update, not hand-pasted) for the real compiler
// assembly and timing evidence this file produces.
package main

import (
	"fmt"
	"math"
	"time"
)

func cpuBound(iterations int) float64 {
	total := 0.0
	for i := 0; i < iterations; i++ {
		total += math.Sqrt(float64(i))
	}
	return total
}

func main() {
	const iterations = 2_000_000_000
	start := time.Now()
	cpuBound(iterations) // result discarded -- this is the bug being demonstrated
	elapsed := time.Since(start)
	fmt.Printf("elapsed: %s\n", elapsed)
}
