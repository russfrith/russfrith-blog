# Claims that need a source before they go in the post as fact

Anything here is excluded from `index.md` as stated fact. Move an item into the post (with its citation) once verified, or drop it.

- Whether LLVM specifically is the dominant reason new systems languages (Rust, Zig, Swift, Mojo) became viable post-2000s, versus other contributing factors, is a contested, multi-causal historical claim. The post states this as "a commonly cited factor" with Go as an explicit counterexample (hand-written backend, not LLVM), not as settled fact.
- Any specific percentage/multiplier performance claim about Python's free-threaded build beyond what PEP 779's own text supports should be re-checked against the PEP text at write time, not quoted from secondary blog summaries.
- Ruby on Rails as "the" pivot for web scripting is a simplification; the post should cite DHH/Rails' own documentation for Rails' stated design goals rather than asserting a historical primacy claim that could be disputed by PHP/Perl/CGI advocates.
- Stack Overflow Developer Survey 2025's Kotlin usage percentage: only found via secondary mentions ("Kotlin appeared on the most-admired list", "4th among languages developers want to learn next, 6%"), never confirmed by reading the primary survey page/results table directly. Dropped from the post rather than cited from a secondary source. If this is wanted, it needs a direct read of the Stack Overflow 2025 results page before publish.

## Resolved in Phase 1b (A11) -- now cited in the post with primary sources, listed here for traceability

- JetBrains' own "State of Kotlin 2026" report (https://kotlinlang.org/state-of-kotlin-2026/ and https://blog.jetbrains.com/kotlin/2026/09/state-of-kotlin-2026-report/): 61% of Kotlin backend developers use Spring/Spring Boot, 51% use Ktor. Labeled vendor-reported (JetBrains surveying its own user base) in the post, not independent.
- The JetBrains/Spring strategic partnership (https://blog.jetbrains.com/kotlin/2025/05/strategic-partnership-with-spring/, announced 2025-05-22 -- note: an earlier draft of this prompt said "February 2025," which does not match the primary source's own dateline): 27% of Spring developers have used Kotlin, per that same JetBrains post. Also vendor-reported.
- RedMonk's Q1 2026 language rankings (https://redmonk.com/sogrady/2026/04/14/language-rankings-1-26/): Kotlin ranked 14th, tied with Scala. This one is independent (RedMonk's own methodology, not a vendor survey) and is cited as such.
