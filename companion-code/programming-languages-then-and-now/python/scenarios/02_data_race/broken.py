"""Scenario 2: data race. `counter += 1` is LOAD/ADD/STORE as separate bytecodes.
Plain back-to-back increments rarely interleave badly even across GIL handoffs --
inserting a deliberate gap between the read and the write (as a slower real-world
read-modify-write over a network or disk would) makes the lost update reliable and
visible, which is the honest way to demonstrate it rather than relying on timing luck."""
import threading
import time

counter = 0
ITERATIONS = 200


def increment():
    global counter
    for _ in range(ITERATIONS):
        current = counter
        time.sleep(0)  # yields to the scheduler -- enough to let another thread interleave
        counter = current + 1


threads = [threading.Thread(target=increment) for _ in range(2)]
for t in threads:
    t.start()
for t in threads:
    t.join()

print(f"counter = {counter} (expected {2 * ITERATIONS})")
