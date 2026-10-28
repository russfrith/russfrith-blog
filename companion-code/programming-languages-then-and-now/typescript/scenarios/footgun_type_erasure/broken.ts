// TypeScript's signature footgun: types are fully erased at runtime. A type
// assertion (`as`) is a promise to the compiler, not a check -- it compiles clean
// and then lies at runtime with no exception raised at the point of the lie.
interface User {
  name: string;
}

const data: unknown = { notName: "oops" };
const user = data as User; // compiler now trusts this completely
console.log(`Hello, ${user.name.toUpperCase()}`); // runtime TypeError, far from the `as`
