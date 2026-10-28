"""Scenario 2 fixed: a Lock makes the read-modify-write atomic, so the artificial
yield in between the read and the write (same as broken.py) can no longer cause a
lost update -- the whole sequence happens inside the lock."""
import sys
import threading
import time

print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)

counter = 0
ITERATIONS = 200
lock = threading.Lock()


def increment():
    global counter
    for _ in range(ITERATIONS):
        with lock:
            current = counter
            time.sleep(0)
            counter = current + 1


threads = [threading.Thread(target=increment) for _ in range(2)]
for t in threads:
    t.start()
for t in threads:
    t.join()

print(f"counter = {counter} (expected {2 * ITERATIONS})")
