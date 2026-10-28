// Scenario 8 fixed: decode into a struct with a pointer field so "absent" is
// distinguishable from "present and zero" (same technique as scenario 1); the
// wrong-type case already errors, so the fix there is just: check the error.
package main

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
)

type Item struct {
	ID    int    `json:"id"`
	Label string `json:"label"`
	Count *int   `json:"count"`
}

func fetchAndDecode(url string) {
	resp, err := http.Get(url)
	if err != nil {
		fmt.Printf("%s: fetch error: %v\n", url, err)
		return
	}
	defer resp.Body.Close()
	body, _ := io.ReadAll(resp.Body)

	var item Item
	if err := json.Unmarshal(body, &item); err != nil {
		fmt.Printf("%s: decode error (correctly caught): %v\n", url, err)
		return
	}
	if item.Count == nil {
		fmt.Printf("%s: count absent (correctly distinguished from zero)\n", url)
	} else {
		fmt.Printf("%s: count = %d\n", url, *item.Count)
	}
}

func main() {
	fetchAndDecode("http://mockserver:8080/json/missing-field")
	fetchAndDecode("http://mockserver:8080/json/wrong-type")
}
