# russfrith-blog

This repository contains the source content for my blog, published at [russfrith.com](https://russfrith.com). It's public mainly so readers can see how a post is written, follow its history, and where applicable, get the code referenced in a post.

This is not a general-purpose Hugo starter or theme; it isn't intended for others to clone and host as their own site.

## Post source vs. the published blog

Posts live under `content/posts/<slug>/`. Not everything here has been published. Each post's front matter includes a `draft` flag, and posts with `draft: true` are excluded from the built site; they may be unfinished, unreviewed, or scheduled for later. If you're browsing this repository, treat draft posts as work in progress, not as final or endorsed content. The published, canonical version of any post is whatever appears on [russfrith.com](https://russfrith.com), not necessarily the current state of a file here.

## Companion code

Some posts reference runnable code — benchmarks, demos, sample projects — that goes beyond a short snippet. Where that applies, it lives under:

```
companion-code/<post-slug>/
```

using the same folder name as the corresponding post under `content/posts/`. For example, the post in `content/posts/spanning-the-gap-with-an-array-of-options/` has its benchmarks in `companion-code/spanning-the-gap-with-an-array-of-options/`.

Each companion-code folder has its own `README.md` with instructions for building and running that project. Not every post has a companion-code folder — only those that reference code substantial enough to warrant a standalone, runnable example.

## Licensing

Different material in this repository is licensed differently; there is no single license for the repository as a whole.

- **Blog post content** (the written articles under `content/posts/*/index.md`) is licensed under [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/) — see [`LICENSE-CONTENT`](LICENSE-CONTENT). Copyright © Russell Frith.
- **Companion code** (`companion-code/`) is licensed under the MIT License — see [`companion-code/LICENSE`](companion-code/LICENSE). Copyright © Russell Frith. Third-party dependencies referenced by an individual companion-code project (packages, base images, etc.) remain under their own licenses.
- **Hugo theme** — the site uses [PaperMod](https://github.com/adityatelange/hugo-PaperMod), included as a git submodule at `themes/PaperMod/` (see `.gitmodules`). PaperMod is MIT-licensed; its license text is at `themes/PaperMod/LICENSE` and is unmodified.
- **Theme-derived template overrides** — `layouts/partials/footer.html` and `layouts/partials/share_icons.html` are modified copies of PaperMod's own partials of the same name and remain subject to PaperMod's MIT license and copyright notice, not the CC BY-NC 4.0 content license. `layouts/shortcodes/aside.html` is original.
- **Post cover images** — each post's front matter includes a `cover.caption` field crediting the image source (Unsplash, Pexels, "generated with Midjourney", etc.). These images are third-party or AI-generated and are used under the terms of their respective source, not under the CC BY-NC 4.0 license that covers post text.
