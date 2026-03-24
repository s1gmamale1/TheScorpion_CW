---
name: Follow Research Materials Before Acting
description: User called out rushing without consulting research — always reference tutorial notes and research docs before making changes
type: feedback
---

Always check the research docs and tutorial notes BEFORE taking action. Don't rush through setup steps.

**Why:** User pointed out the character was falling through the floor because I skipped the ground detection setup from the Invector tutorials. The fix (missing MeshCollider + ground layer check) was documented in our own research at docs/tutorials/invector_core_tutorials.md.

**How to apply:** Before any Invector setup step, read the relevant section from:
- docs/tutorials/invector_core_tutorials.md (core setup, weapons, enemies)
- docs/tutorials/invector_ai_tutorials.md (AI setup, FSM)
- docs/research/06_invector_source_code_analysis.md (architecture, hooks)
- docs/research/12_deep_invector_integration.md (extension patterns, pitfalls)

Research first, plan, test, refine, test again — as the user instructed.
