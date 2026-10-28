Confirmed with the real compiler: `tsc` reports zero type errors on broken.ts -- the `as User` assertion is syntactically valid and
type-checks cleanly. The failure only shows up at runtime, and only once the
lied-about field is actually used.
