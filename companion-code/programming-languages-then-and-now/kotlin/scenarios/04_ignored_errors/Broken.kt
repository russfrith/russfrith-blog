// Scenario 4: ignored errors. A bare catch-and-swallow hides the exception
// entirely, same risk as Python's bare `except: pass`.
import java.io.File

fun main() {
    try {
        val data = File("/no/such/path.txt").readText()
        println("read ${data.length} chars")
    } catch (e: Exception) {
        // swallowed
    }
    println("done")
}
