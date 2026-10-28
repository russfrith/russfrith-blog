// Fixed: a user-defined type guard actually checks the shape at the boundary,
// instead of asserting it away.
interface User {
  name: string;
}

function isUser(value: unknown): value is User {
  return typeof value === "object" && value !== null && typeof (value as any).name === "string";
}

const data: unknown = { notName: "oops" };
if (isUser(data)) {
  console.log(`Hello, ${data.name.toUpperCase()}`);
} else {
  console.log("not a valid User");
}
