Confirms the prediction exactly: System.Text.Json's default behavior for a plain
(non-required) property is to silently zero-fill a MISSING field -- the same
failure mode as Go's encoding/json, not serde/kotlinx.serialization's default
strictness -- while still throwing on a WRONG-TYPED field (a hard type mismatch).
The `required` modifier (C# 11+) closes the gap: with it, BOTH the missing-field
and wrong-type payloads throw a JsonException.