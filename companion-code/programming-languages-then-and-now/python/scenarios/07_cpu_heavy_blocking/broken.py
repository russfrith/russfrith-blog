"""Scenario 7: CPU-heavy work blocking the event loop. A synchronous CPU-bound
call inside an async function runs on asyncio's single event-loop thread with no
`await` points, so a heartbeat task that should tick every 100ms can't run until
the CPU-bound call returns -- there's no preemption in cooperative scheduling."""
import asyncio
import math
import time


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
    ticks = [0]
    hb = asyncio.create_task(heartbeat(ticks))
    start = time.monotonic()
    print("starting CPU-bound work inside the event loop...")
    cpu_bound(30_000_000)
    elapsed = time.monotonic() - start
    print(f"CPU-bound work finished after {elapsed:.3f}s, heartbeat ticks seen: {ticks[0]}")
    hb.cancel()


asyncio.run(main())
