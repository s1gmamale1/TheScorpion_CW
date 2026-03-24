---
name: Unity Meta Files Need Assets/Refresh
description: New scripts written from terminal don't get .meta files until Assets/Refresh is run in Unity
type: feedback
---

When writing new .cs files from the terminal/Claude Code, Unity doesn't automatically detect them and generate .meta files. This causes the scripts to not compile into the assembly.

**Why:** Unity only scans for new files when it regains focus or when Assets/Refresh is explicitly called. Writing files from outside Unity bypasses its file watcher.

**How to apply:** After writing ANY new .cs file, always run:
```
mcp__mcp-unity__execute_menu_item(menuPath: "Assets/Refresh")
```
Then wait a few seconds before recompiling. This ensures Unity generates the .meta file and includes the script in compilation.
