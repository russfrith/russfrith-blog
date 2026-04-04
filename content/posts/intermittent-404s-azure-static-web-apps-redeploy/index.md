---
title: "Intermittent 404s on Azure Static Web Apps: Why Redeploying Can Fix It"
description: "A faster way to diagnose intermittent 404s on custom domains"
date: 2026-03-30T13:31:33-04:00
draft: true
cover:
  image: russellf4_photorealistic_modern_cloud_computing_scene_glowing_a_521c2872-5244-477c-a7af-eb75a9ee75a2.png
  alt: "Man holding keys"
  caption: "Photo by [Maurice Williams](https://unsplash.com/@mauricew98?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/s/photos/rainbow-key?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText)"
tags: ["azure"]
categories: ["cloud"]
---

There’s a particular kind of issue that’s more frustrating than something being completely broken:

> When everything *mostly* works.

I ran into this recently with an Azure Static Web App. The deployment was successful. The default Azure URL worked perfectly. DNS looked correct.

And yet… the custom domain would intermittently return a 404.

Not always. Just enough to make you question everything.

---

## If You’re Seeing Intermittent 404s, Start Here

Before going deep, check these in order:

1. **Confirm DNS is pointing to the correct Static Web App**

   * Make sure you’re not still pointing to an old environment
   * Double-check ALIAS / ANAME / CNAME targets

2. **Verify the domain in Azure (TXT record)**

   * This confirms ownership, not routing

3. **Assume you’re mid-propagation**

   * If it works sometimes but not always, this is the most likely explanation

4. **Test from multiple networks**

   * Your machine may be cached—try another device or mobile network

5. **Avoid making multiple DNS changes at once**

   * Change → wait → validate → repeat

If those don’t resolve it, *then* it’s worth digging deeper.

---

## The Symptoms

Here’s what this looked like in practice:

* The Static Web App deployed successfully
* The default Azure URL worked consistently
* The custom domain:

  * worked in one browser but not another
  * worked on one device but not another
  * intermittently returned 404s
* DNS queries (`nslookup`) appeared correct
* Azure showed the domain as verified

At a glance, everything looked fine.

But clearly, something wasn’t.

---

## What Was Actually Happening

This wasn’t a single issue—it was a combination of small, reasonable steps that didn’t line up at the same time.

### DNS Wasn’t Fully Aligned Yet

Even after updating records, different resolvers were returning different answers.

Some requests were hitting the correct endpoint. Others weren’t.

That’s classic DNS propagation behavior.

---

### Azure Verification Gave a False Sense of Completion

The TXT record verification succeeded, which made it seem like everything was ready.

But verification only proves ownership—it doesn’t guarantee that traffic is routing correctly.

---

### Caching Made It Feel Random

Local DNS caching and browser caching added noise:

* One browser worked
* Another didn’t
* One device worked
* Another didn’t

It wasn’t random—it was just inconsistent state.

---

## What Actually Fixed It

Once the problem was simplified, the fix was straightforward:

### 1. Ensure DNS Points to the Correct Endpoint

* Remove any references to previous environments
* Point ALIAS / CNAME records to the current Static Web App

No overlap. No ambiguity.

---

### 2. Stop Changing Things Rapidly

It’s tempting to tweak multiple settings quickly:

* delete a record
* add a new one
* re-verify
* adjust again

That makes it harder to understand what’s happening.

Make one change at a time.

---

### 3. Wait (Longer Than You Think)

Even when everything looks correct:

* some clients still have cached data
* some DNS resolvers haven’t updated

“Mostly working” usually means you’re still in propagation.

---

### 4. Validate Outside Your Own Environment

Don’t rely on your machine alone.

Check from:

* another device
* another network
* a mobile connection

If it works everywhere, you’re done.

---

## Lessons Worth Keeping

A few takeaways that apply beyond this specific issue:

### Intermittent Success Is a Signal

If something works sometimes and fails other times, think **propagation or caching**, not configuration.

---

### DNS Is Eventually Consistent

Even correct configurations take time to converge globally.

---

### Azure Validation Isn’t End-to-End

Passing verification doesn’t mean your site is reachable in practice.

---

### Slow Down When Changing DNS

Multiple rapid changes create confusion and hide the real cause.

---

## Final Thought

You don’t need to fully understand how you got into this state to fix it.

In most cases, it comes down to one of two things:

* DNS is pointing to the wrong place
* DNS hasn’t finished catching up yet

And when your site works… except when it doesn’t—

> you’re probably closer to the answer than you think.
