# Editorial notes (cross-session handoff)

Working notes for `content/posts/bicep-for-terraform-users/index.md`, for continuing this post's editorial discussion from a different machine/session. This is a scratch/discussion file, not a pre-publish checklist. Prune items here once they're resolved rather than letting this grow into a transcript.

There's no companion code for this post yet (no runnable example has come up), so this file lives here purely to mirror the handoff-notes convention used for other posts. If actual runnable Bicep files end up getting added here later, keep this file alongside them.

## What this post is

Journey post, not a verdict. Thesis: I know Terraform well, and I'm starting a new Azure-only project in Bicep mainly to avoid running a state repository. Title asks a question ("Is Bicep as Strong as Terraform?"); the honest current answer is "too early to say, here's what I'm watching." See `content/posts/bicep-for-terraform-users/notes.md` (lowercase) for my own running log of surprises/observations as I actually use Bicep — that's the source of truth for anything experiential; don't invent results that aren't in it yet.

Planned follow-up (do not create yet, and don't rename this post's slug to make it a series): "Bicep Without a State File: What Held Up", slug `bicep-no-state-file-follow-up`, links back to this post.

## Open: VERIFY flags in the post, unresolved

Two `<!-- VERIFY -->` comments are in the post text, neither confirmed against docs yet:

- The "no state file" claim, that Bicep deploys straight against Azure Resource Manager with no `.tfstate`-equivalent I manage (ARM still tracks deployments itself).
- How incremental mode, complete mode, and deployment stacks each handle a resource removed from the template (auto-delete vs. left alone vs. something else).

## Open: what-if GA date, not yet flagged in the post

Discussed in chat: `az deployment ... what-if` (and the PowerShell `-WhatIf` equivalent) already exists and is referenced in the "What I'm taking on" section as the open question (how *reliable* it is, not whether it exists). I estimated it went GA around 2020/2021 but wasn't fully confident, and this was NOT added as a `<!-- VERIFY -->` comment in the post, pending whether the user wants that. Decide and apply if wanted.

## Open: "iac" tag was invented

Tags on the post are `["azure", "bicep", "terraform", "iac"]`. `azure` is established site-wide taxonomy; `bicep`, `terraform`, and `iac` are new since nothing existing fit this topic. Flagged to the user once; no explicit confirmation either way yet.

## Open: section 4 placeholder, waiting on real experience

`<!-- TODO(notes): ... -->` placeholder still in the post for "A first Bicep deployment" — needs a real, actually-run deployment from this project. `notes.md` currently only has the two seed entries (didn't know Bicep's deletion behavior; main reason for Bicep is no state repo). Fill this in once there's real material in notes.md, using the user's own wording as closely as possible; don't fill it from this NOTES.md or from general Bicep knowledge.

## Resolved, for context (no action needed)

- Cover image added: `schaferle-man-2037255_1920.jpg` (Pixabay), front matter caption set with full attribution.
- Added a paragraph on what I like about Terraform (cross-cloud portability, the feature being given up since this project is Azure-only).
- Added a paragraph on liking HCL's consistency but not loving it at scale, plus a corresponding open-question bullet ("Syntax at scale") in "What I'm taking on."
- Confirmed with the user: Terraform's format is HCL, not YAML (user had misspoken; corrected before writing).
- Confirmed with the user: Bicep does have a pre-deployment preview (`what-if`), contrary to an assumption they raised in chat; this is why it's called out above as not-yet-flagged rather than wrong.
