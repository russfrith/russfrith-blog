// Go's signature footgun: an interface holding a nil *pointer* is NOT a nil
// interface. The interface value has a non-nil type descriptor, so `err != nil`
// is true even though the underlying pointer is nil.
package main

import "fmt"

type MyError struct{ msg string }

func (e *MyError) Error() string { return e.msg }

func mayFail(fail bool) error {
	var err *MyError // nil *MyError
	if fail {
		err = &MyError{"it broke"}
	}
	return err // always returns a non-nil interface, even when err is a nil pointer
}

func main() {
	err := mayFail(false)
	fmt.Printf("err == nil: %v\n", err == nil) // false! surprising if you expected nil
}
