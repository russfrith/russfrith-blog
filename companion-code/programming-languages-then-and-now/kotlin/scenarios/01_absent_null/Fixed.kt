// Scenario 1 fixed: respect the nullable return type instead of forcing past it.
fun main() {
    val record = mapOf("id" to 1, "label" to 10)
    val count = record["count"]
    if (count == null) {
        println("count absent")
    } else {
        println("count = $count")
    }
}
