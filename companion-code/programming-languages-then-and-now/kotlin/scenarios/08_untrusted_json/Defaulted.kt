// Scenario 8, Kotlin, counterpoint: a default value on `count` makes the
// missing-field case silently zero-fill, mirroring Go -- a deliberate opt-in. The
// wrong-type case should still fail: a default only applies when the field is
// ABSENT, not when it's present with the wrong type.
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse

@Serializable
data class Item(val id: Long, val label: String, val count: Long = 0)

val client = HttpClient.newHttpClient()

fun fetchAndDecode(url: String) {
    val body = client.send(HttpRequest.newBuilder(URI.create(url)).GET().build(), HttpResponse.BodyHandlers.ofString()).body()
    try {
        val item = Json.decodeFromString<Item>(body)
        println("$url: $item (no error, no way to tell count=0 from a real zero)")
    } catch (e: Exception) {
        println("$url: decode error: ${e.message}")
    }
}

fun main() {
    fetchAndDecode("http://mockserver:8080/json/missing-field")
    fetchAndDecode("http://mockserver:8080/json/wrong-type")
}
