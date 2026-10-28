"""Scenario 5 fixed: a context manager guarantees close() runs, deterministically,
even on an exception -- not dependent on the garbage collector's timing."""
with open("/etc/hostname") as f:
    data = f.read()
print(f"read {len(data)} bytes, closed={f.closed}")
