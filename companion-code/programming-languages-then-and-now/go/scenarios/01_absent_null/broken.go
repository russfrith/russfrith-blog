// Scenario 1: absent value. encoding/json silently zero-fills a missing field --
// Count ends up 0, indistinguishable from a real zero that was actually present.
package main

import (
	"encoding/json"
	"fmt"
)

type Record struct {
	ID    int    `json:"id"`
	Label string `json:"label"`
	Count int    `json:"count"`
}

func main() {
	data := []byte(`{"id": 1, "label": "widget"}`) // "count" is absent
	var r Record
	if err := json.Unmarshal(data, &r); err != nil {
		panic(err)
	}
	fmt.Printf("count = %d (no error, no way to tell this from a real zero)\n", r.Count)
}
