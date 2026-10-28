// Scenario 4: ignored errors. Go makes you write `, err` everywhere but nothing
// forces you to check it -- this compiles and runs cleanly, using a zero-value
// result as if it were real data.
package main

import (
	"fmt"
	"os"
)

func main() {
	data, _ := os.ReadFile("/no/such/path.txt") // error discarded with `_`
	fmt.Printf("read %d bytes\n", len(data))
}
