Note: unlike Go, there is no built-in race detector in the standard `kotlinc`/`java`
toolchain -- the wrong total is visible, but nothing flags it as a race for you.
This raced reliably on the first run with plain back-to-back `counter++` (no
artificial delay needed), unlike the Python/Rust tight-loop cases.