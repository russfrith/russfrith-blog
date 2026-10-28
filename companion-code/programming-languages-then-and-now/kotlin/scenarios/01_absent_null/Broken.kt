// Scenario 1: absent value. Kotlin's Map.get() is already nullable (Map<K, V> ->
// V?), so the absent case is visible in the type -- but the not-null assertion
// operator `!!` lets you force past that and crash anyway, which is exactly what
// happens below.
fun main() {
    val record = mapOf("id" to 1, "label" to 10)
    val count = record["count"]!! // "count" is absent; !! turns the null into a crash
    println("count = $count")
}
