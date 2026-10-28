// Scenario 1 fixed: use a pointer (or map[string]any) so "absent" is distinguishable
// (nil) from "present and zero".
package main

import (
	"encoding/json"
	"fmt"
)

type Record struct {
	ID    int    `json:"id"`
	Label string `json:"label"`
	Count *int   `json:"count"`
}

func main() {
	data := []byte(`{"id": 1, "label": "widget"}`)
	var r Record
	if err := json.Unmarshal(data, &r); err != nil {
		panic(err)
	}
	if r.Count == nil {
		fmt.Println("count absent")
	} else {
		fmt.Printf("count = %d\n", *r.Count)
	}
}
