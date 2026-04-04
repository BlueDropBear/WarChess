# WarChess — Game Design Review

**Reviewer:** Claude (Game Design Analysis)
**Date:** 2026-04-04
**Documents Reviewed:** GDD v2.0, Development Roadmap, Progress Tracker, Full Codebase
**Scope:** Design improvements, balance concerns, GDD errors, and code bugs

---

## Resolution Status (2026-04-04)

All issues below have been addressed. Changes are in GDD v2.1 and corresponding code commits.

| ID | Status | Resolution |
|----|--------|------------|
| BUG-1 | **FIXED** | Added `HasGuaranteedCharge` flag; Blücher now correctly grants charge without 3-tile movement |
| BUG-2 | **FALSE POSITIVE** | Grenadier/Old Guard CAN form Square — `CountsAsType = LineInfantry` works correctly. Original analysis was wrong. |
| BUG-3 | **FIXED** | `ApplyDismount()` now sets `CountsAsType = LineInfantry` for Battle Line formation. Redundant `Rng == 1` check removed. |
| DESIGN-1 | **NOTED** | Added Rocket Battery fortification bypass as soft artillery counter. Further tuning via configurable values. |
| DESIGN-2 | **FIXED** | Moore's trigger changed from "50% of units" to "50% of army budget points worth" |
| DESIGN-3 | **FIXED** | Starter Ammunition increased to 10. Added to GDD Section 8.2. |
| DESIGN-4 | **FIXED** | Veteran Sergeant changed from -1 MOV to -1 SPD in both code and GDD |
| DESIGN-5 | **FIXED** | Lancer moved from Battle 26 to Battle 20 (end of Act 2) |
| DESIGN-6 | **FIXED** | Rocket Battery cost reduced from 7 to 6; gains fortification bypass |
| DESIGN-7 | **NOTED** | Deferred to multiplayer implementation phase |
| DESIGN-8 | **FIXED** | Morale system defined as new GDD Section 2.10 |
| ERR-1 | **NOTED** | 12 officers confirmed as intended |
| ERR-2 | **FIXED** | SPD description corrected |
| ERR-3 | **FIXED** | Terrain coexistence text clarified |
| ERR-4 | **FIXED** | Wellington trigger range design note added |
| ERR-5 | **FIXED** | Lancer flavor text changed to "punishes charges" |

Additionally: Core Gameplay Loop section (1.6) added to GDD, Militia given Square formation in GDD.

---

## Executive Summary

WarChess has a strong foundation — the core loop (build army → deploy → watch battle) is clean, the Napoleonic theme is well-integrated, and the separation between army building and deployment is a smart design choice. The GDD is thorough and internally consistent for the most part.

This review identified **2 confirmed code bugs** (1 false positive), **6 design concerns**, and **8 improvement opportunities**. All have been addressed in GDD v2.1 and corresponding code changes.

---

## Part 1: Code Bugs Found

### BUG-1: Blücher's "Guaranteed Charge" Does Nothing (HIGH)

**Files:** `Assets/Scripts/Commanders/CommanderSystem.cs:150`, `Assets/Scripts/Battle/BattleEngine.cs:193`

Blücher's ability sets `unit.HasChargedThisBattle = false`, but the charge check in both BattleEngine.cs and BattleEngineV2.cs only reads `unit.HasChargedThisRound`. The `HasChargedThisBattle` field is never checked anywhere in the charge logic. This means the "guaranteed charge on first attack" part of Blücher's ability is completely non-functional — cavalry still need to move 3+ tiles to trigger a charge.

**Impact:** Blücher is advertised as the cavalry rush commander, but half his ability (the guaranteed charge) does nothing. Players using Blücher only get the +2 MOV buff.

**Fix:** Either:
- Add a `HasGuaranteedCharge` flag that bypasses the 3-tile movement requirement, OR
- Check `HasChargedThisBattle == false` as an alternative charge trigger in the damage calculation

---

### BUG-2: Grenadier Excluded from Square Formation (MEDIUM)

**File:** `Assets/Scripts/Formations/FormationDetector.cs:57`

The Square formation check uses:
```csharp
if (effectiveType == UnitType.LineInfantry || unit.Type == UnitType.Militia)
```

Grenadier has `Type = UnitType.Grenadier` and `CountsAsType = UnitType.LineInfantry`. The Battle Line check (line 72) correctly uses `effectiveType` (which is `CountsAsType`), so Grenadiers can form Battle Lines. But the Square check mixes `effectiveType` with a raw `unit.Type == UnitType.Militia` check. Since Grenadier's `Type` is not `LineInfantry` or `Militia`, it fails.

**Impact:** Grenadiers — elite heavy infantry — cannot form the defensive Square formation despite being exactly the kind of unit that historically would. Old Guard (also `CountsAsType = LineInfantry`) has the same problem.

**Fix:** Change line 57 to:
```csharp
if (effectiveType == UnitType.LineInfantry || unit.Type == UnitType.Militia)
```
should become:
```csharp
if (effectiveType == UnitType.LineInfantry)
```
Since Militia's `CountsAsType` should also be set to `LineInfantry` for formation purposes, or add Militia explicitly if it shouldn't count.

---

### BUG-3: Dragoon Always Dismounts on First Attack (MEDIUM)

**File:** `Assets/Scripts/Battle/BattleEngine.cs:223`

The dismount trigger checks `unit.Rng == 1`, but Dragoon starts with `Rng = 1` (melee). This means dismount triggers on the very first melee attack, every single time. There is no tactical choice — the Dragoon always charges once, then permanently becomes slow infantry.

**Impact:** The GDD describes Dragoons as "mounted infantry that can fight in two modes," implying a meaningful mode switch. Currently it's just an automatic first-round transformation with no player agency. The Dragoon is effectively a cavalry unit that becomes infantry after round 1.

**Design Note:** This may actually be intentional per the GDD wording ("Automatically dismounts at the end of any round where the Dragoon made a melee attack"). If so, the `unit.Rng == 1` check is redundant and should be removed for clarity. If it's meant to be conditional, the condition needs rethinking.

---

## Part 2: GDD Errors & Inconsistencies

### ERR-1: Officer Count Mismatch

The GDD Section 2.9 lists **11 officers** in the table, but the Progress tracker and code reference **12 officers**. Counting the GDD table: Veteran Sergeant, Young Lieutenant, Drillmaster, Sharpshooter, Fearless Major, Cautious Colonel, Reckless Captain, Siege Expert, Scout Master, Rally Officer, Ironside, Powder Monkey — that's actually 12. The GDD text says "Officers are attachable modifiers" but never states a count. This is fine, but worth confirming the 12 is intentional and complete.

### ERR-2: SPD vs MOV Confusion in Unit Stats

The GDD defines both **SPD** ("Speed. Determines initiative order and movement per round") and **MOV** ("Movement. Tiles the unit can move per round"). SPD's description says it determines "movement per round" which overlaps with MOV. In practice, SPD is used for initiative ordering and MOV is used for actual tile movement, but the GDD description of SPD is misleading.

**Fix:** Change SPD description to: "Speed. Determines initiative order (who acts first each round)." Remove the "and movement per round" clause.

### ERR-3: Terrain Tile Coexistence Contradiction

Section 2.1 states: "Each tile can hold one unit or one terrain feature, not both" — then immediately contradicts itself with "(units occupy terrain tiles — they coexist)." The parenthetical is correct (units stand on terrain), but the preceding sentence says otherwise.

**Fix:** Rewrite to: "Each tile has one terrain type. A tile can hold at most one unit. Units occupy terrain tiles and receive that terrain's effects."

### ERR-4: Wellington Trigger Range vs Ney

Wellington's manual trigger is listed as "round 1–5" while Napoleon and Ney are "round 1–10." The GDD doesn't explain why Wellington has a restricted trigger window. This may be intentional for balance (forcing early commitment), but it's the only commander with a restricted manual range and it's never called out as a deliberate design choice.

### ERR-5: Lancer RNG = 1 But Called "Anti-Cavalry Specialist with Reach"

The Lancer's flavor text says it has "reach" but its RNG stat is 1 (melee only). The "Brace" ability (attack first when charged by cavalry) doesn't extend range. Historically, lancers had reach advantage over sword-armed cavalry. Consider giving Lancer RNG = 2 to match the thematic promise, or change the flavor text to remove "with reach."

---

## Part 3: Game Design Improvements

### DESIGN-1: No Meaningful Counter to Artillery Stacking (HIGH)

**Problem:** Artillery has 14 ATK, 4 RNG, and Bombardment AoE. Two Artillery in Battery formation get +20% ATK. At 6 cost each, a player can fit 2 Artillery + supporting units in most budgets. The counter (cavalry rush) requires crossing 4-7 tiles of open ground while being bombarded each round.

**The Math:** Two Artillery in Battery deal ~17 ATK each per round at range 4. Cavalry (25 HP, 4 DEF) takes roughly 15 damage per hit. A cavalry unit can be killed in 2 rounds before it closes the gap on many maps, especially with terrain slowing movement.

**Suggestion:** Consider one or more of:
- Reduce Artillery Battery bonus from +20% to +10%
- Add a "reload" mechanic: Artillery fires every other round (historically accurate — reload time was significant)
- Give Hussar a "Screen" ability that reduces ranged damage to units behind it
- Ensure multiplayer map templates always include terrain that breaks artillery sightlines

---

### DESIGN-2: Moore's "Rearguard Action" May Be Too Swingy (MEDIUM)

**Problem:** Moore triggers when you lose 50% of your units and grants +40% ATK and +20% DEF **for the rest of the battle**. This creates a degenerate strategy: field many cheap Militia (cost 1 each) to die quickly, then your remaining expensive units get permanent massive buffs.

**Example:** 40-point budget → 10 Militia (10 pts) + 2 Old Guard (20 pts) + filler. Militia die fast, triggering Moore. Two Old Guard with +40% ATK and +20% DEF become nearly unkillable.

**Suggestion:** Either:
- Change "50% of units" to "50% of army points worth of units" (so cheap fodder doesn't trigger it)
- Reduce duration to 3 rounds instead of permanent
- Scale the buff based on how much value was lost (losing 50% of your *points* gives full buff, losing cheap units gives a smaller buff)

---

### DESIGN-3: Ammunition System Feels Punishing for New Players (MEDIUM)

**Problem:** New multiplayer players start with 0 Ammunition (earned only through daily login and campaign play). A player who downloads the game, finishes Act 1 (free), and wants to try multiplayer needs to either:
- Complete campaign battles for 2 Ammo each (up to 20 from Act 1)
- Wait for daily login rewards (3/day)
- Buy Ammo

The friction between "free to play multiplayer" and "costs Ammunition to deploy" could cause churn at the exact moment players are most engaged.

**Suggestion:** Give new players **10 starter Ammunition** when they first access multiplayer. This covers ~10 matches — enough to get hooked before the earn/spend loop matters. Also consider making Skirmish (25-point, unranked) free to deploy (no Ammunition cost) as a permanent on-ramp.

---

### DESIGN-4: Officer Negative Traits Vary Wildly in Severity (MEDIUM)

**Problem:** Some officer downsides are trivial while others are crippling:
- **Young Lieutenant:** -15% DEF (moderate, affects survivability)
- **Cautious Colonel:** "Will not advance past row 5" (potentially devastating — unit may never engage the enemy)
- **Powder Monkey:** "+15% chance of friendly fire splash" (can lose you the game)
- **Veteran Sergeant:** -1 MOV (nearly irrelevant on slow units like Artillery with MOV 1 → still MOV 1 due to minimum)

The Veteran Sergeant on Artillery is essentially a free +20% ATK with no downside (MOV 1 can't go lower in practice). Meanwhile, Cautious Colonel hard-locks your unit's positioning.

**Suggestion:** Ensure negative traits are roughly equivalent in impact. For Veteran Sergeant, change to "-1 SPD" (acts later in initiative) or "-10% HP" so it always has a cost. Review each officer pairing to verify the negative is meaningful on the "Best On" unit type.

---

### DESIGN-5: Campaign Pacing — Too Many Unlocks in Act 3 (LOW)

**Problem:** Act 3 (Battles 21-30) unlocks 4 unit types: Old Guard (22), Rocket Battery (24), Lancer (26), Dragoon (28). That's a new unit every 2 battles, giving players almost no time to integrate each unit before the next arrives. Meanwhile, Act 2 has better pacing with unlocks at battles 12, 15, 17, 19.

Act 3 also unlocks Ney (battle 24), meaning battle 24 gives *both* a new unit and a new commander simultaneously.

**Suggestion:** Move Lancer to battle 20 (end of Act 2) and push Sapper to battle 22. This gives Act 2 an exciting capstone unlock and reduces Act 3's density. Alternatively, add 2-3 more battles to Act 3 (making it 33 total) to space out the unlocks.

---

### DESIGN-6: Rocket Battery Risk/Reward May Not Be Worth It (LOW)

**Problem:** Rocket Battery has the highest ATK (16) and longest range (5) but only 10 HP and 1 DEF. Its ability hits a *random* 3x3 area and can damage friendlies. At 7 cost, it's expensive for something unreliable.

Compare to Artillery (14 ATK, 4 RNG, 6 cost): Artillery is cheaper, more reliable, has more HP (15), and its AoE is targeted. Rocket Battery costs 1 more point for +2 ATK and +1 RNG but trades all reliability for randomness and friendly fire risk.

**Suggestion:** Either:
- Reduce Rocket Battery cost to 5-6 to match its unreliability
- Increase the 3x3 damage to compensate for the randomness (e.g., full damage in the blast zone, not reduced)
- Give it a unique niche: "ignores Fortification defense bonus" (rockets going over walls)

---

### DESIGN-7: Map Rotation Could Create Feel-Bad Moments (LOW)

**Problem:** Section 7.6 states 3 maps rotate weekly. If a player's army strategy is heavily terrain-dependent (e.g., artillery on hills), they may have zero viable maps some weeks. With armies sitting in the pool, a map rotation change could make deployed armies suboptimal overnight.

**Suggestion:** Increase active maps to 5 per week, or allow players to select which of the 3 maps their army deploys on (adding strategic depth without removing rotation).

---

### DESIGN-8: No Morale System Despite Officer References (LOW)

**Problem:** The Fearless Major officer grants "Immune to morale effects," but there is no morale system defined anywhere in the GDD. No unit has morale, no mechanic reduces morale, and no other officer or ability references it. This is a dangling reference.

**Suggestion:** Either:
- Define a morale system (e.g., units adjacent to dying allies lose 10% ATK for 1 round — Fearless Major prevents this)
- Remove the morale immunity from Fearless Major and replace it with something that works within existing systems (e.g., "Immune to flanking ATK penalties" or "Does not lose formation bonus when an adjacent ally dies")

---

## Part 4: What's Working Well

These aspects of the design are strong and should be preserved:

1. **Separated army building and deployment** — Lets players theorycraft between sessions. Great for mobile.
2. **Army pool multiplayer** — Elegant async solution. No waiting for opponents. Multiple armies in flight creates strategic depth.
3. **Rock-paper-scissors triangle** — Infantry > Cavalry > Artillery > Infantry is clear, historically grounded, and easy to teach.
4. **Officers with mandatory downsides** — Prevents "always equip the best officer" min-maxing. Every choice has a cost.
5. **Star General tier system** — Progressively expanding the unit pool keeps multiplayer fresh at each tier. Tier 1 being strategically complete (all 3 archetypes) is crucial.
6. **Deterministic battle engine** — Integer-only math with seeded RNG is correctly implemented in code. Essential for async multiplayer integrity.
7. **Campaign difficulty via information hiding** — Recruit (full info) → Veteran (types visible) → Marshal (only count visible) is elegant and scales challenge without artificial stat inflation alone.
8. **Cosmetic-only monetization** — No pay-to-win. Ammunition controls deployment frequency, not army power. Ethical and sustainable.

---

## Priority Summary

| ID | Category | Severity | Summary |
|----|----------|----------|---------|
| BUG-1 | Code Bug | HIGH | Blücher's guaranteed charge is non-functional |
| BUG-2 | Code Bug | MEDIUM | Grenadier/Old Guard can't form Square formation |
| BUG-3 | Code Bug | MEDIUM | Dragoon dismount has no tactical decision |
| DESIGN-1 | Balance | HIGH | Artillery stacking lacks meaningful counterplay |
| DESIGN-2 | Balance | MEDIUM | Moore + Militia fodder is a degenerate strategy |
| DESIGN-3 | Onboarding | MEDIUM | New players face Ammunition friction in multiplayer |
| DESIGN-4 | Balance | MEDIUM | Officer negative traits are inconsistent in impact |
| ERR-1 | GDD | LOW | Officer count unstated (minor) |
| ERR-2 | GDD | MEDIUM | SPD description overlaps with MOV |
| ERR-3 | GDD | LOW | Terrain coexistence contradiction |
| ERR-4 | GDD | LOW | Wellington trigger range unexplained |
| ERR-5 | GDD | MEDIUM | Lancer "reach" flavor doesn't match RNG=1 |
| DESIGN-5 | Pacing | LOW | Act 3 unlocks too many units too fast |
| DESIGN-6 | Balance | LOW | Rocket Battery may be undertuned for its cost |
| DESIGN-7 | Multiplayer | LOW | Map rotation could strand deployed armies |
| DESIGN-8 | Consistency | LOW | Morale system referenced but doesn't exist |

---

*This review is based on GDD v2.0 and the current codebase as of 2026-04-04. All code references include file paths and line numbers for traceability.*
