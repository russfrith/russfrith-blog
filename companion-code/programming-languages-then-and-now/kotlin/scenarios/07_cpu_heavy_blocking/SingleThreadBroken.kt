// Scenario 7, Kotlin, variant (a): CPU-bound work runs directly inside a coroutine
// on a SINGLE-THREADED dispatcher (newSingleThreadContext). Only one OS thread
// services every coroutine on this dispatcher, so the heartbeat coroutine can't
// run until the CPU-bound call returns.
import kotlinx.coroutines.*

const val ITERATIONS = 300_000_000L // same workload across every language in this scenario

fun cpuBound(iterations: Long): Double {
    var total = 0.0
    for (i in 0 until iterations) {
        total += Math.sqrt(i.toDouble())
    }
    return total
}

fun main() = runBlocking {
    val dispatcher = newSingleThreadContext("single")
    withContext(dispatcher) {
        var ticks = 0
        val heartbeat = launch {
            while (true) {
                delay(100)
                ticks++
                println("heartbeat tick $ticks")
            }
        }
        println("starting CPU-bound work directly on a single-threaded dispatcher...")
        val start = System.nanoTime()
        val result = cpuBound(ITERATIONS)
        val elapsedMs = (System.nanoTime() - start) / 1_000_000
        println("CPU-bound work finished after ${elapsedMs}ms, heartbeat ticks seen: $ticks, result: $result")
        heartbeat.cancel()
    }
    dispatcher.close()
}
