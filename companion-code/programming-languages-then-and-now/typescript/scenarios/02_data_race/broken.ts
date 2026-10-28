// Scenario 2: data race. Ordinary JS/TS code is single-threaded and can't race.
// Node's worker_threads + a SharedArrayBuffer is the one place a real race can
// happen: two workers increment the same shared Int32Array slot with no atomics.
import { Worker, isMainThread, workerData } from "node:worker_threads";
import { fileURLToPath } from "node:url";

const ITERATIONS = 200_000;

if (isMainThread) {
  const sab = new SharedArrayBuffer(4);
  const view = new Int32Array(sab);
  const workers = [0, 1].map(
    () =>
      new Worker(fileURLToPath(import.meta.url), { workerData: { sab } })
  );
  let done = 0;
  for (const w of workers) {
    w.on("exit", () => {
      done++;
      if (done === workers.length) {
        console.log(`counter = ${view[0]} (expected ${2 * ITERATIONS})`);
      }
    });
  }
} else {
  const view = new Int32Array(workerData.sab);
  for (let i = 0; i < ITERATIONS; i++) {
    view[0] = view[0] + 1; // non-atomic read-modify-write: a real race
  }
  process.exit(0);
}
