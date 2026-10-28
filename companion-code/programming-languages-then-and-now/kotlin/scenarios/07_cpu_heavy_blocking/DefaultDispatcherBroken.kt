// Scenario 7, Kotlin, variant (b): the SAME CPU-bound-inside-a-coroutine code,
// but run on the default coroutine context (Dispatchers.Default's thread pool,
// sized to available cores) instead of a single-threaded dispatcher.
import kotlinx.coroutines.*

const val ITERATIONS = 300_000_000L

fun cpuBound(iterations: Long): Double {
    var total = 0.0
    for (i in 0 until iterations) {
        total += Math.sqrt(i.toDouble())
    }
    return total
}

fun main() = runBlocking(Dispatchers.Default) {
    var ticks = 0
    val heartbeat = launch {
        while (true) {
            delay(100)
            ticks++
            println("heartbeat tick $ticks")
        }
    }
    println("starting CPU-bound work directly on Dispatchers.Default...")
    val start = System.nanoTime()
    val result = cpuBound(ITERATIONS)
    val elapsedMs = (System.nanoTime() - start) / 1_000_000
    println("CPU-bound work finished after ${elapsedMs}ms, heartbeat ticks seen: $ticks, result: $result")
    heartbeat.cancel()
}
