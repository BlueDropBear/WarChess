# WarChess — Game Design Document v2.0
### A Napoleonic Auto-Battler

---

## 1. Game Overview

### 1.1 Elevator Pitch

WarChess is a Napoleonic-era auto-battler where players command armies of historically inspired units on a configurable grid (default 10×10). Build your formation, deploy your troops, and watch the battle unfold. Master terrain, exploit unit synergies, and outthink your opponent — not outclick them.

### 1.2 Genre & Influences

WarChess sits at the intersection of auto-battlers (Teamfight Tactics, Super Auto Pets) and tactical grid games (Into the Breach, Advance Wars). The key distinction: players make all decisions before the battle begins. Once formations are set, the battle resolves automatically. Strategy lives in army composition and placement, not real-time execution.

### 1.3 Target Audience

Players who enjoy tactical thinking but want shorter sessions — a single battle should take 1–3 minutes of setup and 30–60 seconds of auto-resolved combat. The game appeals to fans of chess, auto-battlers, and historical strategy who want something they can play during a commute or lunch break.

### 1.4 Platforms & Controls

- **PC:** Mouse-driven. Click to select units, click to place on grid, scroll to zoom.
- **iOS / Android:** Touch-driven. Tap to select, tap or drag to place, pinch to zoom.
- All platforms share the same game logic. UI adapts to input method and screen size.

### 1.5 Session Length

- **Army building phase:** 1–3 minutes
- **Battle resolution:** 30–60 seconds (watchable, skippable, replayable)
- **Total session:** 2–5 minutes per battle, longer sessions for campaign progression

---

## 2. Core Mechanics

### 2.1 The Grid

The battlefield is a **square grid** (default 10×10, configurable via GameConfig for testing). Grid width and height are stored in GameConfig and can be adjusted independently. All deployment zones, movement ranges, and formation detection adapt to the configured grid size. Each tile can hold one unit or one terrain feature, not both (units occupy terrain tiles — they coexist).

```
     1   2   3   4   5   6   7   8   9   10
   ┌───┬───┬───┬───┬───┬───┬───┬───┬───┬───┐
10 │   │   │   │   │   │   │   │   │   │   │  ← Enemy back row
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 9 │   │   │   │   │   │   │   │   │   │   │
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 8 │   │   │   │   │   │   │   │   │   │   │  ← Enemy deploy zone (rows 8–10)
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 7 │   │   │   │   │   │   │   │   │   │   │
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 6 │   │   │   │   │   │   │   │   │   │   │
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤  ← No-man's land (rows 4–7)
 5 │   │   │   │   │   │   │   │   │   │   │
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 4 │   │   │   │   │   │   │   │   │   │   │
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 3 │   │   │   │   │   │   │   │   │   │   │  ← Player deploy zone (rows 1–3)
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 2 │   │   │   │   │   │   │   │   │   │   │
   ├───┼───┼───┼───┼───┼───┼───┼───┼───┼───┤
 1 │   │   │   │   │   │   │   │   │   │   │  ← Player back row
   └───┴───┴───┴───┴───┴───┴───┴───┴───┴───┘
```

**Deployment zones (configurable — values stored in GameConfig for easy tuning):**

**Multiplayer:**
- **Player** places units on rows 1–3
- **Enemy** places units on rows 8–10
- **Rows 4–7** are no-man's land — empty at battle start, fought over during resolution

**Campaign:**
- **Player** places units on rows 1–3
- **Enemy** can occupy rows 5–10 (larger territory, representing defensive advantage or entrenched positions)
- This asymmetry creates varied campaign challenges — some battles the enemy is close, others they're dug in deep

### 2.2 Army Building (Separate from Battle)

Army building and battle deployment are **two separate activities**. Players build and save armies outside of battles, then choose which saved army to deploy when entering a fight.

**Army Builder:**
- Accessed from the main menu at any time — not tied to a specific battle
- **Campaign Army Builder** — Only units the player has unlocked are available. Point budget is shown per-battle but armies can be pre-built to common budget sizes.
- **Multiplayer Army Builder** — All 14 units available from the start. Budget fixed per format (25/40/60). Officers can be assigned here (see Section 2.8).
- Players can **save multiple armies** with custom names (e.g., "Cavalry Rush," "Artillery Fort," "Balanced Line")
- Each saved army stores: unit composition, unit grid positions (rows 1–3), assigned Officers, and Commander selection
- No limit on saved armies

**Why separate?** This lets players theory-craft and refine armies between sessions without being locked into a battle. It also enables the multiplayer army pool system (see Section 7.3).

### 2.3 Deployment Phase (Pre-Battle)

When entering a battle, the player:

1. **Selects a saved army** from their collection (or builds one on the spot)
2. **Reviews the battlefield** — terrain is visible, enemy composition may be partially or fully visible depending on difficulty/mode
3. **Adjusts placement** if desired — the saved positions are loaded but can be tweaked for this specific map
4. **Confirms deployment** to start the battle

There is no time limit during deployment in single player. In multiplayer, both players submit asynchronously.

### 2.4 Auto-Battle Resolution

Once both sides are deployed, the battle resolves automatically in discrete **rounds**. Each round follows this sequence:

**Round order:**
1. **Initiative** — All units are sorted by Speed stat (ties broken by random seed)
2. **Movement** — Each unit moves toward its target (determined by AI behavior)
3. **Combat** — Each unit in range attacks its target
4. **Cleanup** — Dead units removed, formation bonuses recalculated

**Battle ends when:**
- All units on one side are eliminated → other side wins
- **30 rounds** pass with units remaining on both sides → side with more total remaining HP wins
- Both sides eliminated simultaneously → draw

### 2.5 Unit Targeting AI

Units don't just attack the nearest enemy. Each unit type has a **targeting priority** that determines which enemy it engages:

| Priority | Description | Used by |
|----------|-------------|---------|
| Nearest | Closest enemy by grid distance | Line Infantry, Grenadier, Militia |
| Weakest | Lowest current HP | Rifleman, Hussar |
| Highest Threat | Highest ATK stat | Cavalry, Cuirassier, Lancer |
| Artillery First | Prioritizes ranged/siege units | Dragoon |
| Random | Random valid target | Rocket Battery |

If a unit's preferred target type isn't present, it falls back to **Nearest**.

### 2.6 Combat Math

Damage is calculated as:

```
Base Damage = Attacker ATK - (Defender DEF / 2)
Minimum Damage = 1 (attacks always deal at least 1)
```

**Modifiers (multiplicative, applied in order):**
- Charge bonus: ×2.0 (cavalry on first attack after moving 3+ tiles)
- Terrain defense: ×0.75 damage taken (defender in forest/fortification)
- Terrain elevation: ×1.25 damage dealt (attacker on hill)
- Formation bonus: ×1.15 damage dealt or ×0.85 damage taken (see Section 2.7)
- Side flanking: ×1.3 damage dealt (attacker strikes from side — see Section 2.7)
- Rear attack: ×2.0 damage dealt (attacker strikes from rear — see Section 2.7)

All values are rounded down to integers after all modifiers apply.

### 2.7 Flanking

Units face toward the enemy deployment zone at battle start. Attacks from different directions deal different damage:

```
         Enemy side
    ┌───┬───┬───┐
    │ F │ F │ F │   F = Front (normal damage — this is the arc the unit attacks into)
    ├───┼───┼───┤
    │ S │ U │ S │   S = Side / Flank (attacker deals ×1.3 damage)
    ├───┼───┼───┤
    │ R │ R │ R │   R = Rear (attacker deals ×2.0 damage)
    └───┴───┴───┘
       Player side
```

**All flanking multipliers are configurable per unit type** — stored in the unit's ScriptableObject data for easy tuning during testing. Defaults are ×1.3 side and ×2.0 rear, but a unit like the Old Guard could have reduced rear vulnerability (e.g., ×1.5) while a fragile unit like Rocket Battery could take ×2.5 from rear.

Flanking rewards players who use fast units (cavalry, hussars) to wrap around enemy formations. The severe rear penalty means positioning and facing matter — you don't just want to reach the enemy, you want to get behind them.

### 2.8 Formation Bonuses

When specific unit arrangements are detected at the **start of each round**, bonuses apply for that round only. Formations are re-evaluated every round — if units move out of formation, the bonus is lost next round:

| Formation | Requirement | Bonus |
|-----------|-------------|-------|
| **Battle Line** | 3+ Line Infantry in a horizontal row | +15% DEF to all units in the line |
| **Artillery Battery** | 2+ Artillery adjacent to each other | +20% ATK to all artillery in the group |
| **Cavalry Wedge** | 3+ Cavalry in a diagonal line | +25% Charge damage |
| **Square** | 4 Infantry in a 2×2 block | +30% DEF vs Cavalry, cannot be flanked |
| **Skirmish Screen** | Riflemen with no adjacent friendly units | +20% ATK, +1 Range |

Formations create meaningful placement puzzles — a Battle Line is strong defensively but vulnerable to artillery AoE. A Square blocks cavalry but is a compact target. Players must read the enemy composition and choose accordingly.

### 2.9 Officers

Officers are attachable modifiers that customize individual unit behavior. They are the primary way to make two armies using the same units play differently. Officers are assigned to units in the Army Builder (see Section 2.2).

**How Officers work:**
- Each unit in an army can have **one Officer** attached (or none)
- Officers are **purchased with in-game currency** earned through campaign victories and multiplayer matches
- Each Officer provides **one positive trait and one negative trait** — there are no pure upgrades
- Officers **level up** through use (the unit they're attached to must participate in battles). Higher-level Officers have stronger effects but cost more to assign.
- Officer assignment cost scales with level: Level 1 = free, Level 2 = 1 point from army budget, Level 3 = 2 points, etc.

**Officer Roster:**

| Officer | Positive Trait | Negative Trait | Best On |
|---------|---------------|----------------|---------|
| **Veteran Sergeant** | +20% ATK | -1 MOV | Slow units (Infantry, Artillery) |
| **Young Lieutenant** | +2 MOV | -15% DEF | Fast units needing more reach |
| **Drillmaster** | +25% DEF | -20% ATK | Tanks holding a position |
| **Sharpshooter** | +1 RNG | -15% HP | Ranged units wanting more reach |
| **Fearless Major** | Immune to morale effects, +10% ATK when flanked | Always targets nearest (overrides unit AI) | Aggressive front-line units |
| **Cautious Colonel** | +30% DEF when HP below 50% | Will not advance past row 5 (holds position) | Defensive line holders |
| **Reckless Captain** | +40% Charge damage | Takes +25% damage from all sources | Cavalry glass cannon builds |
| **Siege Expert** | +30% ATK vs units in Fortifications | -2 MOV (minimum 1) | Artillery and Grenadiers |
| **Scout Master** | Reveals hidden enemy units within 3 tiles (fog of war) | -10% ATK, -10% DEF | Hussars and light cavalry |
| **Rally Officer** | Adjacent friendly units gain +10% ATK | Officer's unit has -20% HP | Support units in formation |
| **Ironside** | -50% flanking damage taken (side and rear) | -1 SPD (acts later in initiative) | High-value targets needing protection |
| **Powder Monkey** | +25% AoE radius (Artillery/Rocket Battery) | +15% chance of friendly fire splash | Artillery that wants maximum coverage |

**Officer Leveling:**
- Level 1 → 2: 5 battles participated
- Level 2 → 3: 15 battles participated (20 total)
- Level 3 → 4: 30 battles participated (50 total)
- Level 4 → 5 (max): 50 battles participated (100 total)

At each level, both the positive and negative traits scale by +10% of their base value. A Level 5 Veteran Sergeant has +28% ATK but -1.4 MOV (rounded to -1 MOV, with the .4 reducing movement speed within a tile).

**Officers in Multiplayer:** Officers are available in multiplayer. Their assignment cost (deducted from army budget) prevents spamming high-level Officers on every unit. A Level 5 Officer costs 4 army points to assign — a meaningful investment from a 40-point budget.

---

## 3. Units

### 3.1 Unit Stats

Every unit has these stats:

| Stat | Description |
|------|-------------|
| **HP** | Hit points. Unit dies at 0. |
| **ATK** | Attack damage per hit. |
| **DEF** | Damage reduction. Reduces incoming damage by DEF/2. |
| **SPD** | Speed. Determines initiative order and movement per round. |
| **RNG** | Range. How many tiles away the unit can attack. 1 = melee. |
| **MOV** | Movement. Tiles the unit can move per round. |
| **COST** | Deployment cost in army points. |

### 3.2 Complete Unit Roster

#### Tier 1 — Starting Units (Available from Battle 1)

**Line Infantry**
The backbone of any army. Reliable, affordable, and strong in formation.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 30 | 8   | 6   | 3   | 1   | 2   | 3    |

- Targeting: Nearest
- Ability: None
- Formation: Battle Line (+15% DEF when 3+ in a row)
- Historical basis: British Redcoats, French Line Infantry

**Militia**
Cheap and expendable. Deploy in numbers to screen your valuable units.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 18 | 5   | 3   | 4   | 1   | 2   | 1    |

- Targeting: Nearest
- Ability: **Strength in Numbers** — +1 ATK for each adjacent Militia (max +3)
- Formation: None
- Historical basis: Citizen soldiers, levied conscripts

#### Tier 2 — Early Unlocks (Battles 3–8)

**Cavalry** (Unlocked: Battle 3)
Fast flankers that hit hard on the charge.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 25 | 10  | 4   | 6   | 1   | 4   | 5    |

- Targeting: Highest Threat
- Ability: **Charge** — First attack after moving 3+ tiles deals ×2 damage
- Formation: Cavalry Wedge (+25% Charge damage when 3+ in diagonal)
- Historical basis: Light Dragoons

**Artillery** (Unlocked: Battle 5)
Devastating at range but helpless up close.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 15 | 14  | 2   | 1   | 4   | 1   | 6    |

- Targeting: Nearest
- Ability: **Bombardment** — Attacks hit the target tile AND all 4 orthogonally adjacent tiles for 50% damage
- Formation: Battery (+20% ATK when 2+ Artillery adjacent)
- Historical basis: 12-pounder field cannons

**Grenadier** (Unlocked: Battle 8)
Elite heavy infantry. Expensive but tough.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 40 | 12  | 8   | 2   | 1   | 2   | 7    |

- Targeting: Nearest
- Ability: **Grenade** — On first combat round, deals 5 damage to all enemies within 2 tiles
- Formation: Battle Line (counts as Line Infantry for formation purposes)
- Historical basis: Grenadier Guards, Old Guard Grenadiers

#### Tier 3 — Mid-Campaign Unlocks (Battles 10–17)

**Rifleman** (Unlocked: Battle 10)
Precision shooters who pick off key targets.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 20 | 11  | 3   | 5   | 3   | 2   | 5    |

- Targeting: Weakest
- Ability: **Aimed Shot** — If the Rifleman didn't move this round, +50% ATK
- Formation: Skirmish Screen (+20% ATK, +1 Range when no adjacent friendlies)
- Historical basis: 95th Rifles, Baker rifle skirmishers

**Hussar** (Unlocked: Battle 12)
Lightning-fast light cavalry that harasses and disrupts.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 20 | 7   | 3   | 8   | 1   | 5   | 4    |

- Targeting: Weakest
- Ability: **Hit and Run** — After attacking, moves 2 tiles away from the target
- Formation: Cavalry Wedge (counts as Cavalry)
- Historical basis: Hungarian Hussars, French Hussars

**Cuirassier** (Unlocked: Battle 15)
Armored heavy cavalry. Devastating charge, slow to reposition.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 35 | 13  | 7   | 4   | 1   | 3   | 8    |

- Targeting: Highest Threat
- Ability: **Armored Charge** — Charge (like Cavalry) AND takes 50% less damage on the round it charges
- Formation: Cavalry Wedge (counts as Cavalry)
- Historical basis: French Cuirassiers, heavy armored cavalry

**Horse Artillery** (Unlocked: Battle 17)
Mobile guns. Less powerful than field artillery but can reposition.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 12 | 10  | 2   | 5   | 3   | 3   | 6    |

- Targeting: Nearest
- Ability: **Limbered Up** — Can move and attack in the same round (regular artillery cannot)
- Formation: Battery (counts as Artillery)
- Historical basis: Royal Horse Artillery

#### Tier 4 — Late Campaign Unlocks (Battles 19–28)

**Sapper** (Unlocked: Battle 19)
Combat engineers who reshape the battlefield.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 22 | 6   | 5   | 3   | 1   | 2   | 4    |

- Targeting: Nearest
- Ability: **Entrench** — Instead of attacking, creates a Fortification on an adjacent empty tile. Fortifications grant +30% DEF to any unit on them.
- Formation: None
- Historical basis: Royal Engineers, combat pioneers

**Old Guard** (Unlocked: Battle 22)
Napoleon's elite. The best infantry in the game.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 45 | 14  | 10  | 3   | 1   | 2   | 10   |

- Targeting: Nearest
- Ability: **Unbreakable** — Cannot be routed. When HP drops below 25%, gains +25% ATK (last stand)
- Formation: Battle Line (counts as Line Infantry)
- Historical basis: Napoleon's Imperial Old Guard

**Rocket Battery** (Unlocked: Battle 24)
Unpredictable but terrifying area denial.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 10 | 16  | 1   | 2   | 5   | 1   | 7    |

- Targeting: Random
- Ability: **Congreve Barrage** — Hits a random 3×3 area near the target. High damage but inaccurate — may hit friendly units in the blast zone. The targeting AI does not account for friendly fire; the 3×3 zone is selected randomly within range of the intended target.
- Formation: None
- Historical basis: Congreve rocket system, famously inaccurate

**Lancer** (Unlocked: Battle 26)
Anti-cavalry specialist with reach.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 28 | 11  | 5   | 5   | 1   | 3   | 5    |

- Targeting: Highest Threat
- Ability: **Brace** — When charged by cavalry, attacks first and deals ×1.5 damage
- Formation: Cavalry Wedge (counts as Cavalry)
- Historical basis: Polish Lancers, Vistula Uhlans

**Dragoon** (Unlocked: Battle 28)
Mounted infantry that can fight in two modes.

| HP | ATK | DEF | SPD | RNG | MOV | COST |
|----|-----|-----|-----|-----|-----|------|
| 28 | 9   | 5   | 5   | 1   | 4   | 6    |

- Targeting: Artillery First
- Ability: **Dismount** — Automatically dismounts at the end of any round where the Dragoon made a melee attack. This is a permanent one-time state change: loses MOV (becomes 2) but gains +3 DEF and +2 ATK for the rest of the battle
- Formation: Cavalry Wedge while mounted, Battle Line while dismounted
- Historical basis: Dragoon regiments, originally mounted infantry

### 3.3 Unit Balance Philosophy

The roster follows a rock-paper-scissors triangle with nuances:

```
        Infantry
        (holds ground)
       /            \
      /    beats ↓    \
     /                 \
Cavalry ←—— beats ———— Artillery
(flanks)               (bombards)
```

- **Infantry** beats **Cavalry** — lines hold, squares counter charges
- **Cavalry** beats **Artillery** — fast enough to close the distance and overwhelm
- **Artillery** beats **Infantry** — outranges and bombards slow-moving formations

Every unit outside these three archetypes adds a wrinkle: Riflemen outrange most infantry but are fragile. Hussars beat artillery but can't break a line. Sappers change the terrain itself. The goal is that no single composition dominates — every army has a counter.

### 3.4 Army Point Budget

Each battle has a **point budget** that limits how many and which units you can deploy:

- **Campaign:** Budget varies per battle (starts at 10, scales to 50 by endgame)
- **Multiplayer:** Fixed at **40 points** (standard ranked) with alternative formats at 25 (skirmish) and 60 (grand battle)

### 3.5 Multiplayer Unit Availability

Multiplayer units are gated by **Star General tiers**, not campaign progress (see Section 7.1). New players start with the 4 core unit archetypes (Infantry, Militia, Cavalry, Artillery) and unlock specialist units by winning matches at their current tier. Tier 1 deliberately includes all three points of the rock-paper-scissors triangle (Infantry/Cavalry/Artillery) so that competitive play is strategically complete from the start. Higher tiers add complexity and specialist roles, not fundamental capabilities. Campaign progress has no effect on multiplayer unit availability, and vice versa — the two progressions are fully independent.

---

## 4. Terrain

### 4.1 Terrain Types

Terrain tiles are placed on the grid before deployment. In campaign mode, terrain is pre-designed per battle. In multiplayer, terrain is generated from a pool of balanced map templates.

| Terrain | Movement Cost | Combat Effect | Visual |
|---------|--------------|---------------|--------|
| **Open Field** | 1 (normal) | No modifier | Green grass tile |
| **Forest** | 2 (costs extra MOV) | Defender takes 25% less damage. Blocks line of sight for ranged units. | Dark green trees |
| **Hill** | 2 | Attacker on hill deals 25% more ranged damage. +1 Range for ranged units on hill. | Elevated brown terrain |
| **River** | 3 (major obstacle) | Unit crossing a river cannot attack on the round it crosses | Blue water tile |
| **Bridge** | 1 | No combat modifier. Only way to cross river at normal speed. | Stone bridge over river |
| **Fortification** | 1 | Defender takes 30% less damage. Blocks cavalry charge bonus. Can be pre-placed on maps or created mid-battle by Sappers. | Stone walls/earthworks |
| **Mud** | 2 | Cavalry lose charge bonus when moving through mud | Dark brown tile |
| **Town** | 1 | Defender takes 20% less damage. Blocks line of sight. | Buildings and rooftops |

### 4.2 Line of Sight

Ranged units (Artillery, Riflemen, Horse Artillery, Rocket Battery) require **line of sight** to their target. A straight line is drawn from attacker to target tile — if it passes through a Forest or Town tile occupied by any unit, the shot is blocked.

Exceptions:
- **Hill** — Ranged units on a hill can fire over one intervening obstacle
- **Rocket Battery** — Ignores line of sight (fires in an arc)
- **Artillery Bombardment** — The AoE splash still hits even if some splash tiles are behind cover

### 4.3 Terrain in Campaign vs Multiplayer

- **Campaign:** Terrain is hand-crafted for each battle to create distinct tactical puzzles (a river crossing, a hill defense, a town siege, etc.)
- **Multiplayer:** Maps are selected from a pool of ~20 balanced templates. Both players see the same terrain. Map rotates weekly in ranked to keep the meta fresh.

---

## 5. Commander Abilities

### 5.1 Overview

Each army has one **Commander** — a passive leader who provides a single-use ability during battle. The Commander is not a unit on the grid. Instead, their ability triggers once per battle.

Players choose one Commander before each battle.

**Trigger types:**
- **Manual** — During the deployment phase, the player sets which round (1–10) the ability activates. Once battle begins, it fires automatically on that round. This is a pre-commitment — you cannot change the trigger mid-battle.
- **Automatic** — Triggers when its condition is met (specific round or HP threshold). No player input needed.

### 5.2 Commander Roster

**Wellington — "Hold the Line"**
- Trigger: Manual (player sets which round it activates, 1–5)
- Effect: All infantry gain +30% DEF for 2 rounds
- Strategy: Survive an artillery bombardment or cavalry charge

**Napoleon — "Vive l'Empereur"**
- Trigger: Manual
- Effect: All units gain +20% ATK and +1 MOV for 2 rounds
- Strategy: All-out push to overwhelm the enemy

**Kutuzov — "Strategic Patience"**
- Trigger: Automatic (activates on round 8)
- Effect: All units heal 25% of max HP
- Strategy: Rewards long, attrition-based armies

**Blücher — "Forward, March!"**
- Trigger: Automatic (activates round 1)
- Effect: All cavalry gain +2 MOV and guaranteed charge on first attack
- Strategy: Cavalry rush — break their line before they set up

**Moore — "Rearguard Action"**
- Trigger: Automatic (activates when you lose 50% of your units)
- Effect: All remaining units gain +40% ATK and +20% DEF for the rest of the battle
- Strategy: Comeback mechanic — get stronger as you lose

**Ney — "The Bravest of the Brave"**
- Trigger: Manual
- Effect: Choose one unit — it takes two actions this round (move + attack twice or move twice + attack)
- Strategy: Surgical strike on a key target

### 5.3 Commander Unlocks

- Wellington and Napoleon: Available from start
- Kutuzov: Unlocked at Battle 7
- Blücher: Unlocked at Battle 12
- Moore: Unlocked at Battle 18
- Ney: Unlocked at Battle 24

---

## 6. Campaign

### 6.1 Structure

The campaign consists of **30 battles** across **3 acts**. Each act introduces a new phase of the Napoleonic Wars and serves as a vehicle for introducing new units, mechanics, and terrain types.

Between battles, the player sees a campaign map with nodes. The path is mostly linear with occasional branching choices (pick one of two battles — the other is skipped, offering different rewards).

### 6.2 Progression Systems

**Unit Unlocks:** New unit types unlock at specific battles (see Section 3.2). The first battle after an unlock always features a scenario designed to showcase the new unit.

**Army Budget:** Increases gradually across the campaign, letting players field larger and more diverse armies.

**Battle Rewards:** After each battle, the player earns a star rating (1–3 stars) based on performance:
- **1 star** — Victory (any win)
- **2 stars** — Decisive victory (win with 50%+ of your units surviving)
- **3 stars** — Flawless (win with all units surviving)

Stars are cosmetic and serve as a completionist goal — they do not gate progression. All campaign progression requires only a 1-star victory. In future post-launch updates, total stars earned could unlock cosmetic rewards (unit skins, grid themes).

**Difficulty Curve:** Enemy compositions grow more sophisticated — early enemies use simple massed infantry, late enemies exploit formations, terrain, and combined arms.

**Fog of War:** Some campaign battles (notably Battle 14) use fog of war. In these battles, enemy unit types and placement are completely hidden until the battle begins. The player must build a flexible army that can handle unknown threats. This is distinct from the Marshal difficulty setting (which always hides placement) — fog of war battles hide everything regardless of difficulty.

### 6.3 Act 1 — The Rising Storm (Battles 1–10)

Setting: The early wars of the French Revolution transitioning into the Napoleonic era. The player commands a fledgling army learning the fundamentals.

| Battle | Name | Budget | Terrain | Unlocks | Teaching Focus |
|--------|------|--------|---------|---------|----------------|
| 1 | First Muster | 10 | Open field | — | Basic placement and auto-battle |
| 2 | The Crossroads | 12 | Open + Forest | — | Terrain: forest defense bonus |
| 3 | Cavalry Arrives | 14 | Open field | Cavalry | Using fast units to flank |
| 4 | Hold the Ridge | 14 | Hills | — | Terrain: elevation advantage |
| 5 | Under the Guns | 16 | Open + Hill | Artillery | Ranged bombardment, line of sight |
| 6 | River Crossing | 16 | River + Bridge | — | Terrain: rivers and chokepoints |
| 7 | The Long Game | 18 | Forest + Hill | Kutuzov | Attrition strategy, commander abilities |
| 8 | Storm the Walls | 18 | Fortification + Town | Grenadier | Fortification assault, grenade ability |
| 9 | Ambush! | 20 | Dense Forest | — | Enemy uses flanking and terrain |
| 10 | Battle of the Pass | 22 | Narrow (river on both sides) | Rifleman | Long-range precision, choke defense |

### 6.4 Act 2 — The Grand Campaign (Battles 11–20)

Setting: The height of Napoleon's empire. Larger battles, more complex terrain, and enemies that use combined arms effectively.

| Battle | Name | Budget | Terrain | Unlocks | Teaching Focus |
|--------|------|--------|---------|---------|----------------|
| 11 | Open Plains | 24 | Wide open | — | Large-scale combined arms |
| 12 | The Vanguard | 26 | Open + Mud | Hussar, Blücher | Hit-and-run, cavalry rush |
| 13 | Siege of the Fort | 26 | Heavy fortification | — | Breaking entrenched defenders |
| 14 | The Fog of War | 28 | Forest + River | — | Fog of war: hidden enemy placement |
| 15 | Heavy Horse | 28 | Open + Hill | Cuirassier | Armored charge, heavy cavalry tactics |
| 16 | Town Fight | 30 | Dense Town | — | Urban combat, close quarters |
| 17 | Running Battle | 30 | Varied | Horse Artillery | Mobile artillery repositioning |
| 18 | Rearguard | 24 | River + Bridge + Forest | Moore | Comeback mechanic, fighting outnumbered (budget reduced from 32 to 24) |
| 19 | Dig In | 32 | Open + River | Sapper | Creating fortifications mid-battle |
| 20 | The Grand Battery | 35 | Hill + Open | — | Massive artillery duel, counter-battery |

### 6.5 Act 3 — The Final Act (Battles 21–30)

Setting: The decline and last campaigns. The player faces the toughest challenges, smartest AI, and must master every system.

| Battle | Name | Budget | Terrain | Unlocks | Teaching Focus |
|--------|------|--------|---------|---------|----------------|
| 21 | Winter March | 35 | Mud + River | — | Terrain penalty gauntlet |
| 22 | The Emperor's Guard | 38 | Open + Hill | Old Guard | Elite infantry, last stand mechanic |
| 23 | Desperate Defense | 38 | Fortification | — | Outnumbered survival |
| 24 | Rockets' Red Glare | 40 | Open field | Rocket Battery, Ney | Unpredictable AoE, surgical strikes |
| 25 | The Hornet's Nest | 40 | Dense Forest + Town | — | Complex terrain puzzle |
| 26 | Lancer's Charge | 42 | Open + Mud | Lancer | Anti-cavalry tactics |
| 27 | All Guns Blazing | 42 | Hill + Fortification | — | Full roster mastery |
| 28 | The Versatile Reserve | 45 | Varied | Dragoon | Multi-role units, adaptability |
| 29 | Eve of Battle | 48 | Campaign's most complex map | — | Everything combined |
| 30 | Waterloo | 50 | Iconic recreation | — | Final exam — the ultimate battle |

### 6.6 Difficulty Settings

Difficulty is selected **per-campaign** when starting a new campaign. The player commits to a difficulty for the full run, but can start a separate campaign at a different difficulty without erasing their existing save.

| Difficulty | Enemy Stat Modifier | AI Behavior | Info Shown |
|------------|-------------------|-------------|------------|
| **Recruit** | -15% all stats | Basic (nearest target, no formation) | Full enemy army visible |
| **Veteran** | No modifier | Standard (uses formations, targets wisely) | Enemy unit types visible, placement hidden until battle |
| **Marshal** | +15% all stats | Advanced (counter-picks player composition) | Only unit count visible |

---

## 7. Multiplayer

### 7.1 Star General Tier System

Multiplayer progression is divided into **Star General tiers**. Each tier unlocks access to additional units. Players compete within their tier or any lower tier they've unlocked, but cannot use units from tiers above their current rank.

| Tier | Name | Wins Required | Units Available | Badge |
|------|------|--------------|-----------------|-------|
| **1-Star** | Brigadier | 0 (start) | Line Infantry, Militia, Cavalry, Artillery (4 units) | ★ Bronze |
| **2-Star** | Major General | 10 wins in Tier 1 | + Grenadier, Rifleman (6 units) | ★★ Silver |
| **3-Star** | Lieutenant General | 20 wins in Tier 2 | + Hussar, Cuirassier, Horse Artillery (9 units) | ★★★ Gold |
| **4-Star** | General | 30 wins in Tier 3 | + Sapper, Lancer, Dragoon (12 units) | ★★★★ Platinum |
| **5-Star** | Marshal of the Empire | 50 wins in Tier 4 | + Old Guard, Rocket Battery (14 units — full roster) | ★★★★★ Diamond |

**Key rules:**
- Players can **compete in any tier they've unlocked** — a 4-Star player can queue for a Tier 2 match but can only use Tier 2 units
- Matchmaking pairs players **within the same tier queue** — no cross-tier matches
- Each tier has its own **Elo rating** — a player has separate ratings for each tier they're active in
- Playing in lower tiers does not progress higher tier win counts — you must play at your frontier tier to advance
- Officers are available at all tiers

### 7.2 Elo Rating (Per Tier)

Each tier uses independent **Elo ratings** starting at 1000.

| Rank | Elo Range | Display |
|------|-----------|---------|
| Recruit | 0–999 | Bronze musket |
| Corporal | 1000–1199 | Silver musket |
| Sergeant | 1200–1399 | Bronze sword |
| Lieutenant | 1400–1599 | Silver sword |
| Captain | 1600–1799 | Bronze eagle |
| Colonel | 1800–1999 | Silver eagle |
| General | 2000–2199 | Gold eagle |
| Grand Marshal | 2200+ | Napoleon's bicorne hat |

### 7.3 Army Pool System

Instead of traditional 1v1 matchmaking, multiplayer uses an **army pool** system:

```
Player builds army in Multiplayer Army Builder
        ↓
Spends 1 Ammunition to deploy army into the pool
        ↓
Army sits in the pool for the tier it's submitted to
        ↓
Server matches armies in the pool against each other
(same tier, similar Elo, same map rotation)
        ↓
Battle resolves deterministically
        ↓
Both players receive replay notification + Elo update
```

**How it works:**
- A player can have **multiple armies deployed in the pool simultaneously**
- Each deployment costs **1 Ammunition** (see Section 8)
- Armies remain in the pool until they fight — typically matched within hours, faster at popular tiers
- Players can withdraw an unmatched army (ammunition is refunded)
- After a battle, the army is removed from the pool — deploy again to fight again

**Why a pool?** This creates a richer metagame. Instead of submitting one army and waiting, players can deploy several armies with different strategies across different tiers. It also creates the monetization hook (ammunition) without affecting game balance.

### 7.4 Match Formats

| Format | Point Budget | Tier | Description |
|--------|-------------|------|-------------|
| **Skirmish** | 25 | Any | Quick, small armies. Good for learning. Unranked. |
| **Standard** | 40 | Tier-specific | Default ranked format. |
| **Grand Battle** | 60 | Tier 4+ only | Large armies, long battles. Ranked. |

### 7.5 Anti-Cheat

- Server validates army composition (correct point totals, only tier-appropriate units, valid Officer assignments)
- Battle resolution uses **deterministic seeded RNG** — same seed + same armies = same result. Server resolves, not client.
- Army submissions are locked after deployment — no edits
- Ammunition balance validated server-side

### 7.6 Map Rotation

Ranked play uses a pool of ~20 balanced map templates. **3 maps are active per week**, rotating on Monday. This keeps the meta evolving and prevents stale strategies. All tiers use the same map rotation.

---

## 8. Monetization

**Core principle: No pay-to-win. No paying to unlock units faster.** Units are earned through play. Monetization funds the game without giving paying players a competitive advantage.

### 8.1 Game Purchase Model

**Campaign:**
- **Act 1 (Battles 1–10) is free** — serves as a demo and tutorial
- **Full Campaign (Acts 2–3, Battles 11–30) is a one-time purchase** — this is the primary revenue source
- Price: $4.99–$9.99 (TBD based on market research)
- Purchasing the campaign does NOT unlock multiplayer units faster — tier progression is play-based only

**Multiplayer:**
- Free to play at all tiers
- Monetization through Ammunition and cosmetics (see below)

### 8.2 Ammunition System

**Ammunition** is the currency spent to deploy armies into the multiplayer pool (see Section 7.3).

**Earning Ammunition for free:**
- 3 free Ammunition per day (daily login reward)
- 1 bonus Ammunition per multiplayer win
- 2 Ammunition per campaign battle completed (first clear only)
- Completing a tier promotion (e.g., reaching 3-Star) grants 10 bonus Ammunition

**Purchasing Ammunition:**
- 10 Ammunition — $0.99
- 30 Ammunition — $1.99
- 100 Ammunition — $4.99

**Why this works:** Casual players who deploy 1–3 armies per day never need to spend money — the free daily Ammunition covers them. Heavy players who want to run many armies simultaneously across multiple tiers may choose to buy more. Crucially, Ammunition does not affect battle outcomes — it only controls how many armies you have in the pool at once. A free player's army is exactly as powerful as a paying player's army.

### 8.3 Reward Boxes

When a player earns a new rank (Elo milestone) or achievement, they receive a **Dispatch Box** — a symbolic container they tap to open, revealing their reward with a satisfying animation.

**Dispatch Box contents (cosmetic only):**
- Rank-up boxes: New rank badge + random cosmetic item
- Achievement boxes: Themed cosmetic matching the achievement
- Tier promotion boxes: Unique tier badge + 10 Ammunition + exclusive tier cosmetic

**The box opening is ceremonial** — there's no randomness in what you get for rank/tier promotions (always the badge + known reward). Achievement boxes can contain one item from a small pool of themed cosmetics.

### 8.4 Cosmetic Items

All cosmetics are visual only and never affect gameplay:

- **Unit skins** — Alternative pixel art (Austrian uniforms, Prussian blue, Russian green, Ottoman gold)
- **Grid themes** — Alternative board visuals (winter snow, Egyptian desert, Mediterranean coast)
- **Commander portraits** — Stylized pixel art commander images
- **Victory animations** — Custom battle-end celebrations (fireworks, cavalry parade, cannon salute)
- **Army banners** — Custom flag displayed on your deployment zone

**Acquisition:** Cosmetics are earned through Dispatch Boxes (free) or purchased individually from a rotating shop ($0.99–$2.99 per item). No loot boxes with real money — you always know what you're buying.

### 8.5 Revenue Summary

| Source | Type | Expected Share |
|--------|------|---------------|
| Campaign unlock (Acts 2–3) | One-time purchase | Primary |
| Ammunition packs | Consumable | Secondary |
| Cosmetic shop | One-time per item | Tertiary |

This model respects players: the full single-player experience has a clear price, multiplayer is free to enjoy casually, and spending money never makes your army stronger.

---

## 9. Audio

### 9.1 Sound Effects

Each action needs a distinct audio cue:
- Unit placement: Light thud / stamp
- Battle start: Drum roll
- Infantry attack: Musket volley
- Cavalry charge: Hoofbeats crescendo into clash
- Artillery fire: Cannon boom with echo
- Unit death: Brief death cry + collapse
- Victory: Triumphant brass fanfare
- Defeat: Somber drum and fife

### 9.2 Music

Era-appropriate orchestral music, sourced from free libraries:
- **Menu:** Stately, dignified (think period drama)
- **Army building:** Thoughtful, measured tempo
- **Battle:** Dynamic, escalating intensity
- **Victory:** Triumphant brass
- **Defeat:** Melancholic strings

---

## 10. UI Screens

### 10.1 Screen Flow

```
Main Menu
  │
  ├── Armory (army management hub)
  │     ├── Campaign Army Builder (units limited by campaign progress)
  │     │     ├── Create New Army (select units, place on grid rows 1–3, assign Officers, pick Commander)
  │     │     ├── Saved Armies List (load, edit, duplicate, delete)
  │     │     └── Unit Codex (browse all campaign units, stats, lore, unlock requirements)
  │     │
  │     └── Multiplayer Army Builder (all tier-appropriate units available)
  │           ├── Create New Army (same flow, but budget fixed per format)
  │           ├── Saved Armies List
  │           ├── Officer Management (view, assign, see levels)
  │           └── Unit Codex (all units, grayed out for locked tiers)
  │
  ├── Campaign
  │     ├── Act Selection (Act 1 free, Acts 2–3 behind purchase)
  │     ├── Campaign Map (node selection within act)
  │     ├── Deployment Screen (select saved army → review terrain → adjust placement → deploy)
  │     ├── Battle Viewer (auto-resolution, speed controls, skip option)
  │     └── Results (star rating, unlocks, Dispatch Box if applicable)
  │
  ├── Multiplayer
  │     ├── Tier Selection (play at current tier or any unlocked lower tier)
  │     ├── Army Pool
  │     │     ├── Deploy Army (select saved army → spend 1 Ammunition → submit to pool)
  │     │     ├── Active Deployments (armies currently in the pool, withdraw option)
  │     │     └── Ammunition Balance (current count + purchase option)
  │     ├── Battle Replays (watch completed matches)
  │     ├── Match History (W/L record, Elo changes)
  │     └── Tier Progress (wins toward next tier, current tier stats)
  │
  ├── Leaderboard (per-tier rankings, Elo display)
  │
  ├── Dispatch Boxes (pending unopened boxes, opening animation)
  │
  ├── Cosmetics Shop (rotating items, equipped skins/themes)
  │
  ├── Profile (rank badges, achievements, stats)
  │
  └── Settings
        ├── Audio (music/SFX volume)
        ├── Graphics (quality, screen shake toggle)
        ├── Accessibility (colorblind mode, text size)
        └── Account (login, cloud save)
```

### 10.2 Key UI Principles

- **Information density:** Show unit stats on hover/tap. Don't clutter the grid.
- **Pixel art UI:** Buttons, panels, and frames match the pixel art aesthetic.
- **Mobile-first layout:** Design for phone screens, scale up for tablet/PC.
- **One-thumb playable:** On mobile, critical interactions should work with one hand.
- **Color coding:** Each unit type has a distinct silhouette AND color so they're readable at small sizes and for colorblind players.

---

## 11. Technical Requirements

### 11.1 Deterministic Battle Engine

The single most important technical requirement. For async multiplayer to work, the **same inputs must always produce the same outputs**. This means:
- No floating point math in combat calculations — use integers only
- All randomness seeded with a shared seed
- Turn order resolution must be deterministic (stable sort by Speed, with a tie-breaking rule based on unit ID)

### 11.2 Save System

- Campaign progress saved locally (JSON or Unity's PlayerPrefs for simple data)
- Army compositions saved as serialized data for multiplayer submission
- Cloud save optional (can use PlayFab player data if implemented)

### 11.3 Performance Targets

| Platform | Target FPS | Max Battle Duration |
|----------|-----------|-------------------|
| PC | 60 | No limit |
| iOS | 60 | 30 rounds |
| Android | 60 | 30 rounds |

The game is a 2D grid with pixel art sprites — performance should not be an issue on any modern device.

---

## 12. AI QA Balance Tester

### 12.1 Purpose

A developer-only tool (not player-facing) that automatically builds armies and battles them against each other to identify balance issues before they reach players. This tool runs outside of the game client as a headless simulation using the same deterministic battle engine.

### 12.2 How It Works

The AI QA Tester operates in three modes:

**Mode 1 — Exhaustive Matchup Testing**
- Generates all meaningful unit pair matchups (Unit A vs Unit B, equal cost)
- Runs each matchup 1,000 times with varied terrain and positioning
- Outputs a **win-rate matrix** showing every unit vs every unit
- Flags any matchup where one side wins >70% of the time at equal cost

**Mode 2 — Army Composition Stress Test**
- AI builds random valid armies at a given budget (e.g., 40 points)
- Battles thousands of random armies against each other
- Identifies compositions that dominate (>60% win rate across all opponents)
- Identifies units that never appear in winning armies (underpowered) or appear in >80% of winning armies (overpowered)
- Tests each multiplayer tier independently (Tier 1 with 4 units, Tier 2 with 6, etc.)

**Mode 3 — Officer Impact Analysis**
- Runs Mode 2 twice: once without Officers, once with Officers enabled
- Compares results to ensure no Officer creates a dominant strategy
- Flags any Officer that increases a unit's win rate by more than 15%

### 12.3 AI Army Builder

The AI builds armies using weighted random selection:

1. **Random composition** — Randomly selects units until budget is spent (fully random, no strategy)
2. **Archetype-based** — Builds around a strategy: "cavalry rush," "artillery fort," "balanced line," "infantry wall"
3. **Counter-picking** — Given an opponent army, builds the best counter (used to test if any army is uncounterable)
4. **Meta-gaming** — Uses historical win-rate data from previous test runs to build the statistically strongest army

All four builder strategies are tested against each other to ensure variety is viable.

### 12.4 Output Reports

The tester generates reports as spreadsheets:

- **Unit Matchup Matrix** — 14×14 grid showing win rates for every unit pair
- **Composition Win Rates** — Top 20 best and worst performing army compositions
- **Officer Impact Report** — Win rate delta per Officer per unit type
- **Tier Balance Report** — Per-tier analysis showing if restricted unit pools are balanced
- **Terrain Bias Report** — Whether certain maps favor certain strategies disproportionately
- **Anomaly List** — Any result flagged as a potential balance issue with suggested adjustments

### 12.5 When to Run

- After any unit stat change
- After adding a new unit or Officer
- After terrain/map changes
- Before each version release
- On a weekly schedule during active development

### 12.6 Implementation Note

The AI QA Tester reuses the same battle engine code as the game — it's not a separate simulation. This guarantees that test results match actual gameplay. It runs headless (no rendering, no UI) for speed. Target: 10,000 battles per minute on a standard development machine.

---

## 13. Future Content (Post-Launch)

Ideas for post-launch updates, not in scope for v1:
- **Additional campaigns** (Egyptian campaign, Peninsular War, Russian campaign) — sold as expansion packs
- **Challenge modes** (win with only cavalry, survive 50 rounds, etc.)
- **Weekly tournaments** with unique rules and cosmetic prizes
- **Map editor** for community-created maps
- **2v2 async multiplayer** (four-player battles)
- **Historical scenarios** with fixed armies recreating real battles
- **New Officers** — expand the Officer roster with seasonal additions
- **6-Star tier** — ultimate prestige rank with unique cosmetic rewards

---

*This document is version 2.0. Updated to include: Officers system, Star General tier progression, army pool multiplayer, ammunition monetization, separated army builder/deployment, configurable deployment zones, AI QA balance tester, and revised screen flow.*
