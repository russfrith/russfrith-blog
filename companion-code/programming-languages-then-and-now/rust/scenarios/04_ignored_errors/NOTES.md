Note: unlike Go, the compiler actively warns (though does not block compilation) when
a #[must_use] Result is discarded -- a warning C and (silently) Go do not give you.