// Scenario 3 fixed: check before adding, or use math/bits helpers / a wider type.
package main

import (
	"fmt"
	"math"
)

func main() {
	x := int32(math.MaxInt32)
	if x > math.MaxInt32-1 {
		fmt.Println("overflow detected: would exceed int32 range")
		return
	}
	fmt.Printf("x + 1 = %d\n", x+1)
}
