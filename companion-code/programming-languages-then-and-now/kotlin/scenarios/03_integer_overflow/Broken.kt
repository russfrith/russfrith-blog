// Scenario 3: integer overflow. Kotlin's Int wraps on overflow like Java's int --
// silent, defined two's-complement wraparound, no exception.
fun main() {
    val x = Int.MAX_VALUE
    val y = x + 1
    println("Int.MAX_VALUE + 1 = $y")
}
