---
name: TheScorpion Game Project
description: Arena combat hack-and-slash game in Unity - tight deadline, using Invector Basic RPG package
type: project
---

"The Scorpion" — arena combat / hack-and-slash Unity game. Masked warrior with dual blades, elemental powers (Fire + Lightning for MVP), wave-based combat with boss fight.

**Why:** User is redoing the project with <1 week deadline, focusing on a working prototype with 1 arena, quality gameplay mechanics.

**How to apply:**
- Prioritize getting a playable loop fast: movement → combat → enemies → waves → boss
- User purchased **Invector Third Person Controller - Melee Combat Template** ($70, v2.6.5) from Unity Asset Store to accelerate development
- This replaces custom PlayerController/movement/combat foundation from the original architecture docs
- Invector provides: 3rd person controller, melee combat/combos, lock-on, health/stamina, dodge/roll, AI template, inventory (skip for MVP), Mixamo-compatible humanoid rig support
- Scripts in architecture folder were written without Invector in mind — they need to be adapted to work WITH Invector's systems (vThirdPersonController, vMeleeCombatInput, vMeleeManager, etc.) rather than replacing them
- Recommended setup: Unity 2022.3 LTS + URP
- Keep scope tight to MVP: 1 arena, 10 waves, 3 enemy types + 1 boss, Fire + Lightning elements, ultimate system, HUD
