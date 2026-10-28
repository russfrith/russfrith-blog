// Scenario 8, Kotlin, strict: kotlinx.serialization with a plain (non-nullable,
// no default) `count: Long` field. Missing and wrong-typed fields should both
// fail to decode.
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse

@Serializable
data class Item(val id: Long, val label: String, val count: Long)

val client = HttpClient.newHttpClient()

fun fetchAndDecode(url: String) {
    val body = client.send(HttpRequest.newBuilder(URI.create(url)).GET().build(), HttpResponse.BodyHandlers.ofString()).body()
    try {
        val item = Json.decodeFromString<Item>(body)
        println("$url: $item")
    } catch (e: Exception) {
        println("$url: decode error (correctly caught): ${e.message}")
    }
}

fun main() {
    fetchAndDecode("http://mockserver:8080/json/missing-field")
    fetchAndDecode("http://mockserver:8080/json/wrong-type")
}
