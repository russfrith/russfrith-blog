Node's type stripping (above) does NOT type-check -- this confirms the real compiler, run with the project's strict settings, ALSO
reports zero errors on broken.ts (the `as Record` assertion compiles clean despite
being wrong at runtime), not just that Node executed it without complaint.
