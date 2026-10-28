// Scenario 2: data race. Two goroutines increment a shared counter with no
// synchronization. `go run -race` catches this at runtime via the race detector.
package main

import (
	"fmt"
	"sync"
)

const iterations = 200000

func main() {
	counter := 0
	var wg sync.WaitGroup
	for g := 0; g < 2; g++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for i := 0; i < iterations; i++ {
				counter++
			}
		}()
	}
	wg.Wait()
	fmt.Printf("counter = %d (expected %d)\n", counter, 2*iterations)
}
