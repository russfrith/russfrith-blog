"""Fixed mock HTTP server for the wordfreq comparison. Stdlib only, no third-party deps.

Endpoints:
  GET /fixed/alpha.html   -- fixed small HTML page, known word content
  GET /fixed/beta.html    -- fixed small HTML page, different known word content
  GET /missing            -- always 404
  GET /hang               -- never responds (sleeps far longer than any test timeout)
  GET /huge                -- large (~5 MB) page of repeated ASCII words
  GET /json/missing-field -- JSON object missing a documented field
  GET /json/wrong-type    -- JSON object with a documented field present but wrong-typed
"""

import json
import socketserver
from http.server import BaseHTTPRequestHandler
import time

ALPHA_HTML = b"<html><body>the quick brown fox jumps over the lazy dog the fox runs</body></html>"
BETA_HTML = b"<html><body>the rain in spain falls mainly on the plain in spain</body></html>"
HUGE_WORD = b"lorem "
HUGE_BODY = HUGE_WORD * (5 * 1024 * 1024 // len(HUGE_WORD))

# Documented JSON shape: {"id": int, "label": str, "count": int}
JSON_MISSING_FIELD = json.dumps({"id": 1, "label": "widget"}).encode("utf-8")
JSON_WRONG_TYPE = json.dumps({"id": 2, "label": "gadget", "count": "not-a-number"}).encode("utf-8")


class Handler(BaseHTTPRequestHandler):
    server_version = "WordfreqMock/1.0"

    def _send(self, status, body, content_type="text/html; charset=utf-8"):
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path == "/fixed/alpha.html":
            self._send(200, ALPHA_HTML)
        elif self.path == "/fixed/beta.html":
            self._send(200, BETA_HTML)
        elif self.path == "/huge":
            self._send(200, HUGE_BODY, "text/plain; charset=utf-8")
        elif self.path == "/hang":
            time.sleep(3600)
        elif self.path == "/json/missing-field":
            self._send(200, JSON_MISSING_FIELD, "application/json")
        elif self.path == "/json/wrong-type":
            self._send(200, JSON_WRONG_TYPE, "application/json")
        else:
            self._send(404, b"not found")

    def log_message(self, fmt, *args):
        pass  # keep container logs quiet; acceptance script doesn't rely on them


class ThreadingHTTPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    allow_reuse_address = True
    daemon_threads = True


if __name__ == "__main__":
    server = ThreadingHTTPServer(("0.0.0.0", 8080), Handler)
    server.serve_forever()
