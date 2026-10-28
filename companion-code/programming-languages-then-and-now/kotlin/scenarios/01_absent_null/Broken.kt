// Scenario 1: absent value. Absence IS visible in Kotlin's types -- Map.get()
// returns V?, not V, so the compiler already knows "count" might not be there.
// The not-null assertion operator `!!` is an explicit, deliberate opt-in to crash
// if that type is wrong at runtime -- it's a choice the caller makes, not a gap
// in the type system.
fun main() {
    val record = mapOf("id" to 1, "label" to 10)
    val count = record["count"]!! // "count" is absent; !! turns the null into a crash
    println("count = $count")
}
