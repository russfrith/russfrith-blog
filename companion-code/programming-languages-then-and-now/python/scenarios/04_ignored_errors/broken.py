"""Scenario 4: ignored errors. A bare `except: pass` swallows every exception,
including ones that have nothing to do with the missing file (e.g. a typo later
in the block), and the program carries on as if nothing happened."""
import sys
print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

try:
    with open("/no/such/path.txt") as f:
        data = f.read()
except:  # noqa: E722 -- the point of this snippet is that this is wrong
    pass

print("done")  # no idea whether `data` even exists
