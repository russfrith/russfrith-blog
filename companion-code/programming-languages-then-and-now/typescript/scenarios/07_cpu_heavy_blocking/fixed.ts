// Scenario 7 fixed: offload the CPU-bound work to a worker_thread, which runs on
// its own OS thread, leaving the main thread's event loop free to keep ticking.
import { Worker, isMainThread, parentPort } from "node:worker_threads";
import { fileURLToPath } from "node:url";

function cpuBound(iterations: number): number {
  let total = 0;
  for (let i = 0; i < iterations; i++) {
    total += Math.sqrt(i);
  }
  return total;
}

const ITERATIONS = 300_000_000; // same workload across every language in this scenario

if (isMainThread) {
  let ticks = 0;
  const timer = setInterval(() => {
    ticks++;
    console.log(`heartbeat tick ${ticks} at ${Date.now()}`);
  }, 100);

  const start = Date.now();
  console.log("starting CPU-bound work on a worker thread...");
  const worker = new Worker(fileURLToPath(import.meta.url));
  worker.on("message", (result) => {
    console.log(`CPU-bound work finished after ${Date.now() - start}ms, heartbeat ticks seen: ${ticks}, result: ${result}`);
    clearInterval(timer);
  });
} else {
  const result = cpuBound(ITERATIONS);
  parentPort!.postMessage(result);
}
