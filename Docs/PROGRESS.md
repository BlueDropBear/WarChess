# WarChess — Development Progress

This file tracks what has been completed, what is in progress, and what is next. Claude Code MUST read this file before starting any work to understand the current state.

---

## Phase 0: Foundation
- [x] Install Unity, VS Code/Rider, Git
- [x] Create GitHub repository with .gitignore for Unity
- [x] Complete Unity beginner tutorial
- [x] Game Design Document v2.0
- [x] Technical Design Document
- [x] Art Style Guide with reference examples
- [x] Project folder structure scaffold (create the folder structure from CLAUDE.md)

**Status: COMPLETE**

---

## Phase 1: Core Grid Prototype
- [x] Grid system — 10×10 tile generation, coordinate system, tile highlighting
  - GridCoord.cs, GridMap.cs (pure C#), GridView.cs (Unity MonoBehaviour)
- [x] Unit data model — ScriptableObject architecture for unit types
  - UnitStatsSO.cs, UnitInstance.cs, UnitEnums.cs, GameConfigSO.cs, GameConfigData.cs
- [x] Unit placement — drag-and-drop units onto deployment zone (rows 1–3)
  - UnitPlacementController.cs (validation, budget, click-to-place). David: wire InputSystem drag.
- [x] Auto-battle engine v1 — turn-based resolution (move → attack → resolve)
  - BattleEngine.cs, MovementResolver.cs (pure C#)
- [x] Battle visualization — units animate movement and attacks on grid
  - BattleVisualizer.cs, BattleController.cs, UnitView.cs
- [x] Placeholder art — colored squares or free pixel sprites for 3 unit types
  - BattleSetupDemo generates distinct shapes per type (square/diamond/circle). David: create proper prefabs later.
  - UnitFactory.cs creates all 3 prototype units with GDD stats (no SO assets needed to test)
- [x] Win/loss detection
  - Built into BattleEngine: all dead, round 30 HP comparison, draw
- [x] Flanking system — front/side/rear damage with configurable multipliers per unit
  - FlankingCalculator.cs (pure C#), per-unit multipliers in UnitStatsSO
- [x] Basic targeting AI — nearest, weakest, highest threat priorities
  - ITargetingStrategy + 5 implementations + TargetingFactory (pure C#)

**Status: COMPLETE (code-side) — David: create Battle scene, add BattleSetupDemo component, press Play**
**See Docs/DAVID_TASKS.md for David's remaining Unity editor tasks**

---

## Phase 2: Game Loop & UI
- [ ] Army builder screen — separate from battle, save/load armies
  - SavedArmy.cs, ArmyManager.cs data layer DONE (pure C#). David: build UI.
- [ ] Campaign army builder — units limited by campaign progress
  - CampaignManager.GetUnlockedUnits() provides gating. David: build UI.
- [ ] Multiplayer army builder — all tier-appropriate units available
- [ ] Deployment screen — select saved army, review terrain, adjust placement, deploy
  - UnitPlacementController.cs provides validation. David: build UI.
- [ ] Campaign map v1 — linear node progression (battle → battle)
  - CampaignDatabase.cs has all 30 battles with narrative, budgets, unlocks. David: build map UI.
- [x] Enemy army AI — pre-built enemy compositions per campaign node
  - CampaignDatabase.cs: Act 1 (battles 1-10) fully specified with enemy placements
  - Acts 2-3 enemy compositions TBD (battle metadata complete)
- [x] Unit unlock system — new units introduced at campaign milestones
  - CampaignManager.CompleteBattle() processes unlocks per GDD schedule
  - CampaignDatabase tracks which battles unlock which units/commanders
- [x] Battle results screen — star rating (1–3 stars), unlocks summary
  - BattleResultCalculator.cs: 0-3 stars per GDD rules. David: build results UI.
- [x] Save/load system — persist campaign progress and saved armies locally
  - SaveManager.cs (JSON to persistentDataPath), SaveData.cs (campaign + armies + settings)
- [ ] Main menu, Armory hub, settings, pause menu
  - GameManager.cs handles scene navigation and state. David: build scene UIs.
- [ ] UI framework — consistent pixel-art UI kit (buttons, panels, fonts)
- [ ] Sound effects — free SFX for attacks, movement, victory, defeat
- [ ] Background music — era-appropriate free tracks

**Status: IN PROGRESS**
**Done: Data layer (save/load, campaign database, army management, star ratings, scene nav)**
**Remaining: All UI screens (David), audio sourcing, Acts 2-3 enemy compositions**

---

## Phase 3: Content & Depth
- [x] Full unit roster (14 units) with balanced stats
  - UnitFactory.cs: all 14 units with GDD stats + CreateByTypeName() lookup
- [x] Terrain system — tiles with combat modifiers (forest, hill, river, fortification, mud, town)
  - TerrainData.cs (movement costs, multipliers), TerrainMap.cs (tile storage)
- [x] Line of sight system for ranged units
  - LineOfSight.cs: Bresenham's line, hill exception, Rocket Battery ignores LoS
- [x] Commander abilities — 6 commanders with manual/automatic triggers
  - CommanderDatabase.cs (all 6), CommanderSystem.cs (buff tracking, activation logic)
- [x] Formation bonuses — detection and application (Battle Line, Battery, Wedge, Square, Skirmish)
  - FormationDetector.cs: all 5 formations with adjacency/row/diagonal detection
- [x] Officers system — 12 officers with positive/negative traits, leveling, budget cost
  - OfficerData.cs: 12 officers with stat mods, leveling thresholds (5/15/30/50 battles)
  - OfficerSystem.cs: OfficerInstance (level, XP), OfficerManager (collection CRUD, cost calc)
- [x] Full campaign — 30 battles across 3 acts with narrative context
  - CampaignDatabase.cs: all 30 battles with enemy placements for all 3 acts
- [x] Tutorial system — contextual tooltips and guided first battles
  - TutorialDatabase.cs: 20+ tutorial steps triggered at key battles, covers all mechanics
- [x] Balance testing spreadsheet — unit matchup matrix, win-rate tracking
  - BalanceTester.cs: headless Mode 1 (14x14 matchup matrix) + Mode 2 (composition stress test)
  - Runs thousands of battles with no rendering, flags anomalies per GDD Section 12
- [x] Difficulty settings (Recruit / Veteran / Marshal)
  - DifficultyScaler.cs: stat scaling (-15%/normal/+15%), info visibility levels
- [x] Fog of war mechanic for specific campaign battles
  - FogOfWarSystem.cs: visibility tracking, proximity reveal, Scout Master support
- [ ] Pixel art — unit sprites, terrain tiles, UI elements
- [x] BattleEngineV2 — integrates terrain, formations, commanders, LoS into battle loop
  - MovementResolverV2.cs: terrain-aware movement costs
- [x] Terrain map templates — hand-crafted maps for all 30 campaign battles + Waterloo
  - TerrainMapTemplates.cs: 20+ unique map layouts using all 8 terrain types

**Status: COMPLETE (code-side)**
**Remaining: Pixel art (David)**

---

## Phase 4: Multiplayer
- [ ] Backend setup (PlayFab or Firebase)
  - David: choose and configure backend. All client-side logic is ready.
- [ ] Player authentication — anonymous + optional email/social login
  - David: integrate backend auth SDK
- [x] Army serialization — encode army composition + placement + officers as JSON
  - ArmySerializer.cs: SavedArmy → ArmySubmission → UnitInstances roundtrip
- [x] Star General tier system — 5 tiers with unit gating and independent Elo
  - TierSystem.cs: all 5 tiers with unit lists, GetHighestUnlockedTier()
- [x] Army pool system — deploy armies, server matches and resolves
  - ArmyPool.cs: submit/withdraw/matchmake/resolve. Elo-based matching.
  - Full match lifecycle: submit → pool → match → resolve → history
- [x] Ammunition system — earning, spending, purchasing
  - AmmunitionSystem.cs: daily login (3), wins (1), campaign (2), tier promo (10), IAP
- [x] Deterministic battle resolution — seeded RNG, server-side validation
  - BattleEngine already deterministic. ArmyPool.ResolveMatch() uses it.
- [x] Battle replay viewer
  - BattleReplay.cs: replay data model, serialized events, ReplayFactory
  - BattleEventSerializer.cs: converts events to serializable format
- [x] Leaderboard — per-tier Elo rankings
  - EloSystem.cs: Elo calculation, 8 rank tiers (Recruit → Grand Marshal)
  - PlayerProfile.cs: per-tier Elo/wins/losses tracking, tier promotions
- [x] Anti-cheat — server-side army validation, point budget enforcement, tier enforcement
  - ArmyValidator.cs: validates budget, tier units, placements, grid bounds
  - 3 match formats (Skirmish 25pt, Standard 40pt, Grand Battle 60pt)
- [ ] Multiplayer UI — tier selection, army pool, active deployments, match history
  - David: build UI screens using the data systems above

**Status: IN PROGRESS (code-side COMPLETE)**
**Remaining: Backend setup (David), auth (David), multiplayer UI (David)**

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
