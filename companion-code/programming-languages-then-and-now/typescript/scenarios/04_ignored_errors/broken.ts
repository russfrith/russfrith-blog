// Scenario 4: ignored errors. Calling an async function without `await` or `.catch`
// fires an unhandled promise rejection -- which Node reports but does NOT stop the
// program for by default, so execution continues past the error.
import { readFile } from "node:fs/promises";

async function main() {
  readFile("/no/such/path.txt"); // no await, no catch: rejection goes unhandled
  console.log("done"); // runs immediately, before the rejection is even reported
}

main();
