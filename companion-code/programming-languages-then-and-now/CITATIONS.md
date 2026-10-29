# Citation audit (not published -- internal verification record)

For every link in "Why languages exist," "Eras and pivot points," "What changed after
the 2000s," and the Kotlin/Swift language notes: fetched the page, checked whether it
actually supports the specific claim attributed to it. Format: claim, URL, supporting
passage (<=25 words) or none, verdict.

Three verdict types:
- **PASS** -- fetched, the page supports the claim, passage quoted.
- **FAIL** -- fetched, the page does NOT support the claim as cited (wrong framing,
  wrong document, or the claim isn't there).
- **BLOCKED** -- could not fetch at all (403, archive.org unreachable, PDF too large
  or didn't extract) -- not a finding about the claim's accuracy, just that this
  session's tooling couldn't verify it. Left as-is rather than removed, since "the
  tool couldn't reach it" is not the same as "the claim is wrong."

## Phase 1f re-verification (F6)

Rebuilt the fetch rig using a second, independent method -- a real headless Edge
instance driven over the Chrome DevTools Protocol (navigate, read
`document.body.innerText`), not the WebFetch tool -- specifically to rule out
"this is a WebFetch-specific block." Result: every one of the six BLOCKED items
below is blocked at the HOSTING SERVER'S OWN edge (CloudFront/Akamai access-denied
pages, or a PDF viewer that doesn't expose extractable text), not by WebFetch. A
real browser gets the identical block. This is a genuine network-reachability
limit of this environment, not a tooling choice -- re-running WebFetch again
would not help.

| URL | Result via real headless browser |
|---|---|
| blog.jetbrains.com/.../strategic-partnership-with-spring/ | CloudFront 403 ("Request blocked") |
| blog.jetbrains.com/.../kotlin-on-android-now-official/ | CloudFront 403 ("Request blocked") |
| nokia.com/bell-labs/.../chist.html (Ritchie, C) | Akamai edge "Access Denied" |
| apps.dtic.mil/sti/pdfs/ADA090709.pdf (Ada) | Edge "The request is blocked" |
| nvlpubs.nist.gov .../nbshandbook106.pdf (COBOL) | Loads (into Chrome's native PDF viewer) but exposes no extractable DOM text |
| informatik.uni-bremen.de .../wirth-Turing-Award.pdf (Pascal) | Same as COBOL -- PDF viewer, no extractable text |

Per F6, the post's wording for all six of these claims was reworded (not simply
re-cited) to stop implying this post's own tooling verified the linked page's
text, and the JetBrains Spring-partnership 27% figure was removed outright
(an unverifiable specific statistic, rather than a general claim) -- see the
post's "Eras and pivot points"/"What changed after the 2000s" sections and the
Kotlin language notes.

## Why languages exist

| Claim | URL | Passage | Verdict |
|---|---|---|---|
| Fortran: "FORmula TRANslation," IBM hardware, scientific computation | research.ibm.com/.../the-history-of-fortran-i-ii-and-iii--2 | none -- page is an abstract/landing page only, full text not fetchable | FAIL (weak citation, not a wrong fact -- see note below) |
| COBOL: business data processing, readable by non-programmers | nvlpubs.nist.gov NBS Handbook 106 | not fetched -- file exceeds this tool's size limit | BLOCKED |
| BASIC: accessible to students outside engineering/science at Dartmouth, time-sharing | dartmouth.edu/basicfifty/basic.html | "Those innovations made computing accessible to all Dartmouth students and faculty..." | PARTIAL -- supports accessibility/Dartmouth framing; does not confirm "deliberately simplified versus Fortran" |
| Pascal: teach structured programming, reaction against complexity | informatik.uni-bremen.de Wirth Turing Award PDF | none -- PDF did not extract cleanly | BLOCKED |
| C: write Unix efficiently/portably, replace assembly | nokia.com/bell-labs/.../chist.html | none -- HTTP 403, link itself is correct (bell-labs.com mirror redirects here) | BLOCKED |
| Ada: DoD funding fragmentation, wanted strong typing/concurrency/real-time | apps.dtic.mil ADA090709.pdf | none -- HTTP 403, archive.org fallback also unreachable by this tool | BLOCKED |

Note on the two FAIL/weak items (Fortran, and by extension similar "landing page only"
citations): the underlying facts are extremely well-established computing history
(Backus led the Fortran project, the name is a contraction of "FORmula TRANslation")
-- the issue is specifically that the cited URL's *fetchable* content doesn't contain
the supporting passage, not that the fact is wrong. Recommend either finding a URL to
the full paper text (not an abstract page) or softening "per IBM Research's own
account" to something that doesn't imply the linked page itself states it.

## Eras and pivot points

| Claim | URL | Passage | Verdict |
|---|---|---|---|
| C++/OO era: "C++'s own account" of the complexity-management motivation | stroustrup.com/papers.html | none -- page is a bare list of publication titles/links, no supporting prose itself | FAIL |
| Rails: bundled opinionated conventions into the framework | rubyonrails.org/doctrine | "Rails is an omakase stack"; "Part of the Rails' mission is to swing its machete at the thick...jungle of recurring decisions" | PASS |
| Python/SciPy: ecosystem (not language design) drove Python's dominance | arxiv.org/abs/1907.10121 | Abstract is narrowly about SciPy's own development/adoption, not a claim about Python's overall dominance | FAIL -- paper doesn't make the broader claim it's cited for |

## What changed after the 2000s

| Claim | URL | Passage | Verdict |
|---|---|---|---|
| LLVM: infrastructure for reuse by new language frontends | llvm.org/pubs/2004-01-30-CGO-LLVM.html | Paper's stated purpose is "lifelong program analysis and transformation," not frontend/backend reuse for new languages | FAIL -- 2004 paper predates LLVM's later role as a new-language backend; this is reading today's usage back into a paper that doesn't claim it |
| Go: hand-written backend, not LLVM, per Google's 2009 announcement | developers.googleblog.com/2009/11/go-new-programming-language.html | none -- the announcement is a few sentences, no discussion of compiler backend architecture at all | FAIL -- true fact (Go's backend is in fact hand-written, not LLVM), wrong citation |
| Kotlin: official first-class Android support in 2017 | blog.jetbrains.com/kotlin/2017/05/kotlin-on-android-now-official/ | none -- HTTP 403 | BLOCKED (well-documented public fact independent of this URL: announced at Google I/O, May 2017) |
| Package managers "from day one" (Cargo for Rust, Go modules) | (no citation link in post) | go.dev/blog/using-go-modules: "Go 1.11 and 1.12 include preliminary support for modules..." | **FAIL -- factually wrong.** Go shipped in 2009; Go modules arrived in Go 1.11 (2018), 9 years later. Only Cargo/Rust actually qualifies as "from day one." |

### Dates check (no citations attached, verified against well-established facts)

| Date in post | Verified |
|---|---|
| Go 2009 | correct (announcement post itself is dated Nov. 2009) |
| Rust 2010 announced / 2015 stable 1.0 | correct |
| Swift 2014 | correct (WWDC 2014) |
| Kotlin 2011 | correct (JetBrains announced July 2011) |
| Zig 2016 | correct |
| Mojo 2023 | correct (announced May 2023) |
| Rails 2004 | correct |

All seven dates check out; none required a post change.

## Kotlin language notes

| Claim | URL | Passage | Verdict |
|---|---|---|---|
| State of Kotlin 2026: 61% Spring/Spring Boot, 51% Ktor | kotlinlang.org/state-of-kotlin-2026/ | "61% of Kotlin backend developers use Spring / Spring Boot" / "51% of Kotlin backend developers use Ktor" | PASS |
| JetBrains/Spring partnership: 27% figure, dated 2025-05-22 | blog.jetbrains.com/.../strategic-partnership-with-spring/ | none -- HTTP 403 on every attempt (JetBrains blog blocks this tool) | BLOCKED |
| RedMonk Q1 2026: Kotlin 14th, tied with Scala | redmonk.com/sogrady/2026/04/14/language-rankings-1-26/ | "14 Kotlin / 14 Scala" | PASS |

## Swift language notes

| Claim | URL | Passage | Verdict |
|---|---|---|---|
| Direct quote: "web frameworks ... that provide a variety of tools and libraries to streamline the development process" | swift.org/documentation/server/ | Actual: "...web frameworks such as Vapor and Hummingbird **which** provide a variety of tools and libraries to streamline the development process." | **FAIL -- not an exact quote.** Post says "that provide," page says "which provide." One-word mismatch inside quotation marks. |

## Summary of required post changes

1. **Swift quote**: change "that provide" to "which provide" to match the source exactly (one-word fix, in quotation marks so it must be exact).
2. **Go modules "from day one"**: factually wrong for Go (modules arrived in Go 1.11, 2018, not at Go's 2009 launch). Reword to only claim this for Cargo/Rust, or state the Go modules date explicitly instead of implying "from day one."
3. **Go hand-written backend claim**: citation doesn't support it. Either drop the citation link (the fact itself is true and doesn't strictly need external sourcing to state plainly) or cite something that actually discusses Go's backend architecture.
4. **LLVM paper**: citation doesn't support "infrastructure for reuse by new languages." Reword to not claim the 2004 paper itself says this, or drop the specific "describes it as infrastructure for exactly this kind of reuse" framing.
5. **SciPy paper**: doesn't support the Python-dominance framing. Reword so the paper is cited only for what it actually documents (SciPy's own ecosystem/growth), not as evidence for Python's overall rise.
6. **Stroustrup papers page / Fortran IBM page**: weak citations (landing/abstract pages, not supporting text) -- lower priority than the four FAILs above; consider softening the "own account" framing if not replaced with a better URL.
