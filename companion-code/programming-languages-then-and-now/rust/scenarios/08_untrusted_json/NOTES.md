Confirms the prediction exactly: with a plain (non-Option, no #[serde(default)])
field, serde_json errors on BOTH the missing-field and wrong-type payloads --
strict by default, unlike Go. Adding #[serde(default)] makes ONLY the
missing-field case silently zero-fill (Go-like); the wrong-type case still
correctly errors, because `default` only fills an absent field, it doesn't relax
the type check on a field that is present but wrong-typed.