// Scenario 5: resource cleanup. No `defer f.Close()` -- the file descriptor is
// leaked until the GC finalizer eventually runs (if ever, and not deterministically).
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
	data := make([]byte, 64)
	n, _ := f.Read(data)
	fmt.Printf("read %d bytes, fd never explicitly closed\n", n)
}
