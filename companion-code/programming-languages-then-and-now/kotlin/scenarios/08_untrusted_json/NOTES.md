Confirms the prediction exactly, the same pattern as Rust's serde result: a plain
non-default `count: Long` field errors on BOTH the missing-field and wrong-type
payloads -- strict by default, unlike Go. Giving `count` a default value (`= 0`)
makes ONLY the missing-field case silently zero-fill; the wrong-type case still
correctly errors, because a default only applies when the field is absent, not
when it's present with the wrong type.