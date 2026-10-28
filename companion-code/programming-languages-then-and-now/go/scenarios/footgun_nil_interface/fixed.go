// Fixed: return the untyped nil interface literal directly when there is no error,
// instead of a nil-valued concrete pointer type.
package main

import "fmt"

type MyError struct{ msg string }

func (e *MyError) Error() string { return e.msg }

func mayFail(fail bool) error {
	if fail {
		return &MyError{"it broke"}
	}
	return nil // untyped nil interface
}

func main() {
	err := mayFail(false)
	fmt.Printf("err == nil: %v\n", err == nil) // true, as expected
}
