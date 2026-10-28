// Kotlin's signature footgun: a platform type from Java interop (notated T! in
// diagnostics) can be assigned to a non-null Kotlin type with NO warning or error
// from the compiler, because Kotlin can't see (or trust) Java's nullability
// annotations -- or lack of them -- on this API. java.util.Properties predates
// Java's nullability annotations entirely.
import java.util.Properties

fun main() {
    val props = Properties()
    // Properties.getProperty returns a platform type -- Kotlin assumes non-null here,
    // and this line compiles with ZERO warnings.
    val value: String = props.getProperty("missing.key")
    println("length: ${value.length}") // NPE at runtime, far from the real mistake
}
