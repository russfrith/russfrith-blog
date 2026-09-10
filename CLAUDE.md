# CLAUDE.md

Guidance for Claude Code when working in this repo. This is a personal Hugo blog (russfrith-blog). I write the posts myself; this file just documents conventions so tooling assistance stays consistent.

## Project basics

- Static site built with [Hugo](https://gohugo.io/).
- Posts live under `content/posts/<slug>/index.md`, each in its own folder alongside any images used in that post.
- Preview locally with:
  ```
  hugo server -D
  ```
  (`-D` includes draft posts in the preview.)
- Build with:
  ```
  hugo
  ```

## Front matter conventions

Each post's front matter follows this shape:

```yaml
---
title: "Post Title"
description: "One-sentence description used for previews/SEO"
date: 2026-04-20T10:04:30-04:00
draft: true
cover:
  image: some-image-filename.jpg
  alt: "Short alt text"
  caption: "Photo by [Name](unsplash-profile-url) on [Unsplash](unsplash-photo-url)"
tags: ["tag1", "tag2"]
categories: ["category"]
---
```

- New posts always start with `draft: true`. Never set `draft: false` on a new or existing post — that's a publishing decision I make myself, not something to do automatically as part of content edits.
- `date` should be updated to reflect actual publish time whenever the post is eventually published (i.e. whenever I flip `draft: false` myself).
- Cover images may come from Unsplash, Pexels, Midjourney, or elsewhere — use whatever attribution format fits the source:
  - Unsplash/Pexels (or similar licensed stock sites): `caption: "Photo by [Name](profile-url) on [Site](photo-url)"`
  - AI-generated (Midjourney, etc.): `caption: "Image generated with Midjourney"` (no attribution link needed, but note the tool used)
  - No attribution needed: omit the `caption` field entirely rather than leaving a placeholder
- Don't assume Unsplash by default — check the actual image source before writing a caption.
- Don't invent tags/categories that aren't already in use elsewhere in the site unless asked — check existing posts for the established taxonomy first.

## Writing style

- Straight quotes/apostrophes, not curly/smart quotes.
- Prefer tight, direct prose. Merge short one-sentence paragraphs where it improves flow rather than leaving them fragmented.
- Use bullet lists for actual lists rather than list-like prose paragraphs.
- Keep my voice: plain, first-person, a little dry. Don't over-polish into generic "content marketing" tone.

## Companion code for posts

Some posts reference runnable code — benchmarks, demos, sample projects — that readers can clone and run themselves. This code is NOT a benchmark-only thing; it could be any kind of runnable example a post references.

- Location: `companion-code/<post-slug>/`, using the same slug as the post's folder under `content/posts/`.
- This must stay outside `content/` entirely. Hugo treats a post's folder as a "leaf bundle" and any non-index files inside it become page resources that can get copied into the built site — so companion code living inside a post's own folder risks getting published as part of the site output. Keeping it under a top-level `companion-code/` sibling avoids that.
- `companion-code/` is long-lived and public, not scratch work — commit source files (`.csproj`, `.cs`, `Dockerfile`, etc.) so readers can run things themselves.
- Add a `.gitignore` entry for build output (`bin/`, `obj/`, or language-equivalent) — don't commit build artifacts, only source.
- Include a `README.md` in each post's companion-code folder explaining how to build and run it (local + Docker, if applicable).
- When a post references companion code, prefer showing short, relevant snippets directly in the post itself rather than assuming readers will go clone the folder to understand the point being made — the companion code is for readers who want to reproduce or extend it, not a substitute for explaining things in the post.

## What not to touch without being asked

- Don't modify files under `layouts/`, `static/`, `assets/`, or theme/config files (`hugo.toml` / `config.yaml`) unless explicitly asked.
- Don't change site-wide config, menus, or theme settings as a side effect of editing a post.

## Git / commit conventions

- Use [Conventional Commits](https://www.conventionalcommits.org/).
- Blog content changes (new posts, edits, revisions) use `feat(posts): ...` — treat published/updated content as the "feature," since that's the product of this repo.
- Keep commit subject lines focused on the content change itself, not implementation details like flipping `draft: false`.
- Prefer single-line commit messages unless a change genuinely needs explanation that isn't obvious from the diff.
- One commit per post/topic when multiple posts change in the same session — don't bundle unrelated posts into one commit.

## Notes

- I sometimes work from different machines (desktop and laptop), so don't assume local state (uncommitted changes, editor config) persists between sessions — check `git status` at the start of a session.
