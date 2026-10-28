// Scenario 3 fixed: Math.addExact throws ArithmeticException on overflow instead
// of wrapping silently.
fun main() {
    val x = Int.MAX_VALUE
    try {
        val y = Math.addExact(x, 1)
        println("Int.MAX_VALUE + 1 = $y")
    } catch (e: ArithmeticException) {
        println("overflow detected: ${e.message}")
    }
}
