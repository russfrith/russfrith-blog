---
title: "Linux for Windows Developers"
description: "A practical mental model and reference for Windows developers running Linux, whether that's a server you administer yourself, a daily-driver workstation, or WSL: permissions, SSH hardening, services, logs, and the commands that translate."
date: 2026-11-17T09:00:00-05:00
draft: true
cover:
  image: pexels-realtoughcandy-11034131.jpg
  alt: "A sticker of the Linux penguin mascot, Tux, lying on a blue ice cube tray."
  caption: "Photo by [RealToughCandy.com](https://www.pexels.com/@realtoughcandy/) on [Pexels](https://www.pexels.com/photo/linux-tux-penguin-mascot-cutout-lying-on-a-tray-with-ice-cubes-11034131/)"
ShowToc: true
tags: ["linux", "windows"]
categories: ["cloud"]
---

Here's a version of a mistake that plays out constantly: a deploy works fine locally, then fails on the server with a file-not-found error for a config file that very clearly exists. It turns out the code asks for `Config.json`, the file on disk is `config.json`, and whatever Windows machine the project was developed on never noticed the mismatch, because Windows treats those two names as the same file. The Linux server doesn't. It takes the capital letter seriously, sees no match, and fails. Nothing is broken. The two systems just disagree about something developers rarely have to think about on Windows at all.

[AUTHOR: insert a personal story here]

If any of that sounds familiar, the problem usually isn't that you don't know how to code. It's that nobody ever handed you the model of how Linux is put together, because Windows and Linux make different design choices about almost everything below the application layer: where configuration lives, who owns a file, how a service starts, where the logs go. That model holds whether you're administering a server you're responsible for, running Linux as your day-to-day development machine, or working inside WSL on a Windows box. This post covers all three: the shared mental model and reference material first, then a dedicated section on the server-specific defaults you need to run one safely, and another on using Linux as a workstation, including WSL. It's long on purpose: treat it as something to read once and then come back to, section by section, whenever you need it.

## If you only do five things

These five matter most once you're responsible for a server reachable from the internet. If you're only using Linux as a local workstation or inside WSL, skim them anyway, the underlying ideas (keys over passwords, least privilege, not reaching for `chmod 777`) are good habits everywhere, even if the exact commands (a firewall, in particular) matter less on a machine that isn't exposed:

- **Use SSH keys, not passwords.** Generate a key pair, install the public key on the server, then turn password login off.
- **Create a non-root user with `sudo`.** Don't work as root day to day.
- **Put up a firewall with default deny.** Block everything inbound by default, then open only the ports you actually use.
- **Turn on automatic security updates.** Don't rely on remembering to patch.
- **Set correct ownership and permissions.** Never reach for `chmod 777` as a fix; it almost always means something else is wrong.

Each of these gets its own detailed walkthrough further down. If you do nothing else on this page, do these.

## The Windows-to-Linux mental model

Re-orientation: this section is the map for everything else in this post. If a later section mentions `/etc` or "owner, group, other" and it doesn't click, come back here.

Windows and Linux aren't just different operating systems with different icons. They organize the same underlying ideas, storage, configuration, permissions, software installation, differently enough that assumptions you've built up over years of Windows use will quietly mislead you. None of this makes Windows wrong. It made different design choices, mostly rooted in a different history (Unix in the 1970s vs. DOS and the VMS-influenced NT kernel). Here's the map:

- **One tree, not drive letters.** Windows has `C:\`, `D:\`, maybe a mapped `Z:\`. Linux has a single tree rooted at `/`, and every other disk, partition, or network share gets mounted *into* that tree at some directory, like `/mnt/data` or `/srv`. There's no letter to remember; you just know (or look up) which path a disk lives at.
- **No registry.** Windows centralizes a huge amount of system and application configuration in a single binary database, the registry. Linux doesn't have an equivalent. Configuration lives in plain text files, mostly under `/etc` for system-wide settings and in dotfiles (files starting with `.`) in each user's home directory for personal settings. You can `cat` them, diff them, and put them in Git.
- **Case-sensitive names.** `Config.json` and `config.json` are two different files on most Linux filesystems. Windows (via NTFS, in its default configuration) treats them as the same file. This single difference causes more "works on my machine" deploy failures than almost anything else on this list.
- **`/` as the path separator, not `\`.** Linux shells and most Linux software use forward slashes. Backslash is often an escape character in a Linux shell, so a Windows-style path pasted into a Linux terminal doesn't just fail to resolve, it can get partially interpreted as shell syntax.
- **Owner, group, other instead of per-file ACLs as the default.** Windows' native permission model (NTFS ACLs) lets you grant arbitrary sets of permissions to arbitrary users and groups on each file. Linux's traditional model is simpler: every file has exactly one owner, one group, and a set of permissions for "owner," "group," and "everyone else." (Linux also supports ACLs for when the simple model isn't enough; more in the permissions section.)
- **Package managers instead of installers.** On Windows you mostly download and run installers, one per application, each bringing its own copies of shared libraries. On Linux, you install software through a package manager (`apt` on Debian/Ubuntu, `dnf` on Red Hat-family systems) that resolves shared dependencies for you and knows how to cleanly remove what it installed.
- **SSH as the normal front door.** You don't remote-desktop into a Linux server to click around; you open a terminal and SSH in. Everything in this post, right down to "how do I know if the service is running," assumes you're doing it from a shell, because that's genuinely the native way to operate a Linux server.

Keep this list in your head as you read the rest of this post. Almost every section below is really just one of these ideas worked out in more detail.

Worth a word on "plain text" before moving on, because it's easy to underestimate how far it goes. A typical Windows application's settings might live partly in the registry, partly in a proprietary `.config` format, partly in an installer's own state. On Linux, the default assumption is a text file you can open in any editor, `cat` on a remote session with no GUI at all, and diff against yesterday's version. Your own personal settings, shell aliases, editor preferences, Git configuration, follow the same pattern as dotfiles in your home directory (`~/.bashrc`, `~/.gitconfig`, and so on). This is also why it's common to keep a personal dotfiles repository in Git: there's nothing special about the files that would stop you.

## POSIX in plain terms

Re-orientation: POSIX comes up constantly in Linux documentation and in "why doesn't this work on macOS" bug reports. Here's what it actually is and isn't.

POSIX (Portable Operating System Interface) is a family of standards, maintained jointly by the IEEE, The Open Group, and ISO/IEC, that defines a common interface for Unix-like operating systems. <!-- TODO verify: exact current maintaining bodies and edition name/year --> It specifies things like:

- A set of system calls (the low-level functions a program uses to ask the kernel to open a file, fork a process, and so on).
- The syntax and behavior of the shell command language (the `sh` POSIX shell, which `bash` is a superset of).
- A baseline set of command-line utilities (`ls`, `grep`, `sed`, `awk`, and dozens more) and what options and behavior they're required to support.

Why should a developer care? Because POSIX is the reason skills transfer. A shell script, a set of file permission assumptions, and muscle memory with core utilities built on a Debian server mostly carry over to a Red Hat server, a FreeBSD box, and macOS's Terminal, because all of them target the same baseline interface. That's a genuinely useful property: it's part of why "just SSH in and use the shell" works as a universal skill across so many different systems.

What POSIX does *not* cover is just as important:

- **Package managers.** `apt`, `dnf`, `pacman`, `brew`: none of this is standardized. Every distribution and OS does packaging its own way.
- **Init and service management.** How a system boots, starts services, and supervises them (systemd, which most modern Linux distributions use, is not a POSIX concept at all).
- **GNU extensions.** Most Linux systems ship the GNU versions of core utilities (GNU coreutils, GNU `sed`, GNU `awk`), and GNU adds options and behaviors beyond what POSIX requires. Those extensions work great right up until you run the same script against a non-GNU system.

It's also worth being precise about compliance: most Linux distributions are **largely POSIX-compatible**, not formally POSIX-certified. Formal certification is a specific, paid process that very few systems bother with. <!-- TODO verify: which systems, if any, currently hold formal POSIX certification --> And Windows, outside of things like WSL (covered later), is not a POSIX system; `cmd.exe` and PowerShell don't implement the POSIX shell language or utility behavior, by design.

Here's where the GNU-vs-POSIX gap bites people in practice. This works on Ubuntu or Debian:

```bash
sed -i 's/foo/bar/' config.txt
```

GNU `sed`'s `-i` flag can take an in-place edit with no backup suffix, as shown above. But Alpine Linux (common in minimal Docker images) ships BusyBox's `sed`, and macOS ships BSD `sed`. BSD `sed` requires an explicit (even if empty) backup suffix argument to `-i`:

```bash
# BSD sed (macOS) requires an argument to -i, even if it's empty
sed -i '' 's/foo/bar/' config.txt
```

Run the GNU-style version unmodified on macOS and you'll either get an error or, worse, `sed` will silently treat the next argument as the backup suffix and mangle your command. This is the "portable vs. GNU-only" distinction worth internalizing: a command that works on your Ubuntu server isn't guaranteed to work in a Docker build stage based on Alpine, or on a teammate's Mac. When you need something that's genuinely portable, stick to documented POSIX behavior, test on more than one system, or just pin the one environment you actually run in and stop worrying about the rest.

The shell language itself is part of the same story. The POSIX shell (`sh`) defines a baseline syntax for conditionals, loops, functions, and parameter expansion that `bash`, `dash`, and `zsh` all build on. A script that sticks to that baseline, `#!/bin/sh` instead of `#!/bin/bash`, no arrays, no `[[ ... ]]` (a `bash`-specific extension of the plain `[ ... ]` test), runs on practically anything with a shell. A script that leans on `bash`-only features runs fine until someone invokes it with `sh script.sh` on a system where `sh` is actually `dash` (true by default on Debian and Ubuntu), at which point those features simply aren't there.

## Everything is a file

Re-orientation: this is a slogan you'll hear constantly in Linux circles, here's what it concretely buys you.

On Linux, an enormous range of things, regular files, directories, hardware devices, and many kernel and process interfaces, are represented as files and manipulated with the same small set of operations: open, read, write, close. A serial port might show up as `/dev/ttyUSB0`. Process information lives under `/proc/<pid>/`. You can `cat` a lot of these and get readable output. This uniformity is what makes the next few ideas possible.

**Standard streams.** Every process gets three open file-like streams by default:

- **stdin** (standard input): where input normally comes from, usually your keyboard.
- **stdout** (standard output): where normal output goes.
- **stderr** (standard error): where error and diagnostic output goes, kept separate from stdout on purpose.

**Redirection** lets you point these somewhere other than the terminal:

```bash
# send stdout to a file, overwriting it
echo "hello" > output.txt

# append stdout to a file instead of overwriting
echo "more" >> output.txt

# send stderr to a file (note: no space after the 2)
some-command 2> errors.txt
```

**Pipes** connect one process's stdout directly to another process's stdin, with the `|` character, so you can build small tools into bigger ones without writing a line of glue code:

```bash
# list files, then filter the listing down to lines containing ".log"
ls -l /var/log | grep ".log"
```

**Exit codes.** Every command returns a numeric exit code when it finishes: `0` means success, anything nonzero means some kind of failure (the specific nonzero value is command-defined). Scripts use this constantly, `command1 && command2` only runs the second command if the first exited `0`.

> **Windows note:** PowerShell deliberately broke from this model in one important way. Where a Linux pipe passes raw text between commands, a PowerShell pipeline passes structured .NET objects, so `Get-Process | Where-Object CPU -gt 10` is filtering actual object properties, not grepping text output. Both approaches are reasonable; they just optimize for different things, text streams are simpler and universal, objects are richer and more precise within PowerShell itself.

## Where things live

Re-orientation: a quick-reference map of the standard directories, useful any time you're asked "where did that config file go" or "where should this live."

Most Linux distributions follow something close to the [Filesystem Hierarchy Standard (FHS)](https://refspecs.linuxfoundation.org/fhs.shtml), though distributions vary at the edges and not everything is strictly enforced.

| Directory | What lives there | Closest Windows idea |
|---|---|---|
| `/etc` | System-wide configuration, in plain text files | `HKEY_LOCAL_MACHINE` registry hive, roughly, but readable and version-controllable |
| `/var/log` | Log files for the system and most services | Event Viewer logs, but as plain files |
| `/home` | Per-user home directories (`/home/russ`) | `C:\Users\russ` |
| `/usr` | Installed software and its supporting files, managed by the package manager | `C:\Program Files`, loosely |
| `/bin`, `/usr/bin` | Core executable programs | `C:\Windows\System32`, loosely |
| `/opt` | Self-contained, manually installed third-party software that doesn't fit the package manager's layout | A manually unzipped application folder |
| `/tmp` | Temporary files, often cleared on reboot | `%TEMP%` |
| `/srv` or `/var/www` | Data served by this host (website files, for example) | `C:\inetpub\wwwroot`, loosely |

A few things worth internalizing from this table:

- `/etc` being plain text is a genuine superpower. You can put `/etc` configuration under Git version control (some sysadmins literally `git init` the whole directory) and get real diffs when something changes.
- Where an application *runs* from and where it *stores its data* are usually different directories. The binary might live under `/usr/bin` or `/opt`, while its working data lives under `/var/lib/<app>` or wherever you've pointed it. Don't assume colocating them, the way a self-contained Windows application folder often does, is how Linux software is laid out.
- Distributions disagree at the edges: some put web content under `/var/www`, others expect you to configure `/srv` yourself. When in doubt, check the specific package's documentation rather than assuming FHS is gospel.

A couple of directories that look similar but aren't worth confusing: `/bin` and `/usr/bin` historically held different things (`/bin` for binaries needed before `/usr` was mounted, `/usr/bin` for everything else), but on most current distributions `/bin` is simply a symlink to `/usr/bin`, so in practice you can treat them as one. `/opt`, meanwhile, exists specifically for software that doesn't play by the package manager's rules, a vendor's self-contained bundle, for instance, so finding something installed there isn't a sign of a mistake, just a sign it was installed outside `apt` or `dnf`.

## Users and permissions

Re-orientation: this is the section that answers "why can't I read/write/run this file," and why `chmod 777` is a mistake rather than a fix.

Every permission check on Linux boils down to the same three questions, [a framing I first saw laid out clearly in this piece on file permissions](https://www.xda-developers.com/linux-file-permissions-confused-until-realized-answering-three-questions/), though the explanation and examples below are my own:

1. **Who is asking?** The owner of the file, a member of the file's group, or everyone else ("other").
2. **What do they want to do?** Read, write, or execute.
3. **What are they acting on?** A file, or a directory (execute means something different for each).

### Reading `ls -l`

```bash
$ ls -l deploy.sh
-rwxr-xr-- 1 russ developers 812 Nov 10 09:14 deploy.sh
```

Break down `-rwxr-xr--` into four pieces:

- `-` : file type (`-` for a regular file, `d` for a directory, `l` for a symlink)
- `rwx` : owner's permissions (read, write, execute)
- `r-x` : group's permissions (read, execute, no write)
- `r--` : everyone else's permissions (read only)

So `russ` (the owner) can read, write, and execute this file. Anyone in the `developers` group can read and execute it but not modify it. Everyone else can only read it.

### Directories are different

For a directory, the same three letters mean something slightly different:

- **Read** on a directory lets you list the names of the things inside it (`ls` the directory).
- **Execute** on a directory lets you *traverse into* it, meaning use it as part of a path to reach something inside, whether or not you can list its contents.

This is precisely why a directory with permissions `--x` (execute only, no read) still works: you can `cd` into it, and you can open `/that/dir/specific-file.txt` if you already know the filename, you just can't `ls` and discover what's in there. And it's why permissions on a path matter at every level: to open `/home/russ/project/config.json`, you need execute (traverse) permission on `/home`, on `/home/russ`, and on `/home/russ/project`, not just read permission on the file itself. One overly locked-down directory anywhere along the path breaks access to everything beneath it, which is a common source of "permission denied" errors that look like they're about the file when they're really about a parent directory.

### Octal as addition

Permissions are often written as a three-digit octal number instead of the symbolic `rwx` form, because it's faster to type. Each permission has a fixed value: read = 4, write = 2, execute = 1. Add up the ones you want for each of owner, group, and other:

- `640` = owner gets `6` (4 + 2 = read + write), group gets `4` (read only), other gets `0` (nothing). A sensible default for a config file with secrets: you can edit it, your group can read it, nobody else can touch it.
- `755` = owner gets `7` (4 + 2 + 1 = read + write + execute), group gets `5` (4 + 1 = read + execute), other gets `5` (read + execute). A typical permission set for an executable script or a directory everyone needs to traverse and list.

### Prefer symbolic `chmod` over `chmod 777`

You can also change permissions symbolically, adding or removing a specific permission for a specific category without touching the others:

```bash
# add write permission for the group, leave everything else alone
chmod g+w deploy.sh

# remove execute permission from "other"
chmod o-x deploy.sh
```

`chmod 777` sets read, write, *and* execute for owner, group, and everyone else, on everything. It's the permissions equivalent of giving up: if something is failing because a web server process can't read a file, the fix is to find out *why* that process can't read it (wrong owner? wrong group? a parent directory with no execute bit for that user?) and grant exactly the permission that's missing, not to throw every permission open to every user on the system. `777` on a file reachable from the internet, even briefly, is a real, not theoretical, way to get a server compromised.

Before changing any mode, check two things: who actually owns the file (`ls -l`, or `stat filename` for more detail), and whether the problem is really about this file or about a directory earlier in the path. Changing a mode you don't understand the cause of tends to just move the symptom somewhere else.

### Run things as themselves

A habit worth building early: run each application under its own unprivileged service account rather than as root or as your personal login user. A web server that only needs to read its own files and write to its own log directory should run as a dedicated `www-data`-style account that owns exactly those files, nothing more. If that process is ever compromised, the blast radius is limited to what that one account can touch.

For tasks that genuinely need elevated privileges, Linux's answer isn't "log in as root," it's `sudo`: you run a specific command with temporary elevated privileges, under your own login, and the system logs that you did it. Reach for `sudo <command>` for the occasional privileged action, not for working as root all day.

### New files don't default to wide open

One detail that trips people up when they're first checking a file's permissions: a newly created file is never `777` by default, even though the application that created it might ask for every permission. The kernel applies a **umask**, a mask that subtracts permissions from whatever the creating process requested, and most distributions default to a umask that turns a requested `666` (read/write for everyone) into `644` (owner can write, everyone else can only read) for files, and a requested `777` into `755` for directories. You can see your current umask with `umask`, and change it (per-session, or in a shell startup file for something more permanent) with `umask 022`. It's one more reason the honest fix for a "permission denied" error is almost never "open everything up": the system already deliberately narrows new permissions, and widening them back out undoes a safety default, not a bug.

### One sentence, and a pointer, for the rest

A few related mechanisms exist for cases the owner/group/other model doesn't cover well: **ACLs** let you grant permissions to additional specific users or groups beyond just owner and group (see `man acl` and `setfacl`); **setuid** and the **sticky bit** are special permission bits that change how execution or deletion works in specific cases (see `man chmod`); **umask** controls the default permissions newly created files get (see `man umask`); and **SELinux** (common on Red Hat-family systems) adds a separate, mandatory-access-control layer on top of all of this (see your distribution's SELinux documentation). Each is worth understanding once you hit a case the basics don't cover; none of them are needed to get a typical server running safely.

## Getting in and staying safe

Re-orientation: how to lock down remote access to a fresh server without locking yourself out. This section and the next are written for a server you're exposing to the network; skip ahead to the workstation section if that's not what you're setting up today.

### SSH keys instead of passwords

Generate a key pair on your own machine (not the server):

```bash
ssh-keygen -t ed25519 -C "your-email@example.com"
```

This creates a private key (keep it secret, never copy it anywhere) and a public key (`.pub`), which is safe to share and is what you install on servers. Copy the public key to the server:

```bash
ssh-copy-id user@your-server
```

`ssh-copy-id` appends your public key to `~/.ssh/authorized_keys` on the server for that user. Test that key-based login works (`ssh user@your-server`) before you touch anything else.

`ed25519` (used above) is a modern, fast, and currently recommended key type; you'll still see older guides recommend `rsa` with a longer bit length, which still works but produces larger keys for no real security benefit on a system that supports `ed25519`. <!-- TODO verify: current OpenSSH-recommended key type and any deprecation timeline for older types --> Whichever type you use, the private key never leaves your machine, and if you're asked to set a passphrase on it during generation, do, it's a second factor protecting the key file itself if your laptop is ever lost or stolen.

### Turn off password login, carefully

Once key-based login works, edit `/etc/ssh/sshd_config` on the server:

```
PasswordAuthentication no
PermitRootLogin no
```

`PasswordAuthentication no` disables logging in with a password at all, forcing key-based auth. `PermitRootLogin no` disables logging in as `root` over SSH entirely, you log in as your own user and `sudo` from there. <!-- TODO verify: exact default values and accepted value set for PermitRootLogin on current OpenSSH -->

**Before you restart the SSH service, keep your current session open.** Open a second terminal, SSH in fresh with the new configuration, and confirm it works, *while your first session is still connected*. If the new configuration is broken, you still have a way in to fix it through the still-open first session. Only close that first session once the second one has proven the new configuration works. This single habit has saved more people from locking themselves out of their own server than anything else in this post.

After confirming, apply the change:

```bash
sudo systemctl restart sshd
```

### A default-deny firewall

The quick-start checklist calls for default deny: block everything inbound by default, then explicitly allow only what you need (typically SSH, and whatever ports your application actually serves).

On Debian/Ubuntu, `ufw` ("Uncomplicated Firewall") is the standard front end:

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw enable
```

Allow SSH *before* you enable the firewall, or you'll cut off your own remote session with no way back in short of console access.

On Red Hat-family systems, the equivalent is `firewalld`:

```bash
sudo systemctl enable --now firewalld
sudo firewall-cmd --permanent --add-service=ssh
sudo firewall-cmd --reload
```

`firewalld` defaults new interfaces to a `public` zone with a restrictive baseline, and changes made with `--permanent` only take effect after a `--reload` (or a restart). <!-- TODO verify: current default zone behavior and exact service name strings across supported RHEL/Fedora versions -->

### Automatic security updates

Don't rely on remembering to patch. On Debian/Ubuntu:

```bash
sudo apt install unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

This installs and enables `unattended-upgrades`, which by default checks for and installs security updates on a schedule, configurable in `/etc/apt/apt.conf.d/50unattended-upgrades`.

On Red Hat-family systems, the equivalent package is `dnf-automatic`:

```bash
sudo dnf install dnf-automatic
sudo systemctl enable --now dnf-automatic-install.timer
```

Configuration lives in `/etc/dnf/automatic.conf`; separate timers exist for downloading only, downloading and installing, or just reporting what's available, so pick the timer that matches how hands-off you want this to be. <!-- TODO verify: current default package/timer names and whether "install" timer applies all updates or security-only by default -->

### See what's actually listening

```bash
ss -tlnp
```

This lists TCP (`-t`) listening (`-l`) sockets, numeric (`-n`, skips slow DNS/service-name lookups), with the owning process (`-p`, needs `sudo` to see other users' processes). Run this after any change to confirm only the ports you intend to expose are actually open, and tie unexpected ones back to the process that opened them.

> **Windows note:** this whole section maps to things you already manage on Windows, just through different tools. SSH key auth and `PasswordAuthentication no` are conceptually close to disabling weak RDP auth in favor of certificate or multi-factor login. `ufw`/`firewalld` default-deny is the same idea as Windows Defender Firewall's inbound rules, just configured from a shell instead of a GUI, and usually deny-by-default from the start rather than needing to be switched into that mode. `unattended-upgrades`/`dnf-automatic` are the direct equivalent of Windows Update doing its thing in the background.

## Running things: processes, services, and logs

Re-orientation: how to answer "is my app running, and if not, why." This applies equally on a server and on a Linux workstation or WSL, since both use the same process model and, in most cases, the same systemd.

### Looking at processes

```bash
ps aux
```

Lists every process on the system with its owning user, CPU and memory usage, and the command that started it. For an interactive, continuously updating view, use `top`, or the friendlier `htop` if it's installed:

```bash
htop
```

### Stopping a process

```bash
kill <pid>        # sends SIGTERM: "please shut down cleanly"
kill -9 <pid>     # sends SIGKILL: "stop immediately, no cleanup"
```

Always try a plain `kill` (SIGTERM) first. It asks the process to shut down on its own terms, flush buffers, close files, run cleanup code. `kill -9` (SIGKILL) can't be caught or ignored by the process at all; the kernel just stops it. Reach for `-9` only when a process is genuinely stuck and ignoring a plain `kill`.

This is a direct parallel to closing a Windows application: closing it normally (SIGTERM) lets it save state and release resources, while killing it from Task Manager's "End Task" (closer to SIGKILL) skips all of that. The Linux version just makes the distinction explicit and scriptable, rather than being two different buttons in a GUI.

### Services, the systemd way

Most modern Linux distributions use `systemd` to start, stop, and supervise long-running services:

```bash
systemctl status nginx     # is it running, and recent log lines
sudo systemctl start nginx
sudo systemctl stop nginx
sudo systemctl restart nginx
sudo systemctl enable nginx   # start automatically on boot
```

To run your own small application as a service, write a unit file, typically at `/etc/systemd/system/myapp.service`:

```ini
[Unit]
Description=My small app
After=network.target

[Service]
ExecStart=/usr/bin/python3 /opt/myapp/app.py
Restart=on-failure
User=myapp

[Install]
WantedBy=multi-user.target
```

`After=network.target` says don't start until networking is up. `Restart=on-failure` asks systemd to restart the process if it crashes. `User=myapp` runs it as a dedicated unprivileged account, rather than root, matching the advice earlier about service accounts. After adding or editing a unit file:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now myapp
```

<!-- TODO verify: minimal required unit file fields and current recommended defaults on a current systemd version -->

### Watching logs live

```bash
journalctl -u myapp -f
```

Follows (`-f`) the systemd journal for just that unit (`-u myapp`) in real time, the direct equivalent of `tail -f` for a service's logs. Not every piece of software logs through the systemd journal, though; plenty still write plain files under `/var/log` (`/var/log/nginx/error.log`, for example), so check both places.

### Scheduled tasks

Two mechanisms cover the "run this periodically" need that Task Scheduler covers on Windows:

- **`cron`**, the traditional Unix scheduler, driven by a crontab (`crontab -e` for your own user's jobs, `/etc/crontab` or `/etc/cron.d/` for system-wide ones). Simple, well understood, and still extremely common.
- **systemd timers**, a newer mechanism that pairs a `.timer` unit with a `.service` unit, giving you systemd's logging and dependency handling for free.

Neither is strictly better; plenty of production systems still lean entirely on cron. New systemd-managed services often use a timer alongside it, to keep everything about that service in one consistent system.

One more quiet piece of infrastructure worth knowing about: **`logrotate`**, which most distributions install and configure by default, periodically compresses, archives, and eventually deletes old log files so `/var/log` doesn't grow without bound. If you write your own application's logs straight to a plain file rather than through the systemd journal, check whether `logrotate` already has a rule for it (configuration under `/etc/logrotate.d/`), or add one yourself, rather than discovering months later that a single log file has filled the disk. <!-- TODO verify: current default logrotate installation/scheduling behavior across Debian/Ubuntu and Red Hat-family distributions -->

### Signals worth knowing by name

`SIGTERM` and `SIGKILL` aren't the only two signals you'll run into, just the two you'll use most as a sysadmin:

| Signal | Typical trigger | Meaning |
|---|---|---|
| `SIGHUP` | Terminal closing, or sent manually | Historically "the terminal hung up"; many daemons reinterpret it as "reload your configuration" |
| `SIGINT` | Ctrl+C | Interrupt: please stop |
| `SIGTERM` | Plain `kill` | Please shut down cleanly |
| `SIGKILL` | `kill -9` | Stop now, no cleanup, cannot be caught |

Seeing a service support `kill -HUP <pid>` as a "reload config without a full restart" mechanism is common enough that it's worth recognizing by name rather than being surprised by it.

### Disk and memory

```bash
df -h      # disk space, per filesystem, human-readable
du -sh *   # size of each item in the current directory
free -h    # memory usage, human-readable
```

`df -h` is usually your first stop when something mysteriously stops writing data; a full disk causes a surprising range of unrelated-looking failures. `du -sh *` helps you find out *what* filled it. `free -h` shows total, used, and available memory.

### The four-question incident checklist

When something's wrong and you don't yet know why, work through these in order:

1. **Is the disk full?** `df -h`
2. **Is the service actually running?** `systemctl status <service>`
3. **Is the port listening?** `ss -tlnp`
4. **What do the logs say?** `journalctl -u <service> -n 100` or `tail -n 100 /var/log/<relevant file>`

Most incidents resolve to one of these four questions within the first few minutes.

## Debugging with text tools

Re-orientation: the everyday toolkit for reading, searching, and summarizing text, which on Linux means log files, config files, and command output alike.

- **`less`**: a pager for viewing a file a screen at a time without loading the whole thing (`less /var/log/syslog`); `/` searches forward, `q` quits.
- **`head`** / **`tail`**: show the first or last lines of a file (`head -n 20 file`, `tail -n 20 file`). `tail -f file` follows a file as it grows, the classic way to watch a log in real time.
- **`grep`**: search text for a pattern (`grep "ERROR" app.log`). `-i` ignores case, `-r` searches recursively through a directory, `-c` counts matches instead of printing them.
- **`wc`**: counts things; `wc -l` counts lines, which combined with a pipe is a quick way to count how many results something returned.
- **`sort`** and **`uniq`**: `sort` orders lines; `uniq` collapses *adjacent* duplicate lines, which is why you almost always sort before you `uniq`. `uniq -c` prefixes each line with a count of how many times it occurred.
- **`awk`** and **`sed`**: both deserve their own references, but briefly, `awk` is good at working with columnar text (`awk '{print $1}'` prints the first whitespace-separated field of each line), and `sed` is good at find-and-replace and other line-by-line text transformations (`sed 's/foo/bar/'`).

### Worked example: the most common errors in a log

A genuinely common task: given a log file, find which error message occurs most often.

```bash
grep "ERROR" app.log | sort | uniq -c | sort -nr | head -n 5
```

Walking through each stage:

1. `grep "ERROR" app.log` : pull out only the lines containing "ERROR," discarding everything else.
2. `sort` : put identical lines next to each other, a required step before `uniq` can collapse them.
3. `uniq -c` : collapse adjacent identical lines into one, prefixed with a count of how many there were.
4. `sort -nr` : sort numerically (`-n`) in reverse (`-r`), so the highest counts come first.
5. `head -n 5` : keep only the top five.

Five short, individually simple commands, chained together, answer a question that would otherwise mean writing a small script. This is the actual payoff of "everything is a file" and pipes: small tools compose into exactly the tool you need for the moment.

One portability note worth repeating from the POSIX section: GNU and BSD/macOS versions of these tools sometimes diverge on flags, `sed -i` being the classic case (BSD requires an argument to `-i`; GNU doesn't). If a one-liner behaves differently on your Mac than it does on your Ubuntu server, this kind of GNU/BSD divergence is usually why.

## Windows-to-Linux translation table

For quick orientation, not completeness: the rough equivalents between `cmd`, PowerShell, and Linux for common tasks.

| Task | cmd | PowerShell | Linux |
|---|---|---|---|
| List files | `dir` | `Get-ChildItem` (`ls`) | `ls -l` |
| Copy a file | `copy` | `Copy-Item` | `cp` |
| Show a file's contents | `type` | `Get-Content` (`cat`) | `cat` |
| Search text in files | `findstr` | `Select-String` | `grep` |
| List processes | `tasklist` | `Get-Process` | `ps aux` |
| Stop a process | `taskkill /PID <pid>` | `Stop-Process -Id <pid>` | `kill <pid>` |
| Change permissions | `icacls` | `Set-Acl` | `chmod` |
| Find a file by name | `dir /s` | `Get-ChildItem -Recurse` | `find . -name "*.conf"` |
| Set an environment variable | `set VAR=value` | `$env:VAR = "value"` | `export VAR=value` |
| Manage a service | `sc` / `net start` | `Get-Service` / `Start-Service` | `systemctl` |
| View recent logs | Event Viewer | `Get-EventLog` | `journalctl` / `/var/log` |
| Current directory | `cd` | `Get-Location` (`pwd`) | `pwd` |

<!-- TODO verify: current PowerShell cmdlet names/aliases remain accurate on the latest shipping PowerShell version -->

## Linux as a workstation: daily-driver use and WSL

Re-orientation: everything above this point, the mental model, permissions, process and service management, text tools, applies whether Linux is a remote server or the machine in front of you. This section covers what's specific to using Linux as your actual development environment, day to day, rather than something you SSH into.

### The same model, a different front door

The biggest practical difference between server and workstation use isn't the underlying system, it's how you reach it. On a server, SSH is the front door. On a workstation, you're sitting at a desktop environment (GNOME, KDE, and others each bring their own window manager, settings app, and file manager) or, if you've chosen a minimal setup, straight at a terminal after login. Underneath either one, the terminal is still where development work actually happens: cloning a repository, running a build, starting a local database, debugging a failing test. The shell, the permissions model, and the package manager you'd use to install `git` or a language runtime are the same ones covered earlier in this post, so a solid server mental model carries over directly to the workstation.

Installing development tools follows the same package-manager-first habit as a server: `sudo apt install golang` or `sudo dnf install nodejs`, rather than hunting down an installer from a vendor's website. When a tool genuinely isn't packaged for your distribution, or you need a version newer than what's packaged, that's when you reach for a language-specific version manager (`nvm` for Node, `pyenv` for Python, `rustup` for Rust) instead, which is the Linux-ecosystem answer to "I need a different version than the system one" that a package manager alone doesn't solve well.

### Dotfiles are your workstation's configuration

The plain-text configuration habit from earlier in this post is exactly how you'll customize your own workstation. Your shell's startup file (`~/.bashrc` or `~/.zshrc`), your editor's configuration, your `~/.gitconfig`, your terminal emulator's settings: all plain text, all in your home directory, all safe to put in a personal Git repository and symlink into place on a new machine. This is why experienced Linux (and macOS) users talk about their "dotfiles repo": there's no registry-equivalent step, no exported settings file in a proprietary format, just files you already know how to version and restore.

### WSL

The Windows Subsystem for Linux deserves its own mention here because it's the easiest on-ramp for a Windows-based developer who wants this entire mental model without dual-booting or buying a second machine, but it's worth being precise about what it is and isn't.

WSL 2, the current default, runs a real Linux kernel inside a lightweight, Microsoft-managed virtual machine; WSL 1, the original version, instead translated Linux system calls into Windows kernel calls directly, without a real Linux kernel underneath at all. <!-- TODO verify: current WSL1 availability/support status and exact system-call translation description -->

WSL is a genuinely good full-time development environment, and it's good for practicing the commands in this post and testing scripts before they touch a real server. It is not a server, though: you don't own WSL's boot process the way you own a real machine's, its networking sits behind Windows' own networking stack rather than being directly addressable the way a server's would be, and while recent versions do support running systemd and real services, that's not something to build a production deployment habit around. <!-- TODO verify: current WSL networking model and current systemd support status/configuration -->

Two gotchas worth knowing before they surprise you, both of which apply whether you're using WSL for quick practice or as your daily driver:

- **`/mnt/c` behaves differently.** Files under `/mnt/c` (your Windows C: drive, mounted into the Linux filesystem) are subject to Windows' permission and case-sensitivity rules, not Linux's, and accessing them is noticeably slower than accessing files inside the Linux filesystem itself. For that reason, keep code you're actively working on inside the Linux filesystem (your WSL home directory) rather than under `/mnt/c`, and reserve `/mnt/c` for the occasional file you genuinely need to share with a Windows application. <!-- TODO verify: current performance characteristics and default case-sensitivity setting -->
- **CRLF line endings leak in.** A file edited in a Windows-native editor and then run as a shell script inside WSL can fail with cryptic errors, because Windows line endings (`\r\n`) aren't what the Linux shell expects (`\n`). Configuring your editor and Git (`core.autocrlf`) to keep line endings as `\n` for anything that will run inside WSL or on Linux avoids this entirely.

For the current, authoritative details on all of this, see [Microsoft's WSL documentation](https://learn.microsoft.com/windows/wsl/) directly rather than relying on a summary here, since this is an area Microsoft actively changes.

## Wrap-up

The model underneath all of this is simpler than the list of commands makes it look: one filesystem tree, plain-text configuration, owner/group/other permissions checked all the way down a path, processes supervised by systemd, and the shell as the normal way you operate any of it, whether you reach that shell over SSH on a remote server or at a terminal on your own machine. Everything else in this post, server hardening and workstation habits alike, is that handful of ideas worked out into specific commands and specific defaults.

A few topics worth a deeper look once the basics feel solid, each a reasonable next search rather than something you need today:

- ACLs, setuid, and the sticky bit, for the permission cases owner/group/other doesn't cover
- SSH hardening beyond key-only login and no root login
- Writing systemd unit files for more involved services
- Portable shell scripting, if you need scripts that run unmodified across distributions and macOS

If you want a safe place to try any command in this post without risking anything, WSL or a cheap virtual machine both work fine for that; nothing here needs a real server to practice on.

If there's a topic from this list, or anything else about running or using your own Linux systems, you'd like covered in more depth, say so. Any follow-up will stand on its own and link back here rather than assuming you've read this post recently.

## Further reading

- [Ubuntu Server documentation](https://documentation.ubuntu.com/server/)
- [Debian wiki: UnattendedUpgrades](https://wiki.debian.org/UnattendedUpgrades)
- [Red Hat documentation: configuring firewalls and packet filters](https://docs.redhat.com/en/documentation/red_hat_enterprise_linux/10/html/configuring_firewalls_and_packet_filters/using-and-configuring-firewalld)
- [Red Hat documentation: automating software updates](https://docs.redhat.com/en/documentation/red_hat_enterprise_linux/10/html/managing_software_with_the_dnf_tool/automating-software-updates-in-rhel)
- [OpenSSH `sshd_config` manual](https://man.openbsd.org/sshd_config)
- [The Open Group Base Specifications (POSIX)](https://pubs.opengroup.org/onlinepubs/9699919799/)
- [Microsoft: Windows Subsystem for Linux documentation](https://learn.microsoft.com/windows/wsl/)
- [Filesystem Hierarchy Standard](https://refspecs.linuxfoundation.org/fhs.shtml)
