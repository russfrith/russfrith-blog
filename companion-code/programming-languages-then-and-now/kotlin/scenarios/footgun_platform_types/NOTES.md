Note: the Kotlin-generated NPE message ("getProperty(...) must not be null") is the
compiler's own inserted null-check talking -- it's more informative than a bare
Java NPE would be, but the type error itself was never caught at compile time.

Deviation from an earlier draft of this scenario: java.util.HashMap.get() and
java.util.Map.get() are NOT plain platform types in this Kotlin version -- the
Kotlin compiler has special "enhanced" nullability built in for well-known JDK
collection APIs, so `HashMap<String,String>().get("missing")` type-checks as
nullable (String?) automatically, not as an assumed-non-null platform type.
java.util.Properties.getProperty(), an older, unannotated API, does exhibit the
true platform-type behavior and is what's captured above.