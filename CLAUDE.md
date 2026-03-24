# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**The Scorpion** — a fast-paced arena combat / hack-and-slash game built in Unity (C#). A masked warrior with dual blades fights through 10 waves of enemies using elemental powers (Fire + Lightning) in a single arena, culminating in a 3-phase boss fight.

- **Engine**: Unity 6 (URP) or Unity 2022.3 LTS
- **Framework**: Invector Third Person Controller — Melee Combat Template (v2.6.5)
- **Unity Project Path**: `TheScorption_mvp/cw_1/`
- **Design Docs**: `Project Architechrure/extracted/TheScorpion/docs/`
- **Reference Scripts**: `Project Architechrure/extracted/TheScorpion/Assets/Scripts/`

## Architecture

### Invector (Purchased Asset — DO NOT MODIFY)
Located in `Assets/Invector-3rdPersonController/`. Provides third-person controller, melee combat system, health/stamina, dodge/roll, lock-on, animator integration, and basic AI templates. All custom game systems must **extend or hook into** Invector's systems, not replace them.

Key Invector classes to integrate with:
- `vThirdPersonController` / `vThirdPersonInput` — player movement
- `vMeleeCombatInput` / `vMeleeManager` — combat/combos
- `vHealthController` — health/damage
- `vMeleeAI` — enemy AI base

### Custom Systems (Built on Top of Invector)
| System | Purpose |
|--------|---------|
| ElementSystem | Fire/Lightning switching, abilities, energy management |
| UltimateSystem | Adrenaline meter, Adrenaline Rush (time-slow ultimate) |
| WaveManager | 10-wave spawn progression with enemy composition |
| EnemyAI (3 types) | Hollow Monk (basic), Shadow Acolyte (fast), Stone Sentinel (heavy) |
| BossAI | The Fallen Guardian — 3-phase boss with summons |
| HUDController | Health, adrenaline, element indicator, wave counter, ability cooldowns |
| GameManager | Game state, pause, restart |

### Key Design Specs (from GDD)
- **Arena**: 25m × 25m courtyard, 4 spawn points (N/S/E/W)
- **Elements**: Fire (DoT, area denial) and Lightning (speed, CC). One active at a time, switch with Q/E
- **Element Energy**: Max 100, regen 3/sec + 5 per hit
- **Adrenaline**: +2 per hit, +5 per kill, +10 per combo finisher. Ultimate at 100: 8s time-slow + damage boost
- **Enemy-Element Interactions**: Fire burns slow Fast enemies; Lightning stuns but no knockback on Heavy
- **Boss Phases**: Phase 1 (100-60% HP) sword combos + summons, Phase 2 (60-30%) fire aura + wave attack, Phase 3 (30-0%) enraged pure aggression

## MCP Tools

Four macOS GUI automation MCP servers are configured in `mcp-tools/`:
- **automation-mcp** — mouse, keyboard, screenshots, window management (runs via npx tsx)
- **mac-commander** — visual AI, OCR, smart UI detection (node)
- **mac-mcp-server** — 44 AppleScript tools for full macOS control (node)
- **macos-gui** — accessibility framework GUI control (python3)

These enable Claude Code to interact with the Unity Editor directly.

## Development Workflow

- Custom scripts go in `TheScorption_mvp/cw_1/Assets/Scripts/`
- All custom code must be compatible with Invector's architecture (extend, don't replace)
- Reference scripts in `Project Architechrure/extracted/` were written WITHOUT Invector — use for design intent only, not as drop-in code
- The user operates under a tight deadline (<1 week) — prioritize working prototype over polish
- User preference: autonomous execution — complete full task stages without stopping, self-check, then proceed to next stage
