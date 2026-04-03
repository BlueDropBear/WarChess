# WarChess — Art Style Guide

## 1. Overall Aesthetic

WarChess uses a **pixel art** style inspired by the Napoleonic era (1790s–1815). The art should feel **clean, readable, and charming** — not overly detailed. Clarity at small screen sizes is the priority.

**Reference games:** Advance Wars (GBA), Wargroove, Into the Breach, Kingdom Two Crowns.

---

## 2. Technical Specifications

### 2.1 Grid Tiles

| Property | Value |
|----------|-------|
| Tile size | 128×128 pixels |
| Pixels per unit (Unity) | 128 |
| Filter mode | Point (no filtering) |
| Compression | None |

### 2.2 Unit Sprites

Each unit sprite depicts a **multi-soldier formation** (not a single figure). The number of soldiers varies by unit type to reflect squad composition.

| Property | Value |
|----------|-------|
| Sprite size | 128×128 pixels (fits within one tile) |
| Animation frames | 4 per action (idle: 2, attack: 4, move: 4, death: 4) |
| Sprite sheet layout | Horizontal strip per animation |
| Facing | All sprites face right; flip horizontally for left-facing |

#### Soldiers Per Unit Type

| Unit Type | Soldiers | Formation |
|-----------|----------|-----------|
| Line Infantry | 8 | 2 ranks of 4, shoulder-to-shoulder |
| Militia | 6 | Loose irregular cluster |
| Cavalry | 4 mounted | Staggered line |
| Artillery | 3 crew + cannon | Cannon center, crew around it |
| Grenadier | 6 | Tight two-rank formation |
| Rifleman | 4 | Spread skirmish line |
| Hussar | 3 mounted | Light staggered formation |
| Cuirassier | 3 mounted | Tight wedge |
| Horse Artillery | 2 mounted + cannon | Mobile formation |
| Sapper | 4 | Working group with tools |
| Old Guard | 8 | Perfect two-rank formation |
| Rocket Battery | 3 crew + launcher | Crew around launcher |
| Lancer | 4 mounted | Chevron formation |
| Dragoon | 4 mounted | Line formation |

### 2.3 UI Elements

| Property | Value |
|----------|-------|
| Button height | 16px or 32px (small/large) |
| Font | Pixel font, 8px base size (scales to 16px, 24px) |
| Panel border | 2px solid, rounded corners optional |
| Icon size | 16×16 pixels |

---

## 3. Color Palette

A muted, earthy palette with strong accent colors for unit identification.

### 3.1 Base Palette

| Color | Hex | Use |
|-------|-----|-----|
| Parchment | `#F2E8D0` | UI backgrounds, menus |
| Dark Brown | `#3B2820` | Text, outlines, borders |
| Warm Gray | `#8C8070` | Inactive UI, disabled states |
| Deep Navy | `#1A2040` | Dark backgrounds, night scenes |

### 3.2 Terrain Colors

| Terrain | Primary | Secondary |
|---------|---------|-----------|
| Open Field | `#5A8C3A` | `#4A7A2A` |
| Forest | `#2D5A1E` | `#1E4A10` |
| Hill | `#A08050` | `#8A6A3A` |
| River | `#3A6A9C` | `#2A5A8C` |
| Bridge | `#8A7A6A` | `#6A5A4A` |
| Fortification | `#6A6A6A` | `#5A5A5A` |
| Mud | `#6A5030` | `#5A4020` |
| Town | `#B09070` | `#907050` |

### 3.3 Unit Faction Colors

| Faction | Primary | Trim | Used For |
|---------|---------|------|----------|
| Player (British) | `#CC2222` | `#FFFFFF` | Red coats, white crossbelts |
| Enemy (French) | `#2244AA` | `#FFFFFF` | Blue coats, white trim |
| Neutral | `#888888` | `#AAAAAA` | Unowned units, ghosts |

### 3.4 Unit Type Silhouette Colors (Placeholder Phase)

During Phase 1, units are 128×128 colored shapes with distinct silhouettes:

| Unit | Color | Shape Hint |
|------|-------|------------|
| Line Infantry | Red `#CC2222` | Square |
| Cavalry | Gold `#CCAA22` | Diamond/rhombus |
| Artillery | Dark Gray `#555555` | Circle |

---

## 4. Unit Design Guidelines

### 4.1 Readability First

- Each unit type must have a **distinct silhouette** recognizable at 128×128 pixels (and when scaled down on mobile)
- Units must be distinguishable even without color (for colorblind accessibility)
- Infantry: formation of soldiers standing upright, muskets visible
- Cavalry: mounted figures in formation, taller than infantry
- Artillery: cannon with crew, low and wide profile

### 4.2 Animation Guidelines

- **Idle:** Subtle 2-frame breathing/sway loop (0.5s per frame)
- **Move:** 4-frame walk/trot cycle
- **Attack:** 4 frames — wind up, strike/fire, recoil, return
- **Death:** 4 frames — hit reaction, fall, settle, fade

### 4.3 Direction

- All sprites drawn facing **right** as the base direction
- Engine flips sprites horizontally for left-facing units
- Player units face up (toward enemy), enemy units face down

---

## 5. UI Style

### 5.1 Principles

- **Parchment/paper aesthetic** — menus feel like war room maps and dispatches
- **Minimal chrome** — borders and panels, not heavy 3D effects
- **High contrast text** — dark brown on parchment, white on dark panels
- **Mobile-first** — buttons minimum 44×44 points for touch targets

### 5.2 Panel Style

```
┌─────────────────────┐
│  ARMY BUILDER       │  ← Title bar: Dark Brown bg, Parchment text
├─────────────────────┤
│                     │  ← Content area: Parchment bg
│  Unit list, grid,   │
│  stats display      │
│                     │
├─────────────────────┤
│  [Deploy] [Cancel]  │  ← Action bar: buttons with Dark Brown border
└─────────────────────┘
```

### 5.3 Health Bars

- 2px tall, positioned above unit sprite
- Green (`#44AA44`) → Yellow (`#CCAA22`) → Red (`#CC2222`) based on HP percentage
- Background: Dark Brown (`#3B2820`)

---

## 6. Effects

### 6.1 Battle Effects (Phase 5 — placeholder in earlier phases)

| Effect | Description |
|--------|-------------|
| Hit flash | Unit sprite flashes white for 2 frames on hit |
| Damage number | Integer pops up above defender, floats up and fades (red for damage, green for healing) |
| Death | Unit sprite fades to gray, falls down, particle puff |
| Charge trail | Small dust particles behind charging cavalry |
| Cannon fire | Muzzle flash + small smoke puff at artillery position |
| Screen shake | 2-3 pixel shake on artillery hits and charges |

---

## 7. Asset Sourcing Strategy

### Phase 1 (Prototype)
- Colored squares/shapes as unit placeholders (created in-engine)
- Simple colored tiles for terrain (created in-engine)

### Phase 3+ (Final Art)
Options in order of preference:
1. **AI-assisted pixel art** — Generate base sprites with AI tools, then hand-clean at pixel level
2. **Free asset packs** — itch.io pixel art packs (ensure license allows commercial use)
3. **Commission** — Hire a pixel artist for unit sprites ($5–15 per sprite, ~$70–210 for 14 units)

---

*All art specifications are subject to iteration during playtesting. Readability and gameplay clarity always take priority over visual fidelity.*
