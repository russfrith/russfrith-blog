// Scenario 5: resource cleanup. No `use` block, no explicit close -- relies on the
// JVM's GC-driven finalization to eventually release the file descriptor.
import java.io.FileInputStream

fun main() {
    val stream = FileInputStream("/etc/hostname")
    val bytes = stream.readBytes()
    println("read ${bytes.size} bytes, stream never explicitly closed")
}
