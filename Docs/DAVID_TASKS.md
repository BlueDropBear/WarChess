# WarChess — David's Task List

This file tracks tasks that require David (the human developer) to complete. These involve Unity editor work, hardware/account setup, playtesting, and other things Claude cannot do remotely.

Claude will add new tasks here as they arise. David should check `[ ]` to `[x]` when done.

---

## Phase 1: Core Grid Prototype (CURRENT)

### Unity Editor Setup
- [ ] **Create GameConfig asset** — In Unity: Right-click `Assets/Data/Config/` → Create → WarChess → Game Config. Default values are already set in code. Save as `GameConfig.asset`.
- [ ] **Create 3 Unit ScriptableObject assets** — Right-click `Assets/Data/Units/` → Create → WarChess → Unit Stats. Create one each for:
  - `LineInfantry.asset` — HP:30, ATK:8, DEF:6, SPD:3, RNG:1, MOV:2, COST:3, Targeting:Nearest, Ability:None, Formation:BattleLine, CountsAs:LineInfantry, FlankSide:130, FlankRear:200
  - `Cavalry.asset` — HP:25, ATK:10, DEF:4, SPD:6, RNG:1, MOV:4, COST:5, Targeting:HighestThreat, Ability:Charge, Formation:CavalryWedge, CountsAs:Cavalry, FlankSide:130, FlankRear:200
  - `Artillery.asset` — HP:15, ATK:14, DEF:2, SPD:1, RNG:4, MOV:1, COST:6, Targeting:Nearest, Ability:Bombardment, Formation:Battery, CountsAs:Artillery, FlankSide:130, FlankRear:200

### Scene Setup
- [ ] **Create Battle scene** — File → New Scene (2D). Save to `Assets/Scenes/Battle.unity`. Add:
  1. Empty GameObject "Grid" → Add `GridView` component
  2. Empty GameObject "BattleController" → Add `BattleController` and `BattleVisualizer` components
  3. Wire up references: BattleController needs GridView, BattleVisualizer, and GameConfig asset
  4. Add `BattleSetupDemo` component (Claude will provide this script) for quick testing
  5. Ensure Camera is orthographic, size ~6, positioned to see the grid

### Unit Prefabs
- [ ] **Create unit prefab** — In `Assets/Prefabs/Units/`, create a prefab with:
  1. SpriteRenderer (any 32x32 square sprite, or use the placeholder the code generates)
  2. `UnitView` component
  3. Child object "HealthBar" with SpriteRenderer (thin rectangle) — wire into UnitView's healthBarFill/healthBarRenderer fields
  4. Optional: different colored variants for player (red) vs enemy (blue)

### Tile Prefab
- [ ] **Create tile prefab** — In `Assets/Prefabs/Tiles/`, create a prefab with:
  1. SpriteRenderer (1x1 unit square sprite)
  2. BoxCollider2D (for click detection)
  3. Assign to GridView's tilePrefab field

### Playtesting
- [ ] **Run first test battle** — Open Battle scene, enter Play mode. BattleSetupDemo should auto-place units and run a battle. Verify:
  - Grid renders as 10x10 tiles
  - Units appear on correct tiles
  - Units move toward enemies each round
  - Attacks trigger hit flash
  - Dead units fade out
  - Battle ends with a result logged to console
- [ ] **Report any issues** — Note anything that doesn't work, looks wrong, or feels off

---

## Phase 2: Game Loop & UI (UPCOMING)

### Scenes
- [ ] **Create MainMenu scene** — Save to `Assets/Scenes/MainMenu.unity`
- [ ] **Create Armory scene** — Save to `Assets/Scenes/Armory.unity`
- [ ] **Create Campaign scene** — Save to `Assets/Scenes/Campaign.unity`

### Input System
- [ ] **Set up drag-and-drop** — Using Unity's InputSystem, implement touch/mouse drag in `UnitPlacementController` (Claude provides the validation logic, David wires up InputSystem actions)
- [ ] **Test on mobile** — If you have a test device, check that tap and drag work correctly

### UI
- [ ] **Find a pixel art font** — Free pixel font for all UI text (e.g., from itch.io or Google Fonts). Import to project.
- [ ] **Source free SFX** — From freesound.org, find:
  - Musket volley (infantry attack)
  - Cannon boom (artillery)
  - Hoofbeats (cavalry movement)
  - Drum roll (battle start)
  - Fanfare (victory)
  - Somber tone (defeat)
- [ ] **Source background music** — From incompetech.com or similar, find era-appropriate tracks for menu, battle, and results screens

---

## Phase 2 (continued): Game Loop UI

Claude has built all the data-layer systems. These UI tasks require Unity editor work:

### Campaign Map Screen
- [ ] **Build campaign map UI** — In Campaign scene, create a scrollable/pannable map with 30 battle nodes. Use `CampaignDatabase.AllBattles` to populate names and lock/unlock states. Wire to `CampaignManager.CanPlayBattle()` for gating.
  - Act 1 nodes (1-10): always accessible
  - Act 2-3 nodes (11-30): grayed out with purchase prompt if `!IsFullCampaignUnlocked`
  - Completed battles show star count from `CampaignManager.GetStars()`
  - Tapping a node shows: battle name, narrative intro, point budget, terrain type

### Army Builder Screen
- [ ] **Build army builder UI** — In Armory scene, create a two-panel layout:
  - Left panel: scrollable list of available units (filtered by `CampaignManager.GetUnlockedUnits()` for campaign mode)
  - Right panel: 10x3 grid (deployment zone) where units are placed
  - Show unit stats on hover/tap (HP, ATK, DEF, SPD, RNG, MOV, COST)
  - Budget bar showing points spent / budget
  - Save/Load/Delete army buttons — wire to `ArmyManager`

### Battle Results Screen
- [ ] **Build results screen UI** — After battle ends, show:
  - Star rating (1-3 stars) from `BattleResultCalculator`
  - Units surviving count
  - Rounds played
  - Any new unlocks (units/commanders) from `BattleCompletionResult`
  - "Continue" button returns to campaign map

### Main Menu
- [ ] **Build main menu** — Buttons: Campaign, Armory, Multiplayer (grayed out), Settings
  - Wire to `GameManager.GoToCampaign()`, `GameManager.GoToArmory()`
  - Settings panel: music/SFX volume sliders, screen shake toggle, battle speed

### Scene Wiring
- [ ] **Add scenes to Build Settings** — File → Build Settings → add all 5 scenes:
  - MainMenu (index 0), Armory, Campaign, Battle, Multiplayer

---

## Phase 4: Multiplayer

Claude has built all the multiplayer game logic. These tasks require backend and UI work:

### Backend Setup
- [ ] **Choose backend** — PlayFab (recommended, free tier) or Firebase. Both support auth, leaderboards, cloud data, and cloud functions.
- [ ] **Integrate SDK** — Install PlayFab/Firebase Unity SDK. Set up project in their dashboard.
- [ ] **Player auth** — Anonymous login on first launch, optional email/social. Store PlayerId.
- [ ] **Cloud functions** — Deploy server-side logic:
  - Army submission endpoint (calls `ArmyValidator.Validate()` then adds to pool)
  - Matchmaking cron job (calls `ArmyPool.RunMatchmaking()` periodically)
  - Battle resolution (calls `ArmyPool.ResolveMatch()` for each match)
  - Elo update + replay storage

### Multiplayer UI
- [ ] **Build Multiplayer scene** — Save to `Assets/Scenes/Multiplayer.unity`
- [ ] **Tier selection screen** — Show player's tier badges, current Elo/rank per tier, wins needed for next tier. Use `TierSystem` and `PlayerProfile` data.
- [ ] **Army pool screen** — Show:
  - "Deploy Army" button (spends 1 ammo via `AmmunitionSystem.SpendForDeployment()`)
  - Active deployments list from `ArmyPool.GetPlayerSubmissions()`
  - Withdraw button per deployment
  - Ammunition balance + purchase button
- [ ] **Match history** — List from `ArmyPool.GetMatchHistory()`. Show: opponent, outcome, Elo change, replay button.
- [ ] **Replay viewer** — Load `BattleReplay` data, create units from submissions, feed serialized events to `BattleVisualizer`. Add speed controls (1x, 2x, 4x).
- [ ] **Leaderboard** — Per-tier Elo rankings. Use backend leaderboard API. Show rank icon from `EloSystem.GetRank()`.

## Phase 5+ (FUTURE — Claude will populate as we get closer)

_Tasks will be added here as Phase 2 nears completion._

---

## How This File Works

- Claude adds tasks with `- [ ]` when new work is identified
- David checks them off: `- [x]`
- If a task is blocked or unclear, add a note below it and Claude will address it
- Tasks are grouped by phase and category
- Priority within a phase: do top items first (they unblock other work)
