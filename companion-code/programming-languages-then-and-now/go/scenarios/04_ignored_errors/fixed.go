// Scenario 4 fixed: check the error before trusting the result.
package main

import (
	"fmt"
	"os"
)

func main() {
	data, err := os.ReadFile("/no/such/path.txt")
	if err != nil {
		fmt.Printf("read failed: %v\n", err)
		return
	}
	fmt.Printf("read %d bytes\n", len(data))
}
