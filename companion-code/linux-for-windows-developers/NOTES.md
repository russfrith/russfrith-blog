# Editorial notes (cross-session handoff)

Working notes for `content/posts/linux-for-windows-developers/index.md`, for continuing this post's editorial discussion from a different machine/session. This is a scratch/discussion file, not a pre-publish checklist. Prune items here once they're resolved rather than letting this grow into a transcript.

There's no companion code for this post (it's a reference/explainer post, not something with a runnable example attached), so this file lives here purely to mirror the handoff-notes convention used for other posts.

## What this post is

A long-form reference post (`draft: true`, currently ~7,250 words) teaching Windows developers the Linux mental model: filesystem layout, permissions, POSIX, processes/services/logs, text tools, and a Windows-to-Linux translation table. Originally scoped and written as server-only ("A Server Mental Model" in the title). The user then asked to broaden scope to also cover Linux as a workstation/dev environment and WSL, while keeping a clearly server-specific section for hardening (SSH, firewall, auto-updates, systemd services). That broadening is done: title/description are now scope-neutral, the quick-start checklist and the "Getting in and staying safe"/"Running things" sections are explicitly noted as server-focused, and the old short WSL sidebar was expanded into a full "Linux as a workstation: daily-driver use and WSL" section (dev tooling via package managers, dotfiles, then WSL specifics). Still one post, not a series — no "Part 1" language, no "stay tuned."

## Open: `[AUTHOR: ...]` placeholder still in the post

The hook section has a generic (non-personal) description of the `Config.json` vs `config.json` case-sensitivity mistake, followed by `[AUTHOR: insert a personal story here]`. That placeholder is intentional per the original brief (don't invent a personal anecdote) and is still unfilled. Needs the user's own real story before this goes out of draft.

## Open: `<!-- TODO verify -->` comments, unresolved

A dozen inline `<!-- TODO verify: ... -->` comments remain in the post, flagging facts not independently confirmed beyond a general web search pass. By section:

- POSIX: exact current maintaining bodies/edition name+year; whether any system holds formal POSIX certification today.
- SSH hardening: current OpenSSH default/accepted values for `PermitRootLogin`; current recommended key type and deprecation status of older types.
- Firewall: current default zone behavior and exact `firewalld` service name strings across supported RHEL/Fedora versions.
- Auto-updates: current default `dnf-automatic` package/timer names and whether the install timer is security-only by default.
- systemd: minimal required unit file fields and current recommended defaults.
- `logrotate`: default installation/scheduling behavior across Debian/Ubuntu and Red Hat-family distros.
- Translation table: whether the listed PowerShell cmdlet/alias names are still accurate on the latest shipping version.
- WSL: WSL1's current support status and exact system-call translation description; current networking model and systemd support/configuration; `/mnt/c` performance characteristics and default case-sensitivity setting.

None of these have been checked against primary docs beyond the initial search pass. Resolve before publishing, or leave the comments in if the user is fine shipping with them (Hugo's default renderer omits HTML comments from output, so they're invisible to readers either way).

## Resolved, for context (no action needed)

- Cover image: `pexels-realtoughcandy-11034131.jpg`, credited to RealToughCandy.com via the standard Pexels caption format. Alt text revised from the user's draft wording ("sitting on an ice tray") to "lying on a blue ice cube tray" after checking the actual image — confirmed by the user.
- Tags reused from existing site taxonomy rather than invented: `["linux", "windows"]`. Category: `cloud` (matches other server/infra posts like `practical-proxmox` and `building-a-quiet-nas-for-a-real-office`).
- No manual table of contents — relies on the theme's (PaperMod) automatic `ShowToc: true` rendering, per the original brief's preference for that when available.
- Verified the site builds cleanly with `hugo --buildDrafts` after each round of edits (most recently after the workstation/WSL scope broadening); no errors, post renders.
- Scope-broadening request (this session) fully applied: see "What this post is" above for what changed.
