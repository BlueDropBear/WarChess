# WarChess — Post-Launch Unit Designs

## Overview

These 3 units are designed for the first post-launch content update. They introduce new tactical niches not covered by the existing 14-unit roster, add variety to multiplayer meta, and provide fresh strategic options for veteran players.

---

## Unit 1: Sharpshooter Officer

| Stat | Value |
|------|-------|
| **Name** | Jäger |
| **Era Reference** | Austrian/Prussian Jäger light infantry |
| **Role** | Stealth sniper — high damage, low survivability |
| **Cost** | 6 |
| **HP** | 18 |
| **ATK** | 14 |
| **DEF** | 3 |
| **SPD** | 6 |
| **RNG** | 4 |
| **MOV** | 3 |

### Ability: Concealment
- After attacking, the Jäger becomes hidden for 1 round (cannot be targeted by enemies)
- Broken if an enemy moves adjacent to the Jäger
- Synergy: Fog of War battles, Skirmish Screen formation

### Targeting Priority
- WeakestTarget — picks off damaged units

### Flanking Multipliers
- Side: ×1.5 (higher than standard ×1.3 — ambush specialist)
- Rear: ×2.5 (higher than standard ×2.0)

### Formation Interaction
- Counts as Rifleman for Skirmish Screen formation detection
- Benefits from Skirmish Screen +20% ATK and +1 RNG

### Balance Analysis
- **Strengths:** Highest range in game (4 base, 5 with Skirmish Screen), stealth mechanic makes it hard to pin down, excellent flanking damage
- **Weaknesses:** Lowest DEF in game (3), low HP (18), expensive at 6 cost, only hidden for 1 round
- **Expected matchups:** Strong vs Artillery (outranges), Sapper (snipes behind fortifications). Weak vs Hussar (fast enough to reach and break concealment), Cavalry wedge (closes gap quickly)
- **Cost efficiency:** Slightly above curve for damage output, below curve for survivability. Net neutral at 6 cost.

### Unlock
- **Campaign:** Post-launch Act 4, Battle 2
- **Multiplayer:** 3-Star Lieutenant General tier (alongside Hussar, Cuirassier, Horse Artillery)

---

## Unit 2: Supply Wagon

| Stat | Value |
|------|-------|
| **Name** | Supply Wagon |
| **Era Reference** | Napoleonic supply trains |
| **Role** | Support — heals adjacent allies each round |
| **Cost** | 4 |
| **HP** | 25 |
| **ATK** | 0 |
| **DEF** | 4 |
| **SPD** | 2 |
| **RNG** | 0 |
| **MOV** | 2 |

### Ability: Resupply
- At the start of each round, heals all adjacent friendly units for 3 HP
- Does not stack with multiple Supply Wagons on the same unit
- Cannot attack — purely a support unit

### Targeting Priority
- N/A — cannot attack. Moves toward the centroid of friendly units.

### Flanking Multipliers
- Side: ×1.0 (takes normal damage from all directions — no flanking bonus against it)
- Rear: ×1.0

### Formation Interaction
- Does NOT count toward any formation
- Does not benefit from formation bonuses

### Balance Analysis
- **Strengths:** Sustained healing (3 HP/round to up to 4 adjacent units = 12 HP/round total), high HP for a 4-cost unit, excellent in prolonged battles
- **Weaknesses:** Cannot attack (0 ATK), very slow (2 MOV, 2 SPD), takes up army budget without contributing damage, vulnerable if left unprotected
- **Expected matchups:** Strong behind a Battle Line formation (heals the line), good with Old Guard (extends their survivability). Weak vs Artillery (AoE hits wagon and units around it), Cavalry (easily flanked and killed)
- **Cost efficiency:** Zero combat value. Only efficient if the army survives 5+ additional rounds due to healing.

### Unlock
- **Campaign:** Post-launch Act 4, Battle 5
- **Multiplayer:** 2-Star Major General tier (alongside Grenadier, Rifleman)

---

## Unit 3: Uhlan

| Stat | Value |
|------|-------|
| **Name** | Uhlan |
| **Era Reference** | Polish/Prussian Uhlans (light lancers) |
| **Role** | Skirmish cavalry — hit-and-run with terrain bypass |
| **Cost** | 5 |
| **HP** | 20 |
| **ATK** | 10 |
| **DEF** | 4 |
| **SPD** | 8 |
| **RNG** | 1 |
| **MOV** | 5 |

### Ability: Pathfinder
- Ignores terrain movement cost penalties (Forest, River, Mud all cost 1 MOV)
- Does NOT ignore Fortification blocking charge
- Charge bonus still applies (×2.0 when moving 3+ tiles)

### Targeting Priority
- ArtilleryFirst — prioritizes taking out ranged threats

### Flanking Multipliers
- Side: ×1.3 (standard)
- Rear: ×2.0 (standard)

### Formation Interaction
- Counts as Cavalry for Cavalry Wedge formation detection
- Benefits from Wedge +25% charge damage

### Balance Analysis
- **Strengths:** Highest MOV in game (5, tied with potential Officer boosts), ignores terrain penalties (huge advantage on maps with rivers/mud), very high SPD (acts first), targets Artillery
- **Weaknesses:** Average ATK (10), low HP (20) and DEF (4), expensive at 5 cost for a glass cannon, Fortifications still block charge
- **Expected matchups:** Strong vs Artillery (reaches and kills quickly), Horse Artillery (outpaces). Weak vs Square formation (blocks flanking, +30% DEF vs cavalry), Cuirassier (outmatched in direct combat), Lancer (anti-cavalry specialist)
- **Cost efficiency:** On par with Cavalry (5 cost) but trades HP/DEF for terrain bypass and SPD. Situationally better on terrain-heavy maps, worse on open fields.

### Unlock
- **Campaign:** Post-launch Act 4, Battle 8
- **Multiplayer:** 4-Star General tier (alongside Sapper, Lancer, Dragoon)

---

## Integration Notes

### Code Changes Required

1. **UnitEnums.cs** — Add to `UnitType` enum: `Jager, SupplyWagon, Uhlan`
2. **UnitFactory.cs** — Add `CreateByTypeName` entries with stats above
3. **GameConfigData.cs** — Add to `GetUnitCosts()`: Jager=6, SupplyWagon=4, Uhlan=5
4. **TierSystem.cs** — Add to appropriate tier unit lists
5. **BalanceTester.cs** — Add to `AllUnitTypes` array
6. **BattleEngine.cs** — Handle Supply Wagon healing in round start, Jäger concealment in combat phase, Uhlan terrain cost override in movement phase

### New Ability Types
Add to `AbilityType` enum:
- `Concealment` — Jäger stealth after attack
- `Resupply` — Supply Wagon adjacent healing
- `Pathfinder` — Uhlan terrain cost bypass

### Balance Testing Priority
1. Run Mode 1 (17×17 matchup matrix with new units)
2. Run Mode 2 composition test — check if Supply Wagon creates degenerate healing strategies
3. Verify Jäger concealment doesn't make it untargetable indefinitely
4. Verify Uhlan isn't dominant on terrain-heavy maps (run stress test on Forest/River maps)
