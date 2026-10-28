// Scenario 2: data race. Two JVM threads increment a shared Int with no
// synchronization. The JVM has no built-in race detector (unlike `go run -race`);
// the wrong total is visible, but you have to know to look for it.
fun main() {
    var counter = 0
    val iterations = 200_000
    val threads = (1..2).map {
        Thread {
            for (i in 0 until iterations) {
                counter++
            }
        }
    }
    threads.forEach { it.start() }
    threads.forEach { it.join() }
    println("counter = $counter (expected ${2 * iterations})")
}
