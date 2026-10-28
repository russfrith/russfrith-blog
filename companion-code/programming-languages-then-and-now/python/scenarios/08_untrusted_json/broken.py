"""Scenario 8: parsing untrusted JSON. Direct dict indexing after json.loads
raises KeyError on the missing field -- loud, but only for the field you happened
to touch; the wrong-typed field passes through completely untouched until used."""
import sys
import urllib.request

print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

for url in [
    "http://mockserver:8080/json/missing-field",
    "http://mockserver:8080/json/wrong-type",
]:
    import json

    with urllib.request.urlopen(url) as resp:
        item = json.loads(resp.read())
    count = item["count"]  # KeyError if absent; silently the wrong type otherwise
    print(f"{url}: count = {count!r} (type {type(count).__name__})")
