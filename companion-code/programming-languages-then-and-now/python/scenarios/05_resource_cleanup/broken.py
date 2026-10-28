"""Scenario 5: resource cleanup. No context manager, no explicit close. CPython's
refcounting GC happens to close this particular file promptly when `f` goes out of
scope, but that's an implementation detail of CPython, not a language guarantee --
PyPy or a long-lived reference elsewhere would leave the descriptor open longer."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

f = open("/etc/hostname")
data = f.read()
print(f"read {len(data)} bytes, closed={f.closed}")
