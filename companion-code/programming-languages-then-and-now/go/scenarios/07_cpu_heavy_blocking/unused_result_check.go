package main

import "math"

func cpuBound(iterations int) float64 {
	total := 0.0
	for i := 0; i < iterations; i++ {
		total += math.Sqrt(float64(i))
	}
	return total
}

func main() {
	cpuBound(2_000_000_000) // original mistake: return value never used
}
