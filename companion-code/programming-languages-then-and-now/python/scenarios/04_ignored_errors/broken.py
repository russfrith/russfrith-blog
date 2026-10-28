"""Scenario 4: ignored errors. A bare `except: pass` swallows every exception,
including ones that have nothing to do with the missing file (e.g. a typo later
in the block), and the program carries on as if nothing happened."""
try:
    with open("/no/such/path.txt") as f:
        data = f.read()
except:  # noqa: E722 -- the point of this snippet is that this is wrong
    pass

print("done")  # no idea whether `data` even exists
