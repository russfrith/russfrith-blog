// Scenario 5 fixed: defer f.Close() runs deterministically when main() returns.
package main

import (
	"fmt"
	"os"
)

func main() {
	f, err := os.Open("/etc/hostname")
	if err != nil {
		panic(err)
	}
	defer f.Close()
	data := make([]byte, 64)
	n, _ := f.Read(data)
	fmt.Printf("read %d bytes, fd closed deterministically via defer\n", n)
}
