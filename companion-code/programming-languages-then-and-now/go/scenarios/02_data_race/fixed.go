// Scenario 2 fixed: a Mutex serializes access to the shared counter.
package main

import (
	"fmt"
	"sync"
)

const iterations = 200000

func main() {
	counter := 0
	var mu sync.Mutex
	var wg sync.WaitGroup
	for g := 0; g < 2; g++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for i := 0; i < iterations; i++ {
				mu.Lock()
				counter++
				mu.Unlock()
			}
		}()
	}
	wg.Wait()
	fmt.Printf("counter = %d (expected %d)\n", counter, 2*iterations)
}
