# WarChess — Seasonal Ranked System Design

## Overview

Seasonal ranked play provides recurring engagement for multiplayer. Each season is a time-limited competitive period with Elo resets, exclusive rewards, and a fresh leaderboard.

---

## Season Structure

### Duration
- **Season length:** 6 weeks (42 days)
- **Off-season:** 3 days between seasons (maintenance, reward distribution)
- **Seasons per year:** ~7 seasons

### Season Calendar
| Season | Theme | Notes |
|--------|-------|-------|
| 1 | The Opening Salvo | Launch season, no Elo reset |
| 2 | The March | First full season with reset |
| 3 | Summer Campaign | Longer if spanning holidays |
| 4 | Autumn Offensive | |
| 5 | Winter Quarters | Potential holiday event |
| 6 | Spring Thaw | |
| 7 | The Grand Review | Year-end, enhanced rewards |

---

## Elo Reset Rules

### Soft Reset Formula
At the start of each season, player Elo is compressed toward the default:

```
NewElo = (CurrentElo + DefaultElo) / 2
```

Where `DefaultElo = 1000`.

**Examples:**
| Pre-Season Elo | Post-Reset Elo |
|----------------|----------------|
| 800 | 900 |
| 1000 | 1000 |
| 1200 | 1100 |
| 1600 | 1300 |
| 2000 | 1500 |
| 2200 | 1600 |

### Per-Tier Reset
- Each Star General tier (1-5) has independent Elo
- All tiers reset simultaneously at season start
- Win counts for tier promotion do NOT reset (permanent progression)

### Placement Matches
- First 10 matches of a new season use **K-factor 48** (instead of 32)
- This allows faster calibration to true skill after reset
- After 10 matches, K-factor returns to 32

---

## Season Rewards

### End-of-Season Rewards (Based on Peak Elo)

Rewards are based on the **highest Elo achieved** during the season (not final Elo), per tier.

| Rank Achieved | Dispatch Boxes | Ammunition | Exclusive Cosmetic |
|---------------|----------------|------------|-------------------|
| Recruit (0-999) | 1 Bronze | 5 | — |
| Corporal (1000-1199) | 1 Bronze | 10 | — |
| Sergeant (1200-1399) | 1 Silver | 15 | Season Banner |
| Lieutenant (1400-1599) | 2 Silver | 20 | Season Banner |
| Captain (1600-1799) | 1 Gold | 25 | Season Banner + Portrait |
| Colonel (1800-1999) | 1 Gold + 1 Silver | 30 | Season Banner + Portrait |
| General (2000-2199) | 2 Gold | 40 | Full Season Set |
| Grand Marshal (2200+) | 3 Gold | 50 | Full Season Set + Title |

### Season-Exclusive Cosmetics

Each season has a unique cosmetic set that can **only** be earned during that season:

- **Season Banner** — Army banner with the season's theme (e.g., "Season 2: The March")
- **Season Portrait** — Commander portrait variant with seasonal styling
- **Full Season Set** — Banner + Portrait + Victory Animation
- **Season Title** — Display title next to player name (Grand Marshal only)

These are **never available for purchase** — earning only.

### Participation Rewards
- Complete 10+ matches in a season: 1 Bronze Dispatch Box
- Complete 25+ matches: 1 Silver Dispatch Box
- Complete 50+ matches: Season Badge cosmetic (available every season, different design)

---

## Leaderboard

### Per-Season Leaderboard
- Displayed per Star General tier
- Shows: Rank, Player Name, Elo, Wins, Losses, Win Rate
- Top 100 displayed
- Updated in real-time

### Historical Leaderboard
- Archive of final leaderboard standings per season
- "Hall of Fame" for Grand Marshal achievers
- Accessible from Multiplayer Hub

---

## Season Pass (Future Consideration)

If monetization expansion is desired, a **Season Pass** could be introduced:

- **Free track:** Standard season rewards (as above)
- **Premium track ($2.99):** Additional cosmetics, bonus ammunition, exclusive grid theme
- Premium rewards are cosmetic-only — no gameplay advantage

This is deferred to post-launch and should only be considered after player base is established.

---

## Implementation Notes

### New Data Classes

```csharp
// Add to WarChess.Multiplayer namespace

public class SeasonData
{
    public int SeasonNumber;
    public string SeasonName;
    public long StartDateTicks;     // UTC
    public long EndDateTicks;       // UTC
    public int PlacementKFactor;    // 48 for first 10 matches
    public int NormalKFactor;       // 32 after placement
}

public class SeasonReward
{
    public int MinElo;
    public int DispatchBoxBronze;
    public int DispatchBoxSilver;
    public int DispatchBoxGold;
    public int AmmunitionReward;
    public string ExclusiveCosmeticId;  // null if none
}
```

### Save Data Additions

```csharp
// Add to PlayerProfile or SaveData
public int CurrentSeason;
public Dictionary<int, int> PeakEloPerTierThisSeason;  // Track highest Elo
public int MatchesPlayedThisSeason;
public bool PlacementComplete;  // True after 10 matches
```

### Season Transition Flow
1. Season end timer triggers
2. Calculate rewards based on peak Elo per tier
3. Award Dispatch Boxes, ammunition, exclusive cosmetics
4. Archive leaderboard
5. Apply soft Elo reset
6. Reset seasonal counters
7. Start new season

---

## Anti-Abuse Measures

- **Win trading detection:** Flag accounts with >80% matches against same opponent
- **Elo floor:** Elo cannot drop below 0 (prevent intentional tanking)
- **Minimum games:** Must play 5+ games to appear on leaderboard
- **Inactive decay:** After 2 weeks of inactivity, Elo decays by 10/day (minimum 1000)
