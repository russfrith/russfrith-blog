# TODO for Russ

## Domain source for the discount feature

Per the brief: `companion-code/ef-core-dapper-light-cqrs/src/Domain/` has an `Order` aggregate that fit reasonably well. I copied and trimmed `Order.cs`, `OrderLine.cs`, `OrderStatus.cs`, and `DomainException.cs` from there (dropped `SubmittedAtUtc` bookkeeping that wasn't needed, dropped `Cancel`/`MarkShipped`, kept `Submit`/`EnsureModifiable`), then added `DiscountCode.cs` and `AppliedDiscount.cs` from scratch for the eligibility/stacking/expiry/usage-limit rules. Nothing references the other companion solution; everything needed was copied in.

## Personal anecdotes — placeholders in the post marked `[YOUR EXPERIENCE]`

The hook section's placeholder is filled in now (vertical-slice data-access duplication plus a MediatR-ceremony opinion, with a direct Bogard quote backing the duplication point). Search the post for any remaining `[YOUR EXPERIENCE]` and fill in or delete each one:

- "The questions that decide": a real case where team size or ownership boundaries drove the choice.
- The hybrid section: whether you've actually shipped the "feature folders with a domain core inside" hybrid, and how it held up.
- Conclusion: whether you've actually revisited an architecture choice on a real project as requirements changed.

## Unverified or soft claims to double-check

- MediatR licensing facts (12.x and earlier MIT, 13+ commercial via Lucky Penny Software, launched July 2, 2025) were checked against jimmybogard.com and mediatr.io at the time of writing (2026-11-03). Re-check before publishing if there's any lag, since pricing/terms could change.
- The Jimmy Bogard quote in the hook section ("does assume that your team understands code smells and refactoring...") was pulled from [jimmybogard.com/vertical-slice-architecture/](https://jimmybogard.com/vertical-slice-architecture/) on 2026-11-03. Re-check it's still live and unchanged before publishing.
- No benchmarks, adoption statistics, or survey data are cited anywhere in the post, per your instruction — the file-count/line-count tables are the only numbers, and they're pulled directly from the companion code (see below).

## Where a diagram would help

- Section 2 (the two axes): now an SVG (`onion-vs-slice-diagram.svg`) instead of the original ASCII block, showing the same feature cutting across onion's rings vs. sitting in one vertical-slice folder.
- Section 7 (side-by-side example): no diagram, just the file-count tables. A simple folder-tree screenshot/diagram for each style could reinforce the point if you want one.

## Suggested back-link (not applied — your call)

If you want the Dapper post to point forward to this one, a one-line addition near its own "no mediator" discussion (`content/posts/ef-core-dapper-light-cqrs/index.md`, in the "No mediator" bullet under "The architecture: light CQRS") could read something like:

> For a broader look at when plain handlers without a mediator fit, see [Vertical Slice vs. Onion Architecture](/posts/when-to-use-vertical-slice-architecture/).

I didn't make this edit since the brief says not to touch that post.

## File/line counts used in the post's tables

Pulled directly from this solution on 2026-11-03 (excludes shared `AppDbContext`/DI wiring/`Program.cs` endpoint registration on both sides, since those are infrastructure, not feature code):

- Product catalogue: Onion 15 files / 197 lines; Vertical Slice 13 files / 161 lines.
- Discount code: Onion 14 files / 316 lines; Vertical Slice 6 files / 118 lines.

If you add or change code, regenerate these before publishing (a one-liner per file list, or just recount by hand) so the post doesn't drift from the code.
