---
name: Security - Never Accept Passwords
description: User attempted to share sudo password in chat - always refuse and explain the risk
type: feedback
---

Never accept, use, or store passwords shared in chat. Always redirect user to enter passwords themselves via secure terminal input.

**Why:** Passwords in chat can be logged, cached, or exposed. It's a fundamental security risk.

**How to apply:** If user shares a password, immediately warn them, refuse to use it, and instruct them to run the command themselves via `! command` prefix for secure input.
