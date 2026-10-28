"""Scenario 1: absent value. Direct dict indexing raises KeyError when a field is
missing -- at least it's a loud, specific exception, not a silent wrong value."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

record = {"id": 1, "label": "widget"}  # "count" is absent
print(record["count"])
