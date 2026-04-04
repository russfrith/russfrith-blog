---
title: "Building a Quiet NAS for a Real Office (and Why I Chose TrueNAS)"
description: "How I’m Running My Office Infrastructure Like Something I’d Trust in Production"
date: 2026-03-31T13:33:33-04:00
draft: true
cover:
  image: william-warby-ykyUSsbDviA-unsplash-crop.jpg
  alt: "Hard Drive"
  caption: "Photo by [William Warby](https://unsplash.com/@wwarby?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/photos/a-close-up-of-a-hard-drive-with-a-pair-of-scissors-ykyUSsbDviA?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText)"
tags: ["truenas", "linux", "nas"]
categories: ["cloud"]
---

When people talk about building a NAS, the conversation usually goes one of two ways:

* A step-by-step install guide for TrueNAS
* Or a massive rack-mounted setup with more drives than most small businesses will ever need

Neither of those was what I was looking for.

I didn’t want a lab.
I didn’t want a science project.
And I definitely didn’t want something that sounded like a server room sitting five feet from my desk.

I wanted something simple:

> A reliable, quiet NAS that fits into a real office and just does its job.

This is how I approached that problem—and why I landed on TrueNAS.

---

## The Constraints (This Is Where It Starts)

Before looking at hardware or software, I had to be honest about the constraints.

This wasn’t going into a basement or a dedicated rack closet. It was going into a small office where I actually work every day.

That meant:

* **Noise matters** — constant fan noise gets old fast
* **Size matters** — I don’t have space for a full rack server
* **Power usage matters** — this runs 24/7
* **Reliability matters** — this is backing up real work, not media hoarding
* **Cost matters** — but not at the expense of stability

And maybe most importantly:

> I didn’t need “infinite scalability.” I needed “appropriate capacity.”

That framing alone rules out a lot of overbuilt solutions.

---

## Why Not Just Buy a Synology?

I seriously considered it.

Synology (and QNAP) are great for:

* Plug-and-play setups
* Low effort
* Clean UI

But I kept coming back to a few concerns:

* You’re locked into their ecosystem
* Hardware is often underpowered for the price
* You don’t really control the underlying system
* ZFS (which I wanted) isn’t part of the story

For some use cases, that tradeoff is fine.

For mine, I wanted something that felt a little closer to the metal—without becoming a full-time job to manage.

---

## Why Not Reuse Old Hardware?

This is the classic “free NAS” trap.

Take an old desktop, throw some drives in it, install something, and call it a day.

The problem is:

* It’s usually **louder**
* It’s often **less power efficient**
* It tends to become **less reliable over time**

I’ve done this before. It works—until it doesn’t.

This time, I wanted something intentionally built for the role.

---

Hardware: Right-Sized, Not Overbuilt

I ended up building around an Intel N5105-based motherboard in a compact NAS case.

That decision wasn’t about chasing performance—it was about practicality.

A big factor was:

I already had DDR4 SO-DIMMs on hand

That immediately lowered the cost of the build and made the N5105 platform a very natural fit. It’s easy to overlook things like existing parts, but they can (and should) influence your decisions.

Beyond that, the platform checked the boxes I cared about:

Low power draw
Quiet operation (no need for aggressive cooling)
Enough performance for storage and light services
Compact, purpose-built NAS boards available

For storage, I went with:

3 × WD Red Plus 6TB drives
Configured as RAIDZ1

Why 3 drives?

Because it hit the balance I was looking for:

Redundancy
Reasonable usable capacity
Lower cost and less noise than a larger array

Could I have gone bigger? Sure.

But again, the goal wasn’t to build the biggest NAS possible—it was to build the right one.

---

## The Case for Quiet

The case matters more than most people think.

I went with a **Jonsbo N1**, which is:

* Compact
* Clean looking
* Designed for NAS builds
* Capable of good airflow without excessive fan noise

Combined with:

* Fewer drives
* Efficient CPU
* No unnecessary components

…the result is something that fades into the background.

That was the goal.

---

## Why TrueNAS

Once hardware was settled, the OS decision became much easier.

I considered:

* Unraid
* Plain Linux + ZFS
* Even going back to something simpler

But TrueNAS hit the balance I wanted:

### 1. ZFS Without Compromise

ZFS was a requirement:

* Snapshots
* Data integrity
* Long-term reliability

TrueNAS treats ZFS as a first-class citizen.

---

### 2. A Real UI (Without Losing Control)

I didn’t want to manage everything via CLI.

But I also didn’t want a locked-down appliance.

TrueNAS sits in a good middle ground:

* Web UI for 90% of tasks
* CLI available when needed

---

### 3. Designed for This Exact Use Case

TrueNAS isn’t trying to be everything.

It’s very clearly designed to:

> Store data reliably.

That focus shows up in:

* Dataset management
* Snapshots
* Replication
* Backup workflows

---

## Setup: What Actually Mattered

There are plenty of guides on installing TrueNAS, so I won’t repeat that.

What *did* matter:

### Network Gotchas

My onboard NICs didn’t behave as expected during install.

This is one of those moments where:

> Having a simple USB Ethernet adapter saved a lot of time.

Once installed, everything worked—but it’s a good reminder that:

* Hardware compatibility still matters
* “Supported” doesn’t always mean “smooth”

---

### Pool Design

I kept things simple:

* `boot-pool` (system)
* `core-pool` (main RAIDZ1 storage)

There’s a temptation to over-engineer this early.

I’d recommend resisting that.

Start simple. Expand later.

---

### Dataset Structure

Instead of dumping everything into one dataset, I split by purpose:

* Backups (Proxmox)
* General storage
* Future expansion

This gives flexibility later without overcomplicating the initial setup.

---

## How It Fits Into My Stack

This NAS isn’t standalone.

It’s part of a broader setup:

* **Proxmox** running VMs and containers
* Backups flowing into TrueNAS
* Future plans for snapshots and replication

The goal is:

> Compute happens on Proxmox
> Storage lives on TrueNAS

Keeping those roles separate has already proven to be the right call.

---

## What I’d Do Differently

A few small things:

* I’d plan static IP assignments earlier
* I might use a smaller dedicated boot SSD (2TB is overkill)
* I’d document drive bay → serial mappings sooner

Nothing major—but these are the kinds of details that matter over time.

---

## Final Thoughts

There are a lot of ways to build a NAS.

Most of them fall into one of two categories:

* Overbuilt
* Or underthought

I was aiming for something in the middle.

Not flashy.
Not massive.
Not complicated.

Just:

> Reliable, quiet, and appropriate for the job.

And for that, TrueNAS turned out to be the right foundation.

---

If you’re building something similar, my advice is simple:

Start with your constraints.
Be honest about what you actually need.
And resist the urge to build for a future that may never come.

That alone will get you 90% of the way there.
