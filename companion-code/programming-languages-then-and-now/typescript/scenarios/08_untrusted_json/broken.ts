// Scenario 8: parsing untrusted JSON. Same lie as scenario 1's `as` assertion,
// applied to a real network response -- `tsc` sees nothing wrong with either call.
interface Item {
  id: number;
  label: string;
  count: number;
}

async function fetchAndDecode(url: string) {
  const resp = await fetch(url);
  const item = (await resp.json()) as Item; // trusts the shape completely
  console.log(`${url}: count = ${item.count}, typeof = ${typeof item.count}`);
}

async function main() {
  await fetchAndDecode("http://mockserver:8080/json/missing-field");
  await fetchAndDecode("http://mockserver:8080/json/wrong-type");
}

main();
