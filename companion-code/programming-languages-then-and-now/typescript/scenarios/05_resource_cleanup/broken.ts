// Scenario 5: resource cleanup. A raw file descriptor opened via fs.open is never
// closed. Node/V8's GC collects the JS FileHandle object eventually, but the
// underlying OS file descriptor is a finalizer-driven cleanup, not deterministic --
// under enough concurrent opens a process can hit its fd limit before GC catches up.
import { open } from "node:fs/promises";

async function main() {
  const fh = await open("/etc/hostname", "r");
  const buf = Buffer.alloc(64);
  const { bytesRead } = await fh.read(buf, 0, 64, 0);
  console.log(`read ${bytesRead} bytes, fd ${fh.fd} never explicitly closed`);
}

main();
