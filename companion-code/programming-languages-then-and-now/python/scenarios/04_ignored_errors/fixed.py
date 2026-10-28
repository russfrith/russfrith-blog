"""Scenario 4 fixed: catch the specific exception and handle it explicitly."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

try:
    with open("/no/such/path.txt") as f:
        data = f.read()
except FileNotFoundError as e:
    print(f"file not found: {e}")
else:
    print(f"read {len(data)} bytes")
