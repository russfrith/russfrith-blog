// wordfreq --top N --concurrency N --timeout SECONDS urls.txt
//
// Fetches URLs concurrently (bounded concurrency), tokenizes on non-ASCII-letter
// boundaries, lowercases, and prints the top N words by count (desc), then
// alphabetically, to stdout. Timing and per-URL failures go to stderr.
//
// Exit codes: 0 all succeeded, 1 partial failure, 2 bad arguments / missing input file.
import kotlinx.coroutines.*
import kotlinx.coroutines.sync.Semaphore
import kotlinx.coroutines.sync.withPermit
import java.io.File
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.time.Duration
import java.util.concurrent.ConcurrentHashMap
import kotlin.system.exitProcess

fun tokenize(text: String): List<String> =
    text.lowercase().split(Regex("[^a-z]+")).filter { it.isNotEmpty() }

fun main(args: Array<String>) {
    var top: Int? = null
    var concurrency: Int? = null
    var timeoutSec: Double? = null
    var urlsFile: String? = null

    var i = 0
    while (i < args.size) {
        when (args[i]) {
            "--top" -> { top = args[++i].toIntOrNull() }
            "--concurrency" -> { concurrency = args[++i].toIntOrNull() }
            "--timeout" -> { timeoutSec = args[++i].toDoubleOrNull() }
            else -> { urlsFile = args[i] }
        }
        i++
    }

    if (top == null || concurrency == null || timeoutSec == null || urlsFile == null) {
        System.err.println("usage: wordfreq --top N --concurrency N --timeout SECONDS urls.txt")
        exitProcess(2)
    }

    val file = File(urlsFile)
    if (!file.exists()) {
        System.err.println("wordfreq: cannot read $urlsFile: No such file or directory")
        exitProcess(2)
    }
    val urls = file.readLines().map { it.trim() }.filter { it.isNotEmpty() }

    val client = HttpClient.newBuilder()
        .connectTimeout(Duration.ofSeconds(timeoutSec.toLong().coerceAtLeast(1)))
        .build()
    val counts = ConcurrentHashMap<String, Int>()
    val hadFailure = java.util.concurrent.atomic.AtomicBoolean(false)
    val semaphore = Semaphore(concurrency)
    val start = System.nanoTime()

    runBlocking {
        val jobs = urls.map { url ->
            async(Dispatchers.IO) {
                semaphore.withPermit {
                    try {
                        val request = HttpRequest.newBuilder(URI.create(url))
                            .timeout(Duration.ofMillis((timeoutSec * 1000).toLong()))
                            .GET()
                            .build()
                        val response = client.send(request, HttpResponse.BodyHandlers.ofString())
                        if (response.statusCode() != 200) {
                            throw RuntimeException("HTTP ${response.statusCode()}")
                        }
                        for (w in tokenize(response.body())) {
                            counts.merge(w, 1, Int::plus)
                        }
                    } catch (e: Exception) {
                        hadFailure.set(true)
                        System.err.println("wordfreq: fetch failed: $url: ${e.message}")
                    }
                }
            }
        }
        jobs.awaitAll()
    }

    val elapsedSec = (System.nanoTime() - start) / 1_000_000_000.0
    System.err.println("wordfreq: fetched ${urls.size} url(s) in ${"%.3f".format(elapsedSec)}s")

    val sorted = counts.entries.sortedWith(compareByDescending<Map.Entry<String, Int>> { it.value }.thenBy { it.key })
    for (entry in sorted.take(top)) {
        println("${entry.key} ${entry.value}")
    }

    exitProcess(if (hadFailure.get()) 1 else 0)
}
