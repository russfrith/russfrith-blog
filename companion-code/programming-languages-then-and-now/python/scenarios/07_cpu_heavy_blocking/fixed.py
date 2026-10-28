"""Scenario 7 fixed: run the CPU-bound call in a thread pool executor via
loop.run_in_executor, so the event loop thread stays free to run the heartbeat.
Note this still shares the GIL with the heartbeat task in the default build --
it helps because asyncio can poll/schedule around it, not because of true
parallelism. See the free-threaded (3.14t) comparison in output.txt."""
import asyncio
import math
import sys
import time

print(f"[python {sys.version.split()[0]}, gil_enabled={sys._is_gil_enabled()}]", file=sys.stderr)


def cpu_bound(iterations: int) -> float:
    total = 0.0
    for i in range(iterations):
        total += math.sqrt(i)
    return total


async def heartbeat(ticks: list[int]):
    while True:
        await asyncio.sleep(0.1)
        ticks[0] += 1
        print(f"heartbeat tick {ticks[0]} at {time.monotonic():.3f}")


async def main():
    ITERATIONS = 300_000_000  # same workload across every language in this scenario
    ticks = [0]
    hb = asyncio.create_task(heartbeat(ticks))
    loop = asyncio.get_running_loop()
    start = time.monotonic()
    print("starting CPU-bound work in an executor thread...")
    result = await loop.run_in_executor(None, cpu_bound, ITERATIONS)
    elapsed = time.monotonic() - start
    print(f"CPU-bound work finished after {elapsed:.3f}s, heartbeat ticks seen: {ticks[0]}, result: {result}")
    hb.cancel()


asyncio.run(main())
