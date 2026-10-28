// Scenario 7, Kotlin, fixed: runBlocking itself runs on the calling thread (a
// single thread, like variant (a)), but withContext(Dispatchers.Default) moves
// JUST the CPU-bound call onto the multi-threaded default dispatcher, leaving the
// calling thread free to keep running the heartbeat coroutine.
import kotlinx.coroutines.*

const val ITERATIONS = 300_000_000L

fun cpuBound(iterations: Long): Double {
    var total = 0.0
    for (i in 0 until iterations) {
        total += Math.sqrt(i.toDouble())
    }
    return total
}

fun main() = runBlocking {
    var ticks = 0
    val heartbeat = launch {
        while (true) {
            delay(100)
            ticks++
            println("heartbeat tick $ticks")
        }
    }
    println("starting CPU-bound work via withContext(Dispatchers.Default)...")
    val start = System.nanoTime()
    val result = withContext(Dispatchers.Default) { cpuBound(ITERATIONS) }
    val elapsedMs = (System.nanoTime() - start) / 1_000_000
    println("CPU-bound work finished after ${elapsedMs}ms, heartbeat ticks seen: $ticks, result: $result")
    heartbeat.cancel()
}
