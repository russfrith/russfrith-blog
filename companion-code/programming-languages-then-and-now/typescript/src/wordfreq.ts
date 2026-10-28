// wordfreq --top N --concurrency N --timeout SECONDS urls.txt
//
// Fetches URLs concurrently (bounded concurrency), tokenizes on non-ASCII-letter
// boundaries, lowercases, and prints the top N words by count (desc), then
// alphabetically, to stdout. Timing and per-URL failures go to stderr.
//
// Exit codes: 0 all succeeded, 1 partial failure, 2 bad arguments / missing input file.
import { readFileSync } from "node:fs";

interface Options {
  top: number;
  concurrency: number;
  timeoutMs: number;
  urlsFile: string;
}

function parseArgs(argv: string[]): Options {
  let top: number | undefined;
  let concurrency: number | undefined;
  let timeoutSec: number | undefined;
  let urlsFile: string | undefined;

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === "--top") top = Number(argv[++i]);
    else if (arg === "--concurrency") concurrency = Number(argv[++i]);
    else if (arg === "--timeout") timeoutSec = Number(argv[++i]);
    else if (!arg.startsWith("--")) urlsFile = arg;
  }

  if (
    top === undefined ||
    concurrency === undefined ||
    timeoutSec === undefined ||
    urlsFile === undefined ||
    Number.isNaN(top) ||
    Number.isNaN(concurrency) ||
    Number.isNaN(timeoutSec)
  ) {
    console.error(
      "usage: wordfreq --top N --concurrency N --timeout SECONDS urls.txt"
    );
    process.exit(2);
  }

  return { top, concurrency, timeoutMs: timeoutSec * 1000, urlsFile };
}

function tokenize(text: string): string[] {
  return text
    .toLowerCase()
    .split(/[^a-z]+/)
    .filter((w) => w.length > 0);
}

async function fetchWithTimeout(url: string, timeoutMs: number): Promise<string> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const resp = await fetch(url, { signal: controller.signal });
    if (!resp.ok) {
      throw new Error(`HTTP ${resp.status}`);
    }
    return await resp.text();
  } finally {
    clearTimeout(timer);
  }
}

async function runPool<T>(
  items: T[],
  concurrency: number,
  worker: (item: T) => Promise<void>
): Promise<void> {
  let index = 0;
  async function next(): Promise<void> {
    while (index < items.length) {
      const item = items[index++];
      await worker(item);
    }
  }
  const workers = Array.from({ length: Math.max(1, concurrency) }, () => next());
  await Promise.all(workers);
}

async function main(): Promise<void> {
  const opts = parseArgs(process.argv.slice(2));

  let urls: string[];
  try {
    urls = readFileSync(opts.urlsFile, "utf-8")
      .split("\n")
      .map((l) => l.trim())
      .filter((l) => l.length > 0);
  } catch (e) {
    console.error(`wordfreq: cannot read ${opts.urlsFile}: ${(e as Error).message}`);
    process.exit(2);
  }

  const counts = new Map<string, number>();
  let hadFailure = false;
  const start = Date.now();

  await runPool(urls, opts.concurrency, async (url) => {
    try {
      const body = await fetchWithTimeout(url, opts.timeoutMs);
      for (const w of tokenize(body)) {
        counts.set(w, (counts.get(w) ?? 0) + 1);
      }
    } catch (e) {
      hadFailure = true;
      console.error(`wordfreq: fetch failed: ${url}: ${(e as Error).message}`);
    }
  });

  const elapsed = (Date.now() - start) / 1000;
  console.error(`wordfreq: fetched ${urls.length} url(s) in ${elapsed.toFixed(3)}s`);

  const sorted = [...counts.entries()].sort((a, b) =>
    b[1] !== a[1] ? b[1] - a[1] : a[0].localeCompare(b[0])
  );
  for (const [word, count] of sorted.slice(0, opts.top)) {
    console.log(`${word} ${count}`);
  }

  process.exit(hadFailure ? 1 : 0);
}

main();
