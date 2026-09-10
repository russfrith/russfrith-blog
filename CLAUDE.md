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
- Cover images are typically sourced from Unsplash; keep the caption attribution format above (photographer name linked, "Unsplash" linked to the photo).
- Don't invent tags/categories that aren't already in use elsewhere in the site unless asked — check existing posts for the established taxonomy first.

## Writing style

- Straight quotes/apostrophes, not curly/smart quotes.
- Prefer tight, direct prose. Merge short one-sentence paragraphs where it improves flow rather than leaving them fragmented.
- Use bullet lists for actual lists rather than list-like prose paragraphs.
- Keep my voice: plain, first-person, a little dry. Don't over-polish into generic "content marketing" tone.

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
