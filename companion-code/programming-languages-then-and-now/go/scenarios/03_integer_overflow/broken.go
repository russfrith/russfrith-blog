// Scenario 3: integer overflow. Go wraps silently on overflow (two's complement) --
// no panic, no error, in both debug and release builds.
package main

import (
	"fmt"
	"math"
)

func main() {
	x := math.MaxInt32
	y := int32(x) + 1
	fmt.Printf("int32(math.MaxInt32) + 1 = %d\n", y)
}
