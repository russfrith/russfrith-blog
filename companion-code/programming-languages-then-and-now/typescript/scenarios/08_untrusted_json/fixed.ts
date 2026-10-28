// Scenario 8 fixed: validate the shape at the boundary instead of asserting it.
interface Item {
  id: number;
  label: string;
  count: number;
}

function isItem(value: any): value is Item {
  return (
    typeof value?.id === "number" &&
    typeof value?.label === "string" &&
    typeof value?.count === "number"
  );
}

async function fetchAndDecode(url: string) {
  const resp = await fetch(url);
  const raw = await resp.json();
  if (!isItem(raw)) {
    console.log(`${url}: validation failed, rejecting payload`);
    return;
  }
  console.log(`${url}: count = ${raw.count}`);
}

async function main() {
  await fetchAndDecode("http://mockserver:8080/json/missing-field");
  await fetchAndDecode("http://mockserver:8080/json/wrong-type");
}

main();
