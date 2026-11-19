---
title: "Is Bicep as Strong as Terraform? Starting Without a State File"
description: "A Terraform user starts a new Azure project in Bicep and reports back on what they're giving up."
date: 2026-10-30T10:00:00-04:00
draft: true
cover:
  image: schaferle-man-2037255_1920.jpg
  alt: "A man in a hoodie standing between two chalk-drawn flexed-arm muscles"
  caption: "Image by [Elias](https://pixabay.com/users/sch%C3%A4ferle-3372715/?utm_source=link-attribution&utm_medium=referral&utm_campaign=image&utm_content=2037255) from [Pixabay](https://pixabay.com//?utm_source=link-attribution&utm_medium=referral&utm_campaign=image&utm_content=2037255)"
tags: ["azure", "bicep", "terraform", "iac"]
categories: ["cloud"]
---

Terraform has been my default for infrastructure as code for years, across clouds, across teams. So when I started a new Azure-only project recently, the obvious move was to reach for Terraform again. Instead, I'm trying Bicep, and I want to write down why, in public, before I know how it turns out.

I want to be clear that this isn't a case of abandoning a tool I dislike. I like Terraform. My favorite thing about it is the thing I'm walking away from for this project: it works across clouds. One language, one workflow, whether I'm standing up resources in Azure, AWS, or somewhere else. That portability is genuinely valuable, and giving it up isn't nothing. But this project is Azure-only, start to finish, so for once that strength doesn't actually buy me anything.

There's a smaller, more personal factor too: file format. I like that Terraform has one consistent syntax, HCL, across every provider and every resource type. I never have to context-switch between different configuration styles depending on what I'm deploying. That said, I don't love HCL itself, especially once files get large: the nested blocks and repetition can get hard to scan at a glance. It's not what's driving this change, the state repository is, but it's part of what made trying something else, Bicep's own DSL, an easier call to make.

This is a greenfield project: nothing existing to migrate, no legacy state to inherit, no team convention I'm bound to. That's the only reason this experiment is even possible. Most of the comparisons and migration advice I've found point the other way, people moving *from* Bicep *to* Terraform, usually once they need multi-cloud support or a more mature module ecosystem. I'm going to acknowledge that and then ignore it, because my situation is narrower: one cloud, one new project, and a specific thing I want to avoid.

So, is Bicep "as strong as" Terraform? Too early to say. But to even answer that question later, I need to define what "strong" means here, because it's not one thing. I'm watching:

- **Safety of changes** - how confidently can I tell, before applying, what a change will actually do?
- **Cleanup** - when I remove a resource from my templates, what happens to it in Azure?
- **Ecosystem** - providers, modules, examples, community answers when something breaks.
- **Day-to-day ergonomics** - the stuff I'll feel every single day: syntax, tooling, editor support, how fast the feedback loop is.

This post is the starting point. The follow-up will be the scorecard.

## The reason: no state repository

The real reason I'm trying Bicep isn't philosophical. It's operational: I don't want to run a state repository.

With Terraform, before I write a single resource block, I first need somewhere for state to live. For a solo or small-team Azure project, that typically means, illustratively (I haven't run through this for this project):

- An Azure Storage Account and container to hold the `.tfstate` file.
- Access configured so whoever (or whatever CI) runs `terraform apply` can read and write that container.
- State locking, so two applies can't stomp on each other.
- A plan for what happens when a lock gets stuck, because eventually one does.

None of that is hard, exactly. I've set it up before, more than once. But it's bootstrap work that exists purely to support the tool, not the thing I'm actually trying to deploy. And it's one more piece of infrastructure, with its own permissions and its own failure modes, that I now have to maintain for the life of the project.

Bicep doesn't ask for any of that. It compiles down to ARM templates and deploys straight against Azure Resource Manager. There's no `.tfstate` file for me to store, back up, or lock.

<!-- VERIFY: "no state file" claim - confirm current Microsoft docs on how Bicep/ARM deployments are tracked, and whether this has changed recently -->

To be precise about what that does and doesn't mean: there's no state file *that I manage*. Azure Resource Manager still tracks every deployment, what was deployed, when, and with what template, under the hood. I'm not avoiding state. I'm avoiding being the one responsible for it.

## What I'm taking on

Nothing is free. Here's what I'm trading away, as open questions I genuinely don't know the answer to yet:

- **Resource deletion behavior.** Terraform's model is explicit: if a resource leaves your config, `terraform plan` tells you it's going to destroy it, and you approve that before it happens. Bicep has at least three different deployment modes I know of by name, incremental mode, complete mode, and deployment stacks, and I don't yet know how each one handles a resource that's no longer in the template. Does it get deleted automatically? Left alone? Something in between?

  <!-- VERIFY: incremental vs. complete mode vs. deployment stacks deletion behavior - confirm against current Microsoft Bicep/ARM docs -->

- **Drift detection.** If someone changes a resource by hand in the portal, how does Bicep tell me? Terraform's drift story, while not perfect, is at least a known quantity to me.

- **What-if reliability.** Bicep has `az deployment ... what-if` as its answer to `terraform plan`. I don't know yet how trustworthy it is, whether it reliably catches everything a real deployment would do, or whether it has known blind spots.

- **Syntax at scale.** I like HCL less as files get larger. Whether Bicep's syntax genuinely holds up better once a template grows, or I just end up trading one set of large-file pain points for another, I don't know yet.

I'm starting the project not knowing the answers to any of these. That's the point of writing this down now, before experience smooths over what I was actually worried about.

## A first Bicep deployment

<!-- TODO(notes): a real, actually-run Bicep deployment from this project - what it deployed, what command was used, what happened. Should answer: what does day one with Bicep actually look like in practice? -->

## What I'll report back on

The follow-up to this post will try to answer the questions I've raised here, with actual experience behind them instead of guesses:

- What actually happens when I remove a resource from a Bicep template and redeploy, under whichever mode I end up using?
- Did I ever catch drift, and how?
- Did `what-if` ever lie to me, or miss something a real deployment then did?
- Weighed against not running a state repository, was any of that worth it?

Until then, this is a journey post, not a verdict. I'm not arguing Bicep is "as strong as" Terraform. I'm telling you why I'm betting on it for this project, and exactly what I'm watching to find out if that bet pays off.
