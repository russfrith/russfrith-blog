// Fixed: declare the platform type as nullable and provide a default.
import java.util.Properties

fun main() {
    val props = Properties()
    val value: String? = props.getProperty("missing.key")
    println("value = ${value ?: "(default)"}")
}
