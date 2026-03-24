---
name: Use GUI Tools Instead of Asking User
description: Always use GUI MCP tools to interact with Unity directly — don't ask user to click things
type: feedback
---

Use GUI tools (activate_app, click, key_combination, drag, etc.) to interact with Unity Editor directly. Don't ask the user to perform clicks or UI interactions.

**Why:** User pointed out I have full GUI access and should use it instead of asking them to do clicks.

**How to apply:** For any Unity Editor interaction (domain reload, clicking buttons, navigating menus, dragging objects), use the mac-mcp-server or mac-commander tools. Only ask the user for interactions that truly can't be automated.
