Confirms the prediction exactly: a fresh project template's nullable reference
types annotation (`<Nullable>enable</Nullable>`, which `dotnet new console`
enables by default) makes violating a non-null return type a WARNING (CS8603),
not a build error -- the project still compiles and runs, and the null only
surfaces as a crash at the actual point of use. Dictionary's indexer throws
KeyNotFoundException on a missing key; TryGetValue returns false with no
exception, same two-styles-of-absence pattern as Python's `d[k]` vs `d.get(k)`.