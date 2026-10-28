// Scenario 8: parsing untrusted JSON. encoding/json zero-fills a missing field
// silently (same as scenario 1), and for a wrong-typed field it returns a decode
// ERROR rather than silently coercing -- which is the more interesting case here:
// Go is strict about type mismatches but silent about absence, a combination
// that's easy to assume is "strict" across the board when it's actually split.
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
	Count int    `json:"count"`
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
	err = json.Unmarshal(body, &item)
	fmt.Printf("%s: item=%+v err=%v\n", url, item, err)
}

func main() {
	fetchAndDecode("http://mockserver:8080/json/missing-field")
	fetchAndDecode("http://mockserver:8080/json/wrong-type")
}
