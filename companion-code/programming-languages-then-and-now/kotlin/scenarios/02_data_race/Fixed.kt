// Scenario 2 fixed: java.util.concurrent.atomic.AtomicInteger makes the
// increment a single atomic operation.
import java.util.concurrent.atomic.AtomicInteger

fun main() {
    val counter = AtomicInteger(0)
    val iterations = 200_000
    val threads = (1..2).map {
        Thread {
            for (i in 0 until iterations) {
                counter.incrementAndGet()
            }
        }
    }
    threads.forEach { it.start() }
    threads.forEach { it.join() }
    println("counter = ${counter.get()} (expected ${2 * iterations})")
}
