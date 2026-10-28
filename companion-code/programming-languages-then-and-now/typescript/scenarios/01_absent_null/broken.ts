// Scenario 1: absent value. TypeScript's type system is erased at compile time.
// Declaring `count: number` does not make it true at runtime -- JSON.parse returns
// `any`, and casting it to our interface tells the compiler to trust us, wrongly.
interface Record {
  id: number;
  label: string;
  count: number;
}

const json = '{"id": 1, "label": "widget"}'; // "count" is absent
const record = JSON.parse(json) as Record; // compiler now believes count: number
console.log(`count = ${record.count}`); // prints "undefined", not a type error
console.log(`typeof count = ${typeof record.count}`); // "undefined" -- the type lied
