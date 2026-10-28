// Scenario 3: TypeScript's `number` is an IEEE-754 double. Past 2^53 it silently
// loses integer precision -- a different failure mode than classic wraparound, but
// still a silent wrong answer. No broken/fixed pair: BigInt is the real fix, shown
// alongside, but ordinary `number` arithmetic has no "fixed" version of itself.
const x = Number.MAX_SAFE_INTEGER; // 2^53 - 1
console.log(`Number.MAX_SAFE_INTEGER + 1 = ${x + 1}`);
console.log(`Number.MAX_SAFE_INTEGER + 2 = ${x + 2}`); // same value as +1 -- precision lost silently

const bx = BigInt(Number.MAX_SAFE_INTEGER);
console.log(`BigInt(MAX_SAFE_INTEGER) + 2n = ${bx + 2n}`); // exact
