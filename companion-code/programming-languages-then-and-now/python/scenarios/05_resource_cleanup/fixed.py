"""Scenario 5 fixed: a context manager guarantees close() runs, deterministically,
even on an exception -- not dependent on the garbage collector's timing."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

with open("/etc/hostname") as f:
    data = f.read()
print(f"read {len(data)} bytes, closed={f.closed}")
