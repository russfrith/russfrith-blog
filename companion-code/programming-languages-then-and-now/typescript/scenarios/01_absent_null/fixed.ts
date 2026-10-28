// Scenario 1 fixed: validate the parsed value at the runtime boundary instead of
// trusting a type assertion. The type system can't do this for you -- a validator
// (hand-written here; a library like zod does it declaratively) must.
interface Record {
  id: number;
  label: string;
  count: number;
}

function parseRecord(json: string): Record {
  const raw = JSON.parse(json) as Record;
  if (typeof raw.count !== "number") {
    throw new Error("count missing or wrong type");
  }
  return raw;
}

const json = '{"id": 1, "label": "widget"}';
try {
  const record = parseRecord(json);
  console.log(`count = ${record.count}`);
} catch (e) {
  console.log(`validation failed: ${(e as Error).message}`);
}
