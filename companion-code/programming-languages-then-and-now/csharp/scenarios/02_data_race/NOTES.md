Confirms the prediction: the race reproduced reliably on the FIRST run (200,000
iterations, no artificial delay needed) -- unlike Python/Rust's tight-loop cases,
closer to Kotlin/TypeScript's result. The exact wrong total (365913 this run) is
nondeterministic. Interlocked.Increment fixes it with no lock needed, since the
operation is a single atomic increment, not a broader critical section.