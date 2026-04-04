# WarChess — Claude Code Instructions

## MANDATORY: Read Before Doing Anything

Before writing any code, creating any files, or making any changes to this project, you MUST:

1. Read this file completely
2. Read `Docs/GAME_DESIGN_DOCUMENT.md` — this is the authoritative source for all game mechanics, systems, and design decisions
3. Read `DEVELOPMENT_ROADMAP.md` — this is the project plan with phases, tasks, and ownership
4. Check `Docs/PROGRESS.md` — this tracks what has been completed and what is in progress

Only after reading all of the above should you begin any work. If a task conflicts with the GDD, the GDD wins. If you're unsure about a design decision, say so rather than guessing.

---

## Project Overview

WarChess is a Napoleonic-era auto-battler built in Unity (C#) with pixel art. It targets PC, iOS, and Android. The player builds armies on a 10×10 grid, deploys them into battles, and watches them fight automatically.

Key systems: grid-based combat, 14 unit types, Officers (unit modifiers), Commanders (army-wide abilities), Star General multiplayer tiers, async army pool multiplayer, deterministic battle engine.

---

## Architecture Principles

- **Configurable values:** Deployment zones, flanking multipliers, damage formulas, unit stats — all must be data-driven (ScriptableObjects or JSON config), never hardcoded magic numbers. The GDD explicitly marks many values as "configurable for testing."
- **Deterministic battle engine:** No floating point math in combat. Use integers only. All randomness must use seeded RNG. Same inputs = same outputs, always. This is required for async multiplayer.
- **Separation of concerns:** Battle logic must run headless (no Unity rendering dependencies) so the AI QA Balance Tester (see GDD Section 12) can simulate thousands of battles without the game client.
- **ScriptableObjects for data:** Units, Officers, Commanders, terrain types, formations — all defined as ScriptableObjects. No unit stats in code.
- **Mobile-first UI:** Design for phone screens, scale up for tablet/PC. Touch and mouse input both supported.

---

## Folder Structure

```
Assets/
├── Scripts/
│   ├── Core/              # Grid, tile, coordinate systems
│   ├── Units/             # Unit data, stats, behavior
│   ├── Officers/          # Officer data, traits, leveling
│   ├── Commanders/        # Commander abilities and triggers
│   ├── Battle/            # Auto-battle engine, turn resolution, targeting AI
│   ├── Army/              # Army builder, saved armies, deployment
│   ├── Campaign/          # Campaign map, progression, unlocks, star ratings
│   ├── Multiplayer/       # Army pool, tier system, Elo, matchmaking
│   ├── Terrain/           # Terrain types, line of sight, movement costs
│   ├── Formations/        # Formation detection and bonus application
│   ├── UI/                # All UI controllers and views
│   ├── Audio/             # Sound manager, music controller
│   ├── Save/              # Save/load system, serialization
│   ├── Config/            # GameConfig, balance constants, tunable values
│   └── QA/                # AI balance tester (editor-only)
├── Data/
│   ├── Units/             # Unit ScriptableObjects
│   ├── Officers/          # Officer ScriptableObjects
│   ├── Commanders/        # Commander ScriptableObjects
│   ├── Terrain/           # Terrain type ScriptableObjects
│   ├── Campaign/          # Campaign battle data, enemy compositions
│   ├── Maps/              # Map templates for multiplayer
│   └── Config/            # GameConfig ScriptableObject (deployment zones, multipliers, etc.)
├── Art/
│   ├── Units/             # Unit sprites and animations
│   ├── Terrain/           # Terrain tile sprites
│   ├── UI/                # UI elements, buttons, panels
│   └── Effects/           # Particles, hit effects, death animations
├── Audio/
│   ├── SFX/               # Sound effects
│   └── Music/             # Background music tracks
├── Scenes/
│   ├── MainMenu.unity
│   ├── Armory.unity
│   ├── Campaign.unity
│   ├── Battle.unity
│   └── Multiplayer.unity
└── Prefabs/
    ├── Units/             # Unit prefabs
    ├── Tiles/             # Grid tile prefabs
    └── UI/                # Reusable UI prefabs
```

---

## Key Design Decisions (Do Not Override)

These are settled decisions from the GDD. Do not change without explicit instruction:

- Grid defaults to 10×10 but dimensions are configurable via GameConfig for testing and balance tuning
- Player deploys on rows 1–3 (configurable via GameConfig)
- Campaign enemies can use rows 5–10 (configurable)
- Multiplayer enemies use rows 8–10 (configurable)
- Army building is SEPARATE from deployment — players save armies, then choose one to deploy
- Campaign and Multiplayer have independent army builders with different unit pools
- Flanking: Front = normal, Side = ×1.3, Rear = ×2.0 (all configurable per unit type)
- Officers have one positive AND one negative trait, always
- Multiplayer uses army pool + ammunition system, not direct matchmaking
- 5 Star General tiers gate multiplayer units
- Act 1 is free, Acts 2–3 are a one-time purchase
- No pay-to-win, no paying to unlock units faster
- Dispatch Boxes are symbolic/cosmetic only

---

## Coding Standards

- C# with Unity conventions (PascalCase for public, camelCase for private, _prefix for private fields)
- Every public class and method gets an XML doc comment
- No `MonoBehaviour` for pure data or logic classes — only for Unity lifecycle needs
- Battle engine code must be testable without Unity (plain C# classes, no `UnityEngine` dependencies)
- Use events/delegates for loose coupling between systems (e.g., `OnUnitDied`, `OnBattleEnded`)
- Prefer composition over inheritance for unit behaviors

---

## Current Phase

Check `Docs/PROGRESS.md` for the current phase and task status. The development roadmap in `DEVELOPMENT_ROADMAP.md` defines the phase order.
