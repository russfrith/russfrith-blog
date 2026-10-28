// Scenario 4 fixed: handle the specific exception and report it.
import java.io.File
import java.io.FileNotFoundException

fun main() {
    try {
        val data = File("/no/such/path.txt").readText()
        println("read ${data.length} chars")
    } catch (e: FileNotFoundException) {
        println("file not found: ${e.message}")
    }
}
