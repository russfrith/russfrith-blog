// wordfreq --top N --concurrency N --timeout SECONDS urls.txt
//
// Fetches URLs concurrently (bounded concurrency), tokenizes on non-ASCII-letter
// boundaries, lowercases, and prints the top N words by count (desc), then
// alphabetically, to stdout. Timing and per-URL failures go to stderr.
//
// Exit codes: 0 all succeeded, 1 partial failure, 2 bad arguments / missing input file.
package main

import (
	"bufio"
	"flag"
	"fmt"
	"io"
	"net/http"
	"os"
	"sort"
	"strings"
	"sync"
	"time"
)

func isASCIILetter(r rune) bool {
	return (r >= 'a' && r <= 'z') || (r >= 'A' && r <= 'Z')
}

func tokenize(s string) []string {
	s = strings.ToLower(s)
	var words []string
	var cur strings.Builder
	for _, r := range s {
		if isASCIILetter(r) {
			cur.WriteRune(r)
		} else if cur.Len() > 0 {
			words = append(words, cur.String())
			cur.Reset()
		}
	}
	if cur.Len() > 0 {
		words = append(words, cur.String())
	}
	return words
}

func main() {
	top := flag.Int("top", 0, "number of top words to print")
	concurrency := flag.Int("concurrency", 1, "max concurrent fetches")
	timeoutSec := flag.Float64("timeout", 5, "per-request timeout in seconds")
	flag.Parse()

	if flag.NArg() != 1 {
		fmt.Fprintln(os.Stderr, "wordfreq: expected exactly one urls file argument")
		os.Exit(2)
	}
	urlsFile := flag.Arg(0)

	f, err := os.Open(urlsFile)
	if err != nil {
		fmt.Fprintf(os.Stderr, "wordfreq: cannot read %s: %v\n", urlsFile, err)
		os.Exit(2)
	}
	var urls []string
	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line != "" {
			urls = append(urls, line)
		}
	}
	f.Close()

	client := &http.Client{Timeout: time.Duration(*timeoutSec * float64(time.Second))}
	sem := make(chan struct{}, *concurrency)
	var mu sync.Mutex
	counts := make(map[string]int)
	hadFailure := false
	var wg sync.WaitGroup

	start := time.Now()
	for _, url := range urls {
		wg.Add(1)
		sem <- struct{}{}
		go func(url string) {
			defer wg.Done()
			defer func() { <-sem }()

			resp, err := client.Get(url)
			if err != nil {
				mu.Lock()
				hadFailure = true
				mu.Unlock()
				fmt.Fprintf(os.Stderr, "wordfreq: fetch failed: %s: %v\n", url, err)
				return
			}
			defer resp.Body.Close()
			if resp.StatusCode != 200 {
				mu.Lock()
				hadFailure = true
				mu.Unlock()
				fmt.Fprintf(os.Stderr, "wordfreq: fetch failed: %s: HTTP %d\n", url, resp.StatusCode)
				return
			}
			body, err := io.ReadAll(resp.Body)
			if err != nil {
				mu.Lock()
				hadFailure = true
				mu.Unlock()
				fmt.Fprintf(os.Stderr, "wordfreq: fetch failed: %s: %v\n", url, err)
				return
			}
			words := tokenize(string(body))
			mu.Lock()
			for _, w := range words {
				counts[w]++
			}
			mu.Unlock()
		}(url)
	}
	wg.Wait()
	elapsed := time.Since(start)
	fmt.Fprintf(os.Stderr, "wordfreq: fetched %d url(s) in %s\n", len(urls), elapsed)

	type kv struct {
		word  string
		count int
	}
	var sorted []kv
	for w, c := range counts {
		sorted = append(sorted, kv{w, c})
	}
	sort.Slice(sorted, func(i, j int) bool {
		if sorted[i].count != sorted[j].count {
			return sorted[i].count > sorted[j].count
		}
		return sorted[i].word < sorted[j].word
	})
	if *top < len(sorted) {
		sorted = sorted[:*top]
	}
	for _, kv := range sorted {
		fmt.Printf("%s %d\n", kv.word, kv.count)
	}

	if hadFailure {
		os.Exit(1)
	}
	os.Exit(0)
}
