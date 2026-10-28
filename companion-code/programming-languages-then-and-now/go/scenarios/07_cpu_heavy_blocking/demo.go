// Scenario 7 counterexample: Go's goroutines are scheduled across real OS threads
// (GOMAXPROCS of them by default), so a CPU-bound goroutine does not block a
// concurrent heartbeat the way it blocks Node's or Python asyncio's single thread.
// No broken/fixed pair -- this is the baseline behavior, shown once.
package main

import (
	"fmt"
	"math"
	"sync"
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
	var wg sync.WaitGroup
	ticks := 0
	done := make(chan struct{})

	wg.Add(1)
	go func() {
		defer wg.Done()
		ticker := time.NewTicker(100 * time.Millisecond)
		defer ticker.Stop()
		for {
			select {
			case <-ticker.C:
				ticks++
				fmt.Printf("heartbeat tick %d at %s\n", ticks, time.Now().Format(time.RFC3339Nano))
			case <-done:
				return
			}
		}
	}()

	const iterations = 300_000_000 // same workload across every language in this scenario
	start := time.Now()
	fmt.Println("starting CPU-bound work on the main goroutine, heartbeat goroutine running concurrently...")
	result := cpuBound(iterations)
	elapsed := time.Since(start)
	close(done)
	wg.Wait()
	fmt.Printf("CPU-bound work finished after %s, heartbeat ticks seen: %d, result: %f\n", elapsed, ticks, result)
}
