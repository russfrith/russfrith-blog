"""Scenario 8 fixed: validate presence AND type explicitly before trusting the
value -- Python's own typing does nothing for you at runtime, same as TypeScript."""
import json
import sys
import urllib.request

print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)


def parse_item(raw: bytes) -> dict | None:
    item = json.loads(raw)
    count = item.get("count")
    if not isinstance(count, int):
        return None
    return item


for url in [
    "http://mockserver:8080/json/missing-field",
    "http://mockserver:8080/json/wrong-type",
]:
    with urllib.request.urlopen(url) as resp:
        raw = resp.read()
    item = parse_item(raw)
    if item is None:
        print(f"{url}: validation failed, rejecting payload")
    else:
        print(f"{url}: count = {item['count']}")
