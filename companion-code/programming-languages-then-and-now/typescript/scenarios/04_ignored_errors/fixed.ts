// Scenario 4 fixed: await it and handle the rejection explicitly.
import { readFile } from "node:fs/promises";

async function main() {
  try {
    const data = await readFile("/no/such/path.txt");
    console.log(`read ${data.length} bytes`);
  } catch (e) {
    console.log(`read failed: ${(e as Error).message}`);
  }
}

main();
