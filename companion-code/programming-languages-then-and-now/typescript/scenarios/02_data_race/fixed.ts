// Scenario 2 fixed: Atomics.add performs the read-modify-write atomically across
// the shared memory, the same role a Mutex plays in the other languages.
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
    Atomics.add(view, 0, 1);
  }
  process.exit(0);
}
