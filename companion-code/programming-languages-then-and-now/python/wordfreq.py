#!/usr/bin/env python3
"""wordfreq --top N --concurrency N --timeout SECONDS urls.txt

Fetches URLs concurrently (bounded concurrency), tokenizes on non-ASCII-letter
boundaries, lowercases, and prints the top N words by count (desc), then
alphabetically, to stdout. Timing and per-URL failures go to stderr.

Exit codes: 0 all succeeded, 1 partial failure, 2 bad arguments / missing input file.
"""
import argparse
import re
import sys
import time
import urllib.error
import urllib.request
from collections import Counter
from concurrent.futures import ThreadPoolExecutor, as_completed

WORD_RE = re.compile(r"[^a-zA-Z]+")


def fetch(url: str, timeout: float) -> bytes:
    with urllib.request.urlopen(url, timeout=timeout) as resp:
        return resp.read()


def tokenize(text: str) -> list[str]:
    return [w for w in WORD_RE.split(text.lower()) if w]


def main() -> int:
    parser = argparse.ArgumentParser(prog="wordfreq")
    parser.add_argument("--top", type=int, required=True)
    parser.add_argument("--concurrency", type=int, required=True)
    parser.add_argument("--timeout", type=float, required=True)
    parser.add_argument("urls_file")
    args = parser.parse_args()  # argparse itself exits(2) on bad arguments

    try:
        with open(args.urls_file, "r", encoding="utf-8") as f:
            urls = [line.strip() for line in f if line.strip()]
    except OSError as e:
        print(f"wordfreq: cannot read {args.urls_file}: {e}", file=sys.stderr)
        return 2

    counts: Counter[str] = Counter()
    had_failure = False
    start = time.monotonic()

    with ThreadPoolExecutor(max_workers=args.concurrency) as pool:
        futures = {pool.submit(fetch, url, args.timeout): url for url in urls}
        for future in as_completed(futures):
            url = futures[future]
            try:
                body = future.result()
            except (urllib.error.URLError, OSError, TimeoutError) as e:
                had_failure = True
                print(f"wordfreq: fetch failed: {url}: {e}", file=sys.stderr)
                continue
            text = body.decode("utf-8", errors="ignore")
            counts.update(tokenize(text))

    elapsed = time.monotonic() - start
    print(f"wordfreq: fetched {len(urls)} url(s) in {elapsed:.3f}s", file=sys.stderr)

    for word, count in sorted(counts.items(), key=lambda kv: (-kv[1], kv[0]))[: args.top]:
        print(f"{word} {count}")

    return 1 if had_failure else 0


if __name__ == "__main__":
    sys.exit(main())
