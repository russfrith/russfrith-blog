// Scenario 7: CPU-heavy work blocking the event loop. A synchronous CPU-bound
// function runs on Node's single JS thread, so a setInterval "heartbeat" that
// should tick every 100ms can't run at all until the CPU-bound work finishes --
// there is no preemption.
function cpuBound(iterations: number): number {
  let total = 0;
  for (let i = 0; i < iterations; i++) {
    total += Math.sqrt(i);
  }
  return total;
}

let ticks = 0;
const timer = setInterval(() => {
  ticks++;
  console.log(`heartbeat tick ${ticks} at ${Date.now()}`);
}, 100);

const ITERATIONS = 300_000_000; // same workload across every language in this scenario
const start = Date.now();
console.log("starting CPU-bound work on the main thread...");
const result = cpuBound(ITERATIONS);
console.log(`CPU-bound work finished after ${Date.now() - start}ms, heartbeat ticks seen: ${ticks}, result: ${result}`);

clearInterval(timer);
