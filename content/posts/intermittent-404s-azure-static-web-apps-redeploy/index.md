---
title: "Diagnosing Intermittent 404s on Azure Static Web Apps"
description: "A field story on diagnosing intermittent 404s on custom domains and why DNS propagation is usually the culprit."
date: 2025-12-01T12:02:31-04:00
draft: false
cover:
  image: russellf4_photorealistic_modern_cloud_computing_scene_glowing_a_521c2872-5244-477c-a7af-eb75a9ee75a2.png
  alt: "Server room"
  caption: "Image created by author using [Midjourney](https://www.midjourney.com/)"
tags: ["azure"]
categories: ["cloud"]
---

There's a particular kind of issue that's more frustrating than something being completely broken:

> When everything *mostly* works.

I ran into this recently with an Azure Static Web App. The deployment was successful. The default Azure URL worked perfectly. DNS looked correct.

And yet, the custom domain would intermittently return a 404.

Not always. Just enough to make you question everything.

---

## If You're Seeing Intermittent 404s, Start Here

Before going deep, check these in order:

1. **Confirm DNS is pointing to the correct Static Web App**
   * Make sure you're not still pointing to an old environment
   * Double-check ALIAS / ANAME / CNAME targets
   * Remove any references to previous environments so there's no overlap or ambiguity

2. **Verify the domain in Azure (TXT record)**
   * This confirms ownership, not routing

3. **Assume you're mid-propagation**
   * If it works sometimes but not always, this is the most likely explanation
   * Wait longer than feels reasonable. Even correct configurations take time to converge globally

4. **Test from multiple networks**
   * Your machine may be cached. Try another device, another network, or a mobile connection
   * If it works everywhere, you're done

5. **Avoid making multiple DNS changes at once**
   * Change, wait, validate, then repeat
   * Stacking edits (delete a record, add a new one, re-verify, adjust again) makes it harder to tell what actually fixed things

If those don't resolve it, then it's worth digging into what's happening underneath.

---

## The Symptoms

Here's what this looked like in practice:

* The Static Web App deployed successfully
* The default Azure URL worked consistently
* The custom domain worked in one browser but not another, and on one device but not another
* The custom domain intermittently returned 404s
* DNS queries (`nslookup`) appeared correct
* Azure showed the domain as verified

At a glance, everything looked fine. But clearly it wasn't fully resolved yet.

---

## What Was Actually Happening

This wasn't a single issue. It was a handful of small, reasonable steps that hadn't lined up yet.

**DNS wasn't fully aligned.** Even after updating records, different resolvers were returning different answers. Some requests were hitting the correct endpoint; others weren't. That's classic propagation behavior.

**Azure verification gave a false sense of completion.** The TXT record check succeeded, which made it seem like everything was ready. But verification only proves ownership, it doesn't guarantee traffic is routing correctly.

**Caching made it feel random.** Local DNS caching and browser caching added noise: one browser worked, another didn't; one device worked, another didn't. It wasn't random, it was just inconsistent state settling out over time.

---

## Lessons Worth Keeping

**Intermittent success is a signal.** If something works sometimes and fails other times, think propagation or caching before you think misconfiguration.

**Verification isn't the same as reachability.** Passing Azure's domain check confirms ownership. It says nothing about whether traffic is actually routing correctly yet.

---

## Final Thought

You don't need to fully understand how you got into this state to fix it.

In most cases, it comes down to one of two things:

* DNS is pointing to the wrong place
* DNS hasn't finished catching up yet

And when your site works, except when it doesn't, you're probably closer to the answer than you think.
