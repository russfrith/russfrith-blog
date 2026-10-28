// Scenario 5 fixed: close() in a finally block (or `using`/explicit resource
// management) runs deterministically, independent of GC timing.
import { open } from "node:fs/promises";

async function main() {
  const fh = await open("/etc/hostname", "r");
  try {
    const buf = Buffer.alloc(64);
    const { bytesRead } = await fh.read(buf, 0, 64, 0);
    console.log(`read ${bytesRead} bytes, fd closed deterministically`);
  } finally {
    await fh.close();
  }
}

main();
