"""Scenario 1 fixed: .get() with an explicit default, or check for the sentinel."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

record = {"id": 1, "label": "widget"}
count = record.get("count")
if count is None:
    print("count absent")
else:
    print(f"count = {count}")
