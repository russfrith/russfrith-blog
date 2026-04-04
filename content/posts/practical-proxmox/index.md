---
title: "Practical Proxmox"
description: "How I’m Running My Office Infrastructure Like Something I’d Trust in Production"
date: 2026-03-30T13:33:33-04:00
draft: true
cover:
  image: lukas-NLSXFjl_nhc-unsplash.jpg
  alt: "Debian Linux"
  caption: "Photo by [Lukas](https://unsplash.com/@lukash) on [Unsplash](https://unsplash.com/photos/a-computer-screen-with-a-program-running-on-it-NLSXFjl_nhc)"
tags: ["proxmox", "linux"]
categories: ["cloud"]
---

Proxmox gets significant attention in home lab circles, and I get why. However, my use case pushed past hobby territory into services that are genuinely important to my small business and personal life.

Once the stakes changed, so did the approach. Something you're willing to let break overnight is very different from something you need to actually work. That meant building with intention: defined structure, real recovery paths, and a level of trust you don't usually demand from a weekend project.

It's not about building the flashiest setup. It's about applying production thinking at personal scale, without pretending I have an enterprise budget to back it up.

## The Goal: Treat It Like Production (Without the Budget)

I don’t want a toy.

I want something that:

* Survives mistakes
* Recovers quickly
* Mirrors real-world architecture decisions
* Doesn’t require babysitting

That means fewer “cool experiments” and more boring, intentional structure.

Proxmox is the foundation, but it’s not the strategy. It’s just the tool that lets the strategy work.

## The Core Philosophy: Separation Over Convenience

The biggest mistake in most home labs is collapsing everything into one flat network and one giant VM host.

It works… until it doesn’t.

I’ve moved toward clear separation:

* **Management network** – where Proxmox, switches, and controllers live
* **Server network** – where actual workloads run
* **User network** – normal devices
* **IoT / Guest / Camera networks** – isolated and restricted

This mirrors how real environments are structured, and more importantly, it forces you to think about access, routing, and failure boundaries.

If everything can talk to everything, you haven’t designed anything—you’ve just delayed the problem.

## Proxmox: The Control Plane, Not the Junk Drawer

Proxmox is not where I dump random services anymore.

It’s the control plane.

That distinction matters.

Instead of treating it like a place to “run stuff,” I treat it like:

* A hypervisor with clean networking boundaries
* A predictable host with minimal drift
* A system that should almost never change

Most of the actual work happens inside LXCs and VMs that can be destroyed and rebuilt.

If I hesitate to delete something, that’s a smell. In a real environment, rebuildability matters more than preservation.

## Containers vs VMs: Be Intentional

There’s a temptation to default everything to Docker.

I don’t.

* **LXCs** for lightweight, single-purpose services
* **VMs** when isolation actually matters
* **Docker** inside LXCs when it simplifies deployment

That last one is controversial, but it works well if you’re disciplined.

The key is not the tool—it’s consistency.

If every service is deployed differently, you’re not building flexibility—you’re building future confusion.

## Storage: Backups Are the Product

The most important part of the entire setup isn’t compute.

It’s storage and backups.

Everything else is replaceable.

The strategy:

* **Primary workloads** run on Proxmox
* **Backups** go to a separate storage system
* **Snapshots** are used for short-term recovery
* **Backups** are used for “oh no” moments

There’s a big difference between:

> “I can roll back a bad update”

and

> “I can rebuild everything after losing a node”

If you don’t have the second one, you don’t have a system—you have something that works right up until it doesn’t.

## Naming and IP Strategy: Future You Matters

One of the most underrated improvements is consistency in naming and IP allocation.

Not because it looks nice.

Because it reduces thinking when something goes wrong.

* Gateways are always `.1`
* Core services live in predictable ranges
* Management IPs are easy to recognize
* Temporary systems don’t collide with permanent ones

When something breaks, you shouldn’t have to guess what `10.101.200.73` is.

You should already know whether it belongs there.

## Networking: Where Things Actually Break

Most of the real work has been in networking, not Proxmox.

VLANs, firewall rules, DNS, NAT—this is where the complexity lives.

And this is also where most guides fall short.

A few lessons:

* Direction matters more than you think (`in` vs `out` rules will get you)
* DHCP and DNS are the first things to check, not the last
* “It should work” is not a debugging strategy

The moment things started behaving like a segmented network instead of a flat LAN, everything became more predictable—even if it required more upfront thought.

## The Stack: Build Slowly, Not Broadly

There’s a long list of services I could run.

That doesn’t mean I should run them all at once.

The current approach is simple:

1. Get the foundation stable
2. Add one service at a time
3. Make it observable (logs, uptime, access)
4. Make it recoverable
5. Then move on

It’s tempting to spin up ten containers in a weekend.

It’s better to have three that you actually understand and trust.

## Cloudflare + Reverse Proxy: One Door In

Externally, everything comes through a single path:

* Cloudflare Tunnel
* Reverse proxy routing internally

No exposed ports. No one-off configurations per app.

If a service needs to be public, it goes through the same entry point as everything else.

That’s not just cleaner—it’s how you avoid creating invisible risk over time.

## What This Actually Enables

This setup isn’t just for fun.

It supports:

* Development environments that behave like production
* Internal tools that don’t rely on third-party services
* Experiments that don’t risk everything else
* A platform for building real products

That last one is the point.

If your environment doesn’t make it easier to build or run something useful, it’s just expensive entertainment.

## Final Thought

A good setup isn’t about how much you run.

It’s about how confidently you can change it.

If you can tear something down and bring it back cleanly, you’re doing it right.

If you’re afraid to touch it, you’ve already limited what it can become.

---

Focus less on tools and more on structure.

The tools will change.

The structure is what makes everything else work.
