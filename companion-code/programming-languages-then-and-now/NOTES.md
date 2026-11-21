# Editorial notes (cross-session handoff)

Working notes for `content/posts/programming-languages-then-and-now/index.md` and its companion code, for continuing this post's work from a different machine/session. This is a scratch/discussion file, not a pre-publish checklist. Prune items here once they're resolved rather than letting this grow into a transcript. Don't duplicate `PREDICTIONS.md` (predictions recorded before each run vs. measured outcomes), `REVIEW.md` (claims excluded from the post pending a source), `CITATIONS.md` (citation audit for the historical sections), or `VERSIONS.md` (pinned toolchain versions and lookup dates) — read those directly instead of summarizing them here.

## What this post is

A long (~7,270 words), still-`draft: true` comparison of Python/Go/Rust/TypeScript/Kotlin/C# (plus a C baseline, plus Swift covered in prose only) through a shared `wordfreq` CLI and numbered safety/concurrency/productivity scenarios. Backed by real companion code that actually runs in pinned Docker containers — nothing in the post's numbers is hand-typed or benchmarked-then-guessed; `scripts/check-post.ps1` exists specifically to enforce that (see below).

## The verification machinery — read before touching anything here

Three scripts exist specifically to stop numbers from drifting or being invented:

- **`scripts/run-scenarios.ps1`**: reproduces every scenario's output against the pinned containers. Default (no flags) is check mode. `-Update` regenerates committed `output.txt` files from a real run; `-Update -Only <name[,name...]>` regenerates just the named scenario(s) and merges only their entry into `results/provenance.json` by file key, leaving everything else alone.
- **`scripts/measure-productivity.ps1`**: the only thing allowed to write `results/productivity.json`. `results/productivity.md` is *generated* from that JSON by this same script, never hand-edited. Measures real Docker build times (clean build / no-op rebuild / rebuild-after-a-one-line-change, the last reported net of measured container-startup overhead), a Kotlin Gradle-daemon-kept-warm variant, dependency counts (including actual compiled-crate counts for Rust, not just resolved-lockfile counts), and image sizes.
- **`scripts/check-post.ps1`**: the gate. Checks snippet-path casing, that the site builds, that transcript blocks match their source files verbatim, that every embedded output file matches its provenance entry, that productivity numbers in the post match `productivity.json`, and (via `scripts/claims.psd1`) that specific prose claims and rankings match their real source — including a noise-floor check so a ranking can't rest on a gap smaller than 2x the measured container overhead.
- Run `check-post.ps1` and `run-scenarios.ps1` (check mode, no flags) after any change in this folder; both should come back green with a clean `git status` before committing.
- **Never hand-edit** an `output.txt`, `results/provenance.json`, or `results/productivity.json`/`.md` — always regenerate through the script that owns it.

## Open: proposed MIT LICENSE for this companion code, never added

A pre-publish scan found `LICENSE-CONTENT` at the repo root covers only the blog post Markdown (CC BY-NC 4.0, under `content/posts/`) — it says nothing about `companion-code/`. The post invites readers to clone and run this code, but as published there's no explicit grant to do so. A draft was proposed earlier but never added to the repo, and the scratch file it lived in was in a since-expired session temp directory — reproduced here in full so it isn't lost again.

**Proposed `companion-code/programming-languages-then-and-now/LICENSE`:**

```
MIT License

Copyright (c) 2026 Russell Frith

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to
deal in the Software without restriction, including without limitation the
rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Plus a proposed one-line addition to this folder's `README.md`:

```markdown
## License

This companion code is MIT licensed -- see [`LICENSE`](LICENSE). (The blog
post text itself is CC BY-NC 4.0; see the repo root's `LICENSE-CONTENT`.)
```

Scoped to just this post's folder, not all of `companion-code/`, since other posts' folders deserve their own license decision. Still awaiting a yes/no from the user before either file is actually added.

## Open: non-reader-facing files already published

43 files under this companion code (`REVIEW.md`, `CITATIONS.md`, 41 per-scenario `NOTES.md` files) are internal working notes, not polished reader-facing content — and they're already on `origin/master`, so gitignoring them now would only stop *future* ones from being committed; it wouldn't retroactively unpublish what's already there. A history rewrite would do that but is a much bigger, separate decision, not taken. Proposal for new posts going forward: an untracked (gitignored) `notes/` subfolder per post's companion-code directory, so working notes never get committed in the first place. Not yet applied anywhere, including here.

## Open: 3 claims in `REVIEW.md` still need a primary source

See that file directly — the LLVM-as-the-reason-for-new-systems-languages framing, a Python free-threaded performance claim, and the Stack Overflow 2025 Kotlin usage percentage are all excluded from the post pending verification. Not re-summarized here to avoid the two copies drifting apart.

## Open: 31 `AUTHOR-TODO` placeholders, all needing the user's own content, not Claude's

Grouped by kind rather than itemized (search the post for `AUTHOR-TODO` for exact locations):

- One opening anecdote (the user's own start in the 1980s, and a professor's claim).
- About 7 "verify in a normal browser that this page supports: ..." checks — these flag that a cited source should be re-read in a normal browser (not assistant-fetched), not that the claim itself is in doubt.
- 7 pairs of "what was familiar" / "what surprised me," one pair per per-language retrospective section.
- A disclosure that C# is the user's daily language and the C# notes reflect that bias.
- A closing "if you come from ___, consider ___" scaffold, unfilled.
- A couple of "candidate observation" notes flagging ideas the user may want to promote into prose later.

None of these are Claude's to fill in — they're explicitly the user's own voice/experience per the post's own convention; don't draft placeholder text for them.

## Not done, intentionally: Zig and Mojo

Both get prose-only "(not implemented in this post)" sections — no companion code, no scenarios, no benchmarks for either. Every round of this post's hardening work has carried the same standing instruction ("No Zig or Mojo"). Don't start on either without being explicitly asked.

## Resolved, for context (no action needed)

- Many rounds of measurement-honesty hardening: every productivity number in the post is now derived from `results/productivity.json` via a Hugo shortcode (`layouts/shortcodes/productivity-table.html`), never hand-typed; rebuild timings are corrected for Docker container-startup overhead (measured separately per image); a Kotlin Gradle-daemon-kept-warm variant was added and is correctly reported raw (it's timed in-container via nanosecond markers, so container overhead was never part of that number — an earlier version of this wrongly subtracted overhead from it anyway, since fixed); any ranking resting on a gap smaller than 2x the measured overhead says "within noise" instead of naming a winner. See `git log --oneline -- companion-code/programming-languages-then-and-now content/posts/programming-languages-then-and-now` for the commit-by-commit detail rather than a replay of it here.
- `scripts/claims.psd1` + `scripts/check-post.ps1` catch prose/data drift automatically now, including claims that check text in a file other than the rendered post itself (via a `PostFile` field) and ranking claims with a noise floor.
- Cover image added, and a two-sentence addition on client-side scripting (JavaScript, with a brief VBScript mention) added to the "Eras and pivot points" section — both committed 2026-11-19.
