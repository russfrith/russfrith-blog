Confirmed with the real compiler, not just by Node executing it without complaint: `tsc` reports zero errors on broken.ts for either
call -- the declared `Item` type is simply wrong at runtime in both cases, and
nothing catches it until the value is printed.
