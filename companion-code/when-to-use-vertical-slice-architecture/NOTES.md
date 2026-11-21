# Editorial notes (cross-session handoff)

Working notes for `content/posts/when-to-use-vertical-slice-architecture/index.md`, for continuing this post's editorial discussion from a different machine/session. This is a scratch/discussion file, not a pre-publish checklist; see `TODO.md` for that. Prune items here once they're resolved rather than letting this grow into a transcript.

## Open: thesis vs. stated personal leaning (unresolved tension)

The post's formal thesis says neither onion nor vertical slice architecture is a sensible default. The "my own leaning" paragraph right after it says that for a typical system, I organize by feature and add an onion-style core only where complexity earns it, which is itself a soft default. Pressure-testing this (prompted by an infographic arguing vertical slices should be the default for most .NET apps) led to this conclusion, not yet applied to the post:

- Feature-first-by-default is a reasonable **tie-breaker**, not an unconditional default. It assumes complexity is lumpy (concentrated in one or two features), not pervasive.
- Three conditions under which it's the wrong call, worth naming explicitly if we revise the thesis/leaning section:
  1. **Pervasive, not concentrated, complexity** — a domain where nearly every feature touches the same dense ruleset (e.g., underwriting, cross-catalog pricing). Feature-first here means duplicating shared rules across slices or reconstructing a domain layer anyway.
  2. **Team shape that mirrors layers, not features** — a dedicated platform/domain team separate from feature teams fights against vertical slices by construction; onion matches that org structure.
  3. **Infrastructure churn over feature churn** — systems where the database/integration layer is more likely to change (regulatory migrations, vendor swaps) than new features benefit more from onion's swappable infrastructure.
- Possible revision: reframe "the five questions" as what can override the feature-first tie-breaker in either direction, rather than purely "how much rigor to add on top of a feature-first default."
- **Decision needed:** sharpen the thesis/leaning paragraphs (currently right after the hook, before "## The two axes") to reflect this, or leave the current framing as is. Not yet decided either way.

## Open: two Derek Comartin additions, drafted but never committed

Both came out of the duplication/MediatR discussion. Neither is in the post file yet; both are still just drafted text below, pending a decision on whether to use them (last instruction was "integrate those ideas... let us not commit until we explicitly decide what is our final position... our position will evolve").

**Addition 1 — append to the end of the hook's first new paragraph (the one with the Bogard quote, currently ending "...is a team-maturity question as much as an architecture one."):**

> [Derek Comartin](https://codeopinion.com/?p=10468) pushes back on the opposite worry, that slices can never share anything: his answer to drift like this isn't a system-wide shared model, it's a narrowly scoped one, built specifically for the features that actually need it, not a general-purpose repository everyone's forced through.

**Addition 2 — new second paragraph after the existing MediatR-ceremony paragraph in the hook (the one ending "...the mediator is ceremony no matter how big the codebase around it gets."):**

> [Derek Comartin](https://codeopinion.com/why-use-mediatr-3-reasons-why-and-1-reason-not/) raises a sharper, different objection that applies even once you've cleared that bar: MediatR dispatches everything in-process, on the same thread and inside the same try/catch as the caller. That's fine for a genuine request/response, but it's the wrong tool for anything that's conceptually an event: if several handlers should react independently to "order submitted," and one of them throws, that exception bubbles straight back to whoever published it instead of failing on its own. Pub/sub wants decoupled, often out-of-process delivery; a mediator can't give you that no matter how many pipeline behaviors sit in front of it.

Note: this is a genuinely different argument from the ceremony point already in the post (it's about in-process coupling for event/notification-style dispatch, not about ceremony on simple request/response CRUD), so it reads as a second, distinct point rather than reinforcement.

## Open: post is over the original word-count target

Original brief target was roughly 2,500–3,500 words. Current count (as of 2026-11-20): **4,249 words**, driven mainly by the hook-section additions (Bogard quote, MediatR ceremony paragraph), the expanded "Vertical slices don't require MediatR" section, and the rules-engine aside. Flagged to the user once already; no decision yet on whether to trim.

## Resolved, for context (no action needed)

- Cover image added (`soham-banerjee-h-Cp-u0J-gc-unsplash-1900x1280.jpg`), front matter updated.
- ASCII diagram replaced with `onion-vs-slice-diagram.svg`.
- `[YOUR EXPERIENCE]` hook placeholder replaced with the duplication/Bogard paragraph and the MediatR-ceremony paragraph.
- Colon overuse fixed in the committed text; a rule was added to the repo's `CLAUDE.md` against the "setup: payoff" colon crutch.
- Dedicated section added and renamed to "Vertical slices don't require MediatR," covering: not required by VSA, ceremony cost, real benefits, endpoint-filter/decorator/middleware distinctions, don't swap one mediator for another, closing organizational-vs-implementation-decision takeaway.
- Aside added after the discount-code code comparison explaining why guard clauses were used instead of a specification pattern or rules engine.

## Still open from the original `TODO.md` (not duplicated here, see that file)

- Remaining `[YOUR EXPERIENCE]` placeholders: team-size/ownership case, whether the hybrid has actually been shipped, whether an architecture choice was actually revisited later.
- Suggested one-line back-link for the Dapper post (not applied, your call).
