// Scenario 5 fixed: `use` (Kotlin's try-with-resources equivalent) closes the
// stream deterministically, even on an exception.
import java.io.FileInputStream

fun main() {
    FileInputStream("/etc/hostname").use { stream ->
        val bytes = stream.readBytes()
        println("read ${bytes.size} bytes, stream closed deterministically via use{}")
    }
}
