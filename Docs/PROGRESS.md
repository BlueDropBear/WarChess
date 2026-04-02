# WarChess — Development Progress

This file tracks what has been completed, what is in progress, and what is next. Claude Code MUST read this file before starting any work to understand the current state.

---

## Phase 0: Foundation
- [x] Install Unity, VS Code/Rider, Git
- [x] Create GitHub repository with .gitignore for Unity
- [x] Complete Unity beginner tutorial
- [x] Game Design Document v2.0
- [ ] Technical Design Document
- [ ] Art Style Guide with reference examples
- [ ] Project folder structure scaffold (create the folder structure from CLAUDE.md)

**Status: IN PROGRESS**
**Current task: Technical Design Document**

---

## Phase 1: Core Grid Prototype
- [ ] Grid system — 10×10 tile generation, coordinate system, tile highlighting
- [ ] Unit data model — ScriptableObject architecture for unit types
- [ ] Unit placement — drag-and-drop units onto deployment zone (rows 1–3)
- [ ] Auto-battle engine v1 — turn-based resolution (move → attack → resolve)
- [ ] Battle visualization — units animate movement and attacks on grid
- [ ] Placeholder art — colored squares or free pixel sprites for 3 unit types
- [ ] Win/loss detection
- [ ] Flanking system — front/side/rear damage with configurable multipliers per unit
- [ ] Basic targeting AI — nearest, weakest, highest threat priorities

**Status: NOT STARTED**

---

## Phase 2: Game Loop & UI
- [ ] Army builder screen — separate from battle, save/load armies
- [ ] Campaign army builder — units limited by campaign progress
- [ ] Multiplayer army builder — all tier-appropriate units available
- [ ] Deployment screen — select saved army, review terrain, adjust placement, deploy
- [ ] Campaign map v1 — linear node progression (battle → battle)
- [ ] Enemy army AI — pre-built enemy compositions per campaign node
- [ ] Unit unlock system — new units introduced at campaign milestones
- [ ] Battle results screen — star rating (1–3 stars), unlocks summary
- [ ] Save/load system — persist campaign progress and saved armies locally
- [ ] Main menu, Armory hub, settings, pause menu
- [ ] UI framework — consistent pixel-art UI kit (buttons, panels, fonts)
- [ ] Sound effects — free SFX for attacks, movement, victory, defeat
- [ ] Background music — era-appropriate free tracks

**Status: NOT STARTED**

---

## Phase 3: Content & Depth
- [ ] Full unit roster (14 units) with balanced stats
- [ ] Terrain system — tiles with combat modifiers (forest, hill, river, fortification, mud, town)
- [ ] Line of sight system for ranged units
- [ ] Commander abilities — 6 commanders with manual/automatic triggers
- [ ] Formation bonuses — detection and application (Battle Line, Battery, Wedge, Square, Skirmish)
- [ ] Officers system — 12 officers with positive/negative traits, leveling, budget cost
- [ ] Full campaign — 30 battles across 3 acts with narrative context
- [ ] Tutorial system — contextual tooltips and guided first battles
- [ ] Balance testing spreadsheet — unit matchup matrix, win-rate tracking
- [ ] Difficulty settings (Recruit / Veteran / Marshal)
- [ ] Fog of war mechanic for specific campaign battles
- [ ] Pixel art — unit sprites, terrain tiles, UI elements

**Status: NOT STARTED**

---

## Phase 4: Multiplayer
- [ ] Backend setup (PlayFab or Firebase)
- [ ] Player authentication — anonymous + optional email/social login
- [ ] Army serialization — encode army composition + placement + officers as JSON
- [ ] Star General tier system — 5 tiers with unit gating and independent Elo
- [ ] Army pool system — deploy armies, server matches and resolves
- [ ] Ammunition system — earning, spending, purchasing
- [ ] Deterministic battle resolution — seeded RNG, server-side validation
- [ ] Battle replay viewer
- [ ] Leaderboard — per-tier Elo rankings
- [ ] Anti-cheat — server-side army validation, point budget enforcement, tier enforcement
- [ ] Multiplayer UI — tier selection, army pool, active deployments, match history

**Status: NOT STARTED**

---

## Phase 5: Polish & Platform Prep
- [ ] Touch controls — mobile-friendly drag, zoom, tap interactions
- [ ] Responsive UI — scales from phone to desktop
- [ ] Performance optimization — target 60fps on mid-range phones
- [ ] Particle effects — battle impact, unit death, ability activation
- [ ] Screen shake, hit flash, damage numbers
- [ ] Music and SFX polish pass
- [ ] Accessibility — colorblind mode, text scaling, unit labels
- [ ] Localization framework (English first)
- [ ] Analytics integration
- [ ] Dispatch Box system — opening animation, cosmetic rewards
- [ ] Cosmetics shop — rotating items, equipped skins/themes
- [ ] Monetization — Act 2–3 purchase gate, ammunition IAP, cosmetic purchases
- [ ] Apple Developer Account + Google Play Console setup
- [ ] iOS build setup — Xcode, provisioning, TestFlight
- [ ] Android build setup — keystore, APK/AAB generation
- [ ] PC build — itch.io or Steam listing

**Status: NOT STARTED**

---

## Phase 6: Launch
- [ ] Store listings — screenshots, description, keywords, icon
- [ ] Trailer / gameplay GIF
- [ ] Beta test — invite 10–20 players, collect feedback
- [ ] Bug fix sprint based on beta feedback
- [ ] Submit to App Store + Google Play
- [ ] Launch on itch.io / Steam
- [ ] Launch announcement — Reddit, Twitter, indie game forums
- [ ] Post-launch monitoring — crash reports, server health

**Status: NOT STARTED**

---

## Phase 7: Post-Launch
- [ ] AI QA Balance Tester — headless simulation tool
- [ ] Balance patches based on matchup data
- [ ] New unit releases
- [ ] Seasonal ranked seasons
- [ ] Community feedback pipeline
- [ ] New campaign chapters

**Status: NOT STARTED**

---

## How to Update This File

When completing a task:
1. Change `[ ]` to `[x]` for the completed item
2. Update the phase **Status** line if the phase is now complete
3. Update **Current task** to reflect what should be worked on next
4. Add any notes about decisions made or issues encountered below the task

When all items in a phase are checked, change its status to `COMPLETE` and update the next phase to `IN PROGRESS`.
