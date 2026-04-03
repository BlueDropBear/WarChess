# WarChess — Technical Design Document

**Engine:** Unity 2022 LTS (C#) | **Rendering:** URP 2D | **Input:** New Input System

This document defines the software architecture for WarChess. All code must conform to the principles in `CLAUDE.md` and implement the mechanics described in `GAME_DESIGN_DOCUMENT.md`.

---

## 1. Architecture Overview

### 1.1 Layer Separation

The codebase is split into three layers. The critical boundary is between Logic and Presentation — battle logic must run without Unity.

```
┌─────────────────────────────────────────────────┐
│  PRESENTATION LAYER (Unity-dependent)           │
│  MonoBehaviours, UI, sprites, animations,       │
│  input handling, audio                          │
│  Namespaces: WarChess.View, WarChess.UI         │
├─────────────────────────────────────────────────┤
│  BRIDGE LAYER (thin adapters)                   │
│  Controllers that wire logic to presentation    │
│  BattleController, GridController,              │
│  ArmyBuilderController                          │
│  Namespace: WarChess.Controllers                │
├─────────────────────────────────────────────────┤
│  LOGIC LAYER (pure C# — no UnityEngine refs)    │
│  BattleEngine, GridMap, DamageCalculator,       │
│  FlankingCalculator, TargetingStrategies,       │
│  UnitInstance, BattleEvent, BattleResult        │
│  Namespaces: WarChess.Core, WarChess.Battle,    │
│  WarChess.Units                                 │
└─────────────────────────────────────────────────┘
```

**Rule:** No file in the Logic Layer may `using UnityEngine;`. This is enforced by convention and code review. The Logic Layer uses only `System` namespaces.

### 1.2 System Dependency Diagram

```
GameConfigSO ──→ GameConfigData (plain C# copy)
                        │
UnitStatsSO ──→ UnitInstance ──→ BattleEngine
                        │              │
                   GridMap ─────────────┘
                        │              │
                        │         BattleEvent[]
                        │              │
                   GridView ←── BattleVisualizer
                        │              │
                   UnitView ←──────────┘
```

ScriptableObjects (SOs) live in Unity. Before battle, their data is copied into plain C# structs/classes that the Logic Layer consumes. This is the "data handoff" pattern.

---

## 2. Integer Math Convention

**No floating-point math in the Logic Layer.** All combat calculations use integers to guarantee deterministic results across platforms.

### 2.1 Fixed-Point Multipliers

Multipliers are stored as **integers representing percentages** (base 100):

| GDD Value | Stored As | Applied As |
|-----------|-----------|------------|
| ×1.0 | 100 | `(damage * 100) / 100` |
| ×1.3 (side flank) | 130 | `(damage * 130) / 100` |
| ×2.0 (rear flank) | 200 | `(damage * 200) / 100` |
| ×0.75 (forest def) | 75 | `(damage * 75) / 100` |
| ×1.15 (formation) | 115 | `(damage * 115) / 100` |
| +20% ATK | 120 | `(atk * 120) / 100` |
| -15% DEF | 85 | `(def * 85) / 100` |

### 2.2 Modifier Stacking

When multiple multipliers apply, they chain multiplicatively:

```csharp
int ApplyModifiers(int baseDamage, int[] multipliers)
{
    int result = baseDamage;
    foreach (int mult in multipliers)
    {
        result = (result * mult) / 100;
    }
    return Math.Max(result, 1); // minimum 1 damage
}
```

Order of modifier application (per GDD 2.6):
1. Charge bonus
2. Terrain defense
3. Terrain elevation
4. Formation bonus
5. Flanking multiplier

### 2.3 Seeded RNG

All randomness uses `System.Random` initialized with a shared seed:

```csharp
public class BattleRng
{
    private readonly Random _rng;
    public BattleRng(int seed) => _rng = new Random(seed);
    public int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
}
```

The seed is generated at battle start. For multiplayer, the server provides the seed. For campaign, a seed is generated from `System.Environment.TickCount` and stored in the replay data.

---

## 3. Grid System

### 3.1 GridCoord (Pure C#)

`Scripts/Core/GridCoord.cs`

```csharp
public readonly struct GridCoord : IEquatable<GridCoord>
{
    public readonly int X; // 1-10 (column)
    public readonly int Y; // 1-10 (row, 1 = player back row, 10 = enemy back row)

    public GridCoord(int x, int y) { X = x; Y = y; }

    public int ManhattanDistance(GridCoord other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public bool IsValid => X >= 1 && X <= 10 && Y >= 1 && Y <= 10;

    // IEquatable, GetHashCode, operators omitted for brevity
}
```

### 3.2 GridMap (Pure C#)

`Scripts/Core/GridMap.cs`

Manages the logical 10×10 grid. Stores which unit occupies each tile and terrain data.

Key methods:
- `PlaceUnit(UnitInstance unit, GridCoord coord)` — assigns unit to tile
- `RemoveUnit(GridCoord coord)` — clears tile
- `GetUnitAt(GridCoord coord)` — returns unit or null
- `IsInDeploymentZone(GridCoord coord, Owner owner, GameConfigData config)` — validates placement
- `GetCoordsInRange(GridCoord center, int range)` — returns all valid coords within Manhattan distance
- `GetAdjacentCoords(GridCoord coord)` — 4 orthogonal neighbors
- `GetAllUnits(Owner owner)` — all units for a side

GridMap does NOT depend on UnityEngine. It is a plain C# class.

### 3.3 GridView (Unity MonoBehaviour)

`Scripts/Core/GridView.cs`

Responsible for:
- Instantiating tile GameObjects from a prefab in a 10×10 grid
- Converting screen/world positions to GridCoord and back
- Highlighting tiles (valid placement, attack range, movement range)
- Displaying terrain overlays

GridView holds a reference to GridMap and reads its state for rendering. It never modifies game state directly.

---

## 4. Unit Data Pipeline

### 4.1 Data Flow

```
[ScriptableObject]     [Runtime Logic]      [Visual]
UnitStatsSO ──copy──→ UnitInstance ──read──→ UnitView
(asset in Data/)       (plain C# class)     (MonoBehaviour)
```

### 4.2 UnitStatsSO (ScriptableObject)

`Scripts/Units/UnitStatsSO.cs`

Fields (all `int`):
- `unitName` (string), `unitType` (enum UnitType)
- `hp`, `atk`, `def`, `spd`, `rng`, `mov`, `cost`
- `targetingPriority` (enum: Nearest, Weakest, HighestThreat, ArtilleryFirst, Random)
- `flankSideMultiplier` (int, default 130 = ×1.3)
- `flankRearMultiplier` (int, default 200 = ×2.0)
- `abilityType` (enum identifying special ability)
- `formationType` (enum: None, BattleLine, Battery, CavalryWedge, Square, SkirmishScreen)
- `countsAsType` (enum UnitType — for formation counting, e.g., Grenadier counts as LineInfantry)
- `description` (string, for UI display)

### 4.3 UnitInstance (Pure C#)

`Scripts/Units/UnitInstance.cs`

Runtime state during battle. Created from UnitStatsSO data at battle start.

Fields:
- `int Id` — unique per battle, used for deterministic tie-breaking
- `UnitType Type`, `string Name`
- `int MaxHp, CurrentHp, Atk, Def, Spd, Rng, Mov`
- `int FlankSideMultiplier, FlankRearMultiplier`
- `GridCoord Position`
- `FacingDirection Facing` (enum: North, South — toward enemy deployment)
- `Owner Owner` (enum: Player, Enemy)
- `TargetingPriority TargetingPriority`
- `AbilityType Ability`
- `bool HasChargedThisRound, HasMovedThisRound, HasAttackedThisRound`
- `bool IsAlive => CurrentHp > 0`

Methods:
- `TakeDamage(int amount)` — reduces CurrentHp, clamps to 0
- `Heal(int amount)` — increases CurrentHp, clamps to MaxHp

### 4.4 UnitView (Unity MonoBehaviour)

`Scripts/Units/UnitView.cs`

Attached to unit prefab GameObjects. Reads from UnitInstance to update visuals:
- Sprite position (lerps to grid position via GridView world coords)
- Health bar display
- Facing indicator
- Death animation trigger
- Damage number popups

UnitView subscribes to BattleVisualizer events to animate state changes.

### 4.5 Enums (Pure C#)

`Scripts/Units/UnitEnums.cs`

```csharp
public enum UnitType
{
    LineInfantry, Militia, Cavalry, Artillery, Grenadier,
    Rifleman, Hussar, Cuirassier, HorseArtillery, Sapper,
    OldGuard, RocketBattery, Lancer, Dragoon
}

public enum TargetingPriority { Nearest, Weakest, HighestThreat, ArtilleryFirst, Random }
public enum FacingDirection { North, South }
public enum Owner { Player, Enemy }
public enum AbilityType
{
    None, StrengthInNumbers, Charge, Bombardment, Grenade,
    AimedShot, HitAndRun, ArmoredCharge, LimberedUp,
    Entrench, Unbreakable, CongreveBarrage, Brace, Dismount
}
public enum FormationType { None, BattleLine, Battery, CavalryWedge, Square, SkirmishScreen }
public enum FlankDirection { Front, Side, Rear }
```

---

## 5. Battle Engine

### 5.1 BattleEngine (Pure C#)

`Scripts/Battle/BattleEngine.cs`

The core auto-battle loop. Takes two army lists, a GridMap, config data, and a seed. Produces a BattleResult and a complete list of BattleEvents for replay.

```csharp
public class BattleEngine
{
    private readonly GridMap _grid;
    private readonly GameConfigData _config;
    private readonly BattleRng _rng;
    private readonly List<UnitInstance> _allUnits;
    private readonly List<BattleEvent> _events;
    private int _currentRound;

    public BattleEngine(GridMap grid, List<UnitInstance> playerUnits,
        List<UnitInstance> enemyUnits, GameConfigData config, int seed) { ... }

    /// <summary>Runs the full battle to completion. Returns the result.</summary>
    public BattleResult RunFullBattle() { ... }

    /// <summary>Runs a single round. Returns false if battle is over.</summary>
    public bool RunRound() { ... }

    public IReadOnlyList<BattleEvent> Events => _events;
}
```

### 5.2 Round Loop (per GDD 2.4)

Each round follows this sequence:

```
1. ROUND START
   - Emit RoundStartedEvent
   - Recalculate formation bonuses (FormationDetector)

2. INITIATIVE
   - Sort all living units by SPD descending
   - Tie-break: higher unit Id goes first (deterministic)

3. MOVEMENT (each unit in initiative order)
   - Select target via ITargetingStrategy
   - MovementResolver calculates path toward target
   - Move up to MOV tiles (reduced by terrain cost)
   - Emit UnitMovedEvent for each unit that moves
   - Track tiles moved (for Charge detection: 3+ tiles)

4. COMBAT (each unit in initiative order)
   - Skip if no target in range
   - DamageCalculator computes damage with all modifiers
   - Apply damage to target (and AoE targets if applicable)
   - Emit UnitAttackedEvent for each attack
   - Emit UnitDiedEvent for any unit reaching 0 HP

5. CLEANUP
   - Remove dead units from GridMap
   - Reset per-round flags (HasMovedThisRound, etc.)
   - Check win conditions

6. WIN CHECK
   - All player units dead → EnemyWin
   - All enemy units dead → PlayerWin
   - Both sides dead → Draw
   - Round >= MaxRounds (30) → compare total remaining HP, higher wins (draw if equal)
```

### 5.3 BattleEvent System

`Scripts/Battle/BattleEvent.cs`

Events are plain C# data objects emitted by BattleEngine. The Presentation Layer consumes them to animate the battle.

```csharp
public abstract class BattleEvent
{
    public int Round { get; }
}

public class RoundStartedEvent : BattleEvent
{
    public int RoundNumber { get; }
}

public class UnitMovedEvent : BattleEvent
{
    public int UnitId { get; }
    public GridCoord From { get; }
    public GridCoord To { get; }
    public int TilesMoved { get; }
}

public class UnitAttackedEvent : BattleEvent
{
    public int AttackerId { get; }
    public int DefenderId { get; }
    public int DamageDealt { get; }
    public FlankDirection FlankDirection { get; }
    public bool IsChargeAttack { get; }
    public bool IsAoE { get; }
}

public class UnitDiedEvent : BattleEvent
{
    public int UnitId { get; }
    public int KillerId { get; }
}

public class BattleEndedEvent : BattleEvent
{
    public BattleOutcome Outcome { get; }
    public int RoundsPlayed { get; }
}
```

### 5.4 BattleResult

`Scripts/Battle/BattleResult.cs`

```csharp
public enum BattleOutcome { PlayerWin, EnemyWin, Draw }

public class BattleResult
{
    public BattleOutcome Outcome { get; }
    public int RoundsPlayed { get; }
    public int PlayerUnitsRemaining { get; }
    public int EnemyUnitsRemaining { get; }
    public int PlayerHpRemaining { get; }
    public int EnemyHpRemaining { get; }
    public IReadOnlyList<BattleEvent> Events { get; }
}
```

---

## 6. Damage Calculator

### 6.1 Core Formula (Pure C#)

`Scripts/Battle/DamageCalculator.cs`

Implements GDD Section 2.6:

```csharp
public static class DamageCalculator
{
    /// <summary>
    /// Calculates final damage from attacker to defender.
    /// All math is integer-only. Multipliers are base-100 percentages.
    /// </summary>
    public static int Calculate(
        UnitInstance attacker,
        UnitInstance defender,
        FlankDirection flankDir,
        int terrainDefenseMultiplier,   // 100 = no effect, 75 = forest
        int terrainAttackMultiplier,    // 100 = no effect, 125 = hill
        int formationMultiplier,        // 100 = no effect, 115 = formation bonus
        bool isCharge,
        int chargeMultiplier)           // 200 for standard charge
    {
        // Step 1: Base damage
        int baseDamage = attacker.Atk - (defender.Def / 2);
        baseDamage = Math.Max(baseDamage, 1);

        // Step 2: Apply modifiers in GDD order
        int damage = baseDamage;
        if (isCharge) damage = (damage * chargeMultiplier) / 100;
        damage = (damage * terrainDefenseMultiplier) / 100;
        damage = (damage * terrainAttackMultiplier) / 100;
        damage = (damage * formationMultiplier) / 100;

        // Step 3: Flanking (uses defender's per-unit multipliers)
        int flankMult = flankDir switch
        {
            FlankDirection.Side => defender.FlankSideMultiplier,
            FlankDirection.Rear => defender.FlankRearMultiplier,
            _ => 100
        };
        damage = (damage * flankMult) / 100;

        return Math.Max(damage, 1); // minimum 1 damage always
    }
}
```

### 6.2 AoE Damage (Artillery Bombardment)

Artillery's Bombardment ability hits the target tile at full damage and 4 orthogonal neighbors at 50% damage:

```csharp
// Primary target: full damage
// Adjacent targets: (damage * 50) / 100
```

Rocket Battery's Congreve Barrage selects a random 3×3 area near the target using BattleRng. Friendly fire is possible — the barrage does not distinguish friend from foe.

---

## 7. Flanking System

### 7.1 FlankingCalculator (Pure C#)

`Scripts/Battle/FlankingCalculator.cs`

Determines attack direction based on attacker position relative to defender position and facing.

```csharp
public static class FlankingCalculator
{
    /// <summary>
    /// Determines flank direction. Units face toward enemy deployment:
    /// Player units face North (toward row 10), Enemy units face South (toward row 1).
    /// </summary>
    public static FlankDirection GetFlankDirection(
        GridCoord attackerPos, GridCoord defenderPos, FacingDirection defenderFacing)
    {
        int dx = attackerPos.X - defenderPos.X;
        int dy = attackerPos.Y - defenderPos.Y;

        // Determine if attack comes from front, side, or rear
        // relative to defender's facing
        if (defenderFacing == FacingDirection.North)
        {
            // Defender faces North (toward row 10)
            // Front = attacker is north (dy > 0)
            // Rear = attacker is south (dy < 0)
            // Side = attacker is east/west (|dx| >= |dy|) and dy == 0
            if (dy > 0 && Math.Abs(dx) < dy) return FlankDirection.Front;
            if (dy < 0 && Math.Abs(dx) < Math.Abs(dy)) return FlankDirection.Rear;
            return FlankDirection.Side;
        }
        else // FacingDirection.South
        {
            // Mirror: Front = south, Rear = north
            if (dy < 0 && Math.Abs(dx) < Math.Abs(dy)) return FlankDirection.Front;
            if (dy > 0 && Math.Abs(dx) < dy) return FlankDirection.Rear;
            return FlankDirection.Side;
        }
    }

    /// <summary>
    /// Returns the flanking damage multiplier (base-100) for the given direction,
    /// using the defender's per-unit flank multipliers.
    /// </summary>
    public static int GetMultiplier(FlankDirection direction, UnitInstance defender)
    {
        return direction switch
        {
            FlankDirection.Side => defender.FlankSideMultiplier,
            FlankDirection.Rear => defender.FlankRearMultiplier,
            _ => 100
        };
    }
}
```

### 7.2 Facing Rules

- Player units start facing **North** (toward row 10)
- Enemy units start facing **South** (toward row 1)
- Units do **not** rotate during battle in v1 (facing is fixed)
- Future: units could rotate to face their current target

---

## 8. Targeting AI

### 8.1 Strategy Pattern (Pure C#)

`Scripts/Battle/Targeting/ITargetingStrategy.cs`

```csharp
public interface ITargetingStrategy
{
    /// <summary>
    /// Selects the best target from the list of enemies.
    /// Returns null if no valid target exists.
    /// </summary>
    UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid);
}
```

### 8.2 Implementations

All in `Scripts/Battle/Targeting/`:

**NearestTargeting.cs** — Selects enemy with smallest Manhattan distance. Tie-break: lowest Id.
```csharp
// Used by: Line Infantry, Grenadier, Militia, Artillery, Horse Artillery, Sapper, Old Guard
```

**WeakestTargeting.cs** — Selects enemy with lowest current HP. Tie-break: nearest.
```csharp
// Used by: Rifleman, Hussar
```

**HighestThreatTargeting.cs** — Selects enemy with highest ATK. Tie-break: nearest.
```csharp
// Used by: Cavalry, Cuirassier, Lancer
```

**ArtilleryFirstTargeting.cs** — Selects enemy with highest RNG (prioritizes artillery/ranged). Tie-break: nearest. Falls back to Nearest if no ranged enemies.
```csharp
// Used by: Dragoon
```

**RandomTargeting.cs** — Selects a random enemy using BattleRng. Deterministic given same seed.
```csharp
// Used by: Rocket Battery
```

### 8.3 Fallback

All strategies fall back to **Nearest** if their preferred target type doesn't exist (per GDD 2.5).

### 8.4 TargetingFactory

`Scripts/Battle/Targeting/TargetingFactory.cs`

Maps `TargetingPriority` enum to strategy instance. Strategies are stateless singletons (except RandomTargeting which needs BattleRng).

```csharp
public static class TargetingFactory
{
    public static ITargetingStrategy Create(TargetingPriority priority, BattleRng rng)
    {
        return priority switch
        {
            TargetingPriority.Nearest => NearestTargeting.Instance,
            TargetingPriority.Weakest => WeakestTargeting.Instance,
            TargetingPriority.HighestThreat => HighestThreatTargeting.Instance,
            TargetingPriority.ArtilleryFirst => ArtilleryFirstTargeting.Instance,
            TargetingPriority.Random => new RandomTargeting(rng),
            _ => NearestTargeting.Instance
        };
    }
}
```

---

## 9. GameConfig Pattern

### 9.1 GameConfigSO (ScriptableObject)

`Scripts/Config/GameConfigSO.cs` — Unity asset at `Data/Config/GameConfig.asset`

All tunable values from the GDD live here. Nothing is hardcoded in logic classes.

Fields:
- **Grid:** `gridWidth` (10), `gridHeight` (10)
- **Deployment:** `playerDeployMinRow` (1), `playerDeployMaxRow` (3), `enemyDeployMinRowCampaign` (5), `enemyDeployMaxRowCampaign` (10), `enemyDeployMinRowMultiplayer` (8), `enemyDeployMaxRowMultiplayer` (10)
- **Battle:** `maxRounds` (30), `minimumDamage` (1)
- **Flanking Defaults:** `defaultFlankSideMultiplier` (130), `defaultFlankRearMultiplier` (200)
- **Terrain:** `forestDefenseMultiplier` (75), `hillAttackMultiplier` (125), `fortificationDefenseMultiplier` (70), `townDefenseMultiplier` (80)
- **Formation:** `battleLineDefBonus` (115), `batteryAtkBonus` (120), `cavalryWedgeChargeBonus` (125), `squareDefVsCavalryBonus` (130), `skirmishAtkBonus` (120), `skirmishRangeBonus` (1)
- **Charge:** `chargeMinTilesMoved` (3), `chargeMultiplier` (200)
- **Difficulty:** `recruitStatMultiplier` (85), `veteranStatMultiplier` (100), `marshalStatMultiplier` (115)

### 9.2 GameConfigData (Pure C#)

`Scripts/Config/GameConfigData.cs`

A plain C# struct copied from GameConfigSO at battle start. This is what the Logic Layer consumes — no ScriptableObject references cross the boundary.

```csharp
public readonly struct GameConfigData
{
    public readonly int GridWidth;
    public readonly int GridHeight;
    public readonly int PlayerDeployMinRow;
    public readonly int PlayerDeployMaxRow;
    public readonly int MaxRounds;
    public readonly int MinimumDamage;
    // ... all fields mirrored from GameConfigSO
}
```

---

## 10. Event Architecture

### 10.1 Loose Coupling via Events

Systems communicate through C# events and delegates. No system directly calls another system's methods.

**Battle Engine → Presentation:**
- BattleEngine produces `List<BattleEvent>` (data objects)
- BattleVisualizer consumes them sequentially with coroutine-driven animations
- No callbacks from Presentation back to Engine during resolution

**Controller Events (Unity side):**

```csharp
// In BattleController.cs (Bridge Layer)
public event Action<BattleResult> OnBattleCompleted;
public event Action<int> OnRoundStarted;

// In GridController.cs
public event Action<GridCoord> OnTileClicked;
public event Action<GridCoord, UnitInstance> OnUnitPlaced;
public event Action<GridCoord> OnUnitRemoved;
```

### 10.2 BattleVisualizer (Unity MonoBehaviour)

`Scripts/Battle/BattleVisualizer.cs`

Replays BattleEvents as animated sequences:

```
BattleController calls BattleEngine.RunRound()
    → gets List<BattleEvent> for that round
    → passes to BattleVisualizer.PlayRound(events)
    → Visualizer plays each event as a coroutine:
        UnitMovedEvent → slide sprite from A to B (0.3s)
        UnitAttackedEvent → flash attacker, show damage number (0.2s)
        UnitDiedEvent → death animation + remove sprite (0.5s)
    → When all events played, signals BattleController to run next round
```

Speed controls: 1×, 2×, 4×, Skip (runs all rounds instantly, shows final state).

---

## 11. Save System

### 11.1 Approach

`Scripts/Save/SaveManager.cs`

Uses JSON serialization (`JsonUtility` or `System.Text.Json`) to save/load:
- **Campaign progress:** Current battle, stars per battle, unlocked units, difficulty
- **Saved armies:** List of army compositions with unit types, positions, officer assignments, commander
- **Settings:** Audio volumes, accessibility options

Files stored in `Application.persistentDataPath`:
- `campaign_save.json`
- `armies_campaign.json`
- `armies_multiplayer.json`
- `settings.json`

### 11.2 Army Serialization

```csharp
[Serializable]
public class SavedArmy
{
    public string Name;
    public string CommanderType;
    public List<SavedUnit> Units;
}

[Serializable]
public class SavedUnit
{
    public string UnitType;
    public int GridX;
    public int GridY;
    public string OfficerType; // nullable
}
```

This format also serves as the multiplayer army submission payload.

---

## 12. Phase 1 File Manifest

Complete list of files to create for Phase 1 (Core Grid Prototype):

### Pure C# (Logic Layer — no UnityEngine)

| File | Description |
|------|-------------|
| `Scripts/Core/GridCoord.cs` | Grid coordinate struct |
| `Scripts/Core/GridMap.cs` | 10×10 logical grid |
| `Scripts/Units/UnitEnums.cs` | All game enums |
| `Scripts/Units/UnitInstance.cs` | Runtime unit state |
| `Scripts/Config/GameConfigData.cs` | Plain C# config copy |
| `Scripts/Battle/BattleEngine.cs` | Core auto-battle loop |
| `Scripts/Battle/BattleEvent.cs` | Event data classes |
| `Scripts/Battle/BattleResult.cs` | Battle outcome data |
| `Scripts/Battle/BattleRng.cs` | Seeded RNG wrapper |
| `Scripts/Battle/DamageCalculator.cs` | Combat math |
| `Scripts/Battle/FlankingCalculator.cs` | Flank direction + multiplier |
| `Scripts/Battle/MovementResolver.cs` | Unit pathfinding (Manhattan) |
| `Scripts/Battle/Targeting/ITargetingStrategy.cs` | Targeting interface |
| `Scripts/Battle/Targeting/NearestTargeting.cs` | Nearest enemy |
| `Scripts/Battle/Targeting/WeakestTargeting.cs` | Lowest HP enemy |
| `Scripts/Battle/Targeting/HighestThreatTargeting.cs` | Highest ATK enemy |
| `Scripts/Battle/Targeting/ArtilleryFirstTargeting.cs` | Prioritize ranged |
| `Scripts/Battle/Targeting/RandomTargeting.cs` | Random (seeded) |
| `Scripts/Battle/Targeting/TargetingFactory.cs` | Enum-to-strategy mapper |

### Unity (Presentation + Bridge Layer)

| File | Description |
|------|-------------|
| `Scripts/Units/UnitStatsSO.cs` | ScriptableObject for unit data |
| `Scripts/Units/UnitView.cs` | Unit visual representation |
| `Scripts/Config/GameConfigSO.cs` | ScriptableObject for config |
| `Scripts/Core/GridView.cs` | Grid tile rendering + input |
| `Scripts/Battle/BattleController.cs` | Orchestrates battle flow |
| `Scripts/Battle/BattleVisualizer.cs` | Animates battle events |
| `Scripts/Army/UnitPlacementController.cs` | Drag-and-drop placement |

### Data Assets (created in Unity Editor)

| Asset | Path |
|-------|------|
| LineInfantry SO | `Data/Units/LineInfantry.asset` |
| Cavalry SO | `Data/Units/Cavalry.asset` |
| Artillery SO | `Data/Units/Artillery.asset` |
| GameConfig SO | `Data/Config/GameConfig.asset` |

### Prefabs (created in Unity Editor)

| Prefab | Path |
|--------|------|
| Grid Tile | `Prefabs/Tiles/GridTile.prefab` |
| Line Infantry Unit | `Prefabs/Units/LineInfantry.prefab` |
| Cavalry Unit | `Prefabs/Units/Cavalry.prefab` |
| Artillery Unit | `Prefabs/Units/Artillery.prefab` |

---

*This document is the authoritative technical reference. All implementation must conform to the architecture defined here. The GDD is authoritative for game mechanics; this document is authoritative for how those mechanics are implemented in code.*
