# WarChess — Development Roadmap
### A Napoleonic Auto-Battler | Concept to Deployment

**Engine:** Unity (C#) | **Art:** Pixel Art | **Platforms:** PC, iOS, Android
**Team:** Solo developer + Claude (design, docs, architecture, content)
**Budget:** Bootstrapped ($0–500)

---

## How This Document Works

This roadmap is organized into **7 phases**, each broken into tasks. Every task is tagged with who does it:

- **🧑 David** — Requires a human (installing software, playtesting, App Store accounts, etc.)
- **🤖 Claude** — Claude produces this deliverable (documents, code, designs, balance sheets)
- **🧑🤖 Both** — Collaborative (David implements, Claude architects/reviews)

Phases are sequential, but tasks within a phase can often run in parallel. No phase has a hard deadline — move to the next when you're confident in the current one.

---

## Phase 0: Foundation (Weeks 1–2)
*Goal: Development environment ready, core concept locked down.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 0.1 | Install Unity (LTS version), VS Code/Rider, Git | 🧑 David | Working dev environment |
| 0.2 | Create GitHub/GitLab repository with .gitignore for Unity | 🧑 David | Version-controlled project |
| 0.3 | Complete Unity beginner tutorial (Roll-a-Ball or equivalent) | 🧑 David | Familiarity with Unity editor |
| 0.4 | Game Design Document v1 (GDD) | 🤖 Claude | Full GDD covering mechanics, units, campaign structure |
| 0.5 | Technical Design Document (TDD) | 🤖 Claude | Architecture, folder structure, key systems design |
| 0.6 | Art Style Guide with reference examples | 🤖 Claude | Pixel art specs, palette, tile sizes, animation frames |
| 0.7 | Project folder structure scaffold | 🤖 Claude | Unity project organization template |

**Phase 0 exit criteria:** David can open Unity, create a scene, and push to Git. GDD and TDD are reviewed and agreed upon.

---

## Phase 1: Core Grid Prototype (Weeks 3–6)
*Goal: A 10×10 board where units can be placed and auto-battle plays out.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 1.1 | Grid system — 10×10 tile generation, coordinate system, tile highlighting | 🧑🤖 Both | Grid renders, tiles are clickable |
| 1.2 | Unit data model — ScriptableObject architecture for unit types | 🤖 Claude | Unit SO template with stats (HP, ATK, DEF, range, movement) |
| 1.3 | Unit placement — drag-and-drop units onto your half of the board | 🧑🤖 Both | Units snap to grid tiles |
| 1.4 | Auto-battle engine v1 — turn-based resolution (move → attack → resolve) | 🤖 Claude | Battle logic script, unit AI targeting |
| 1.5 | Battle visualization — units animate movement and attacks on grid | 🧑🤖 Both | Watchable auto-battle |
| 1.6 | Placeholder art — colored squares or free pixel sprites for 3 unit types | 🧑🤖 Both | Visual distinction between unit types |
| 1.7 | Win/loss detection | 🤖 Claude | Battle ends, result displayed |

### Unit Types for Prototype

| Unit | Role | Range | Movement | Special |
|------|------|-------|----------|---------|
| Line Infantry | Frontline | 1 tile | 2 tiles | None — the baseline |
| Cavalry | Flanker | 1 tile | 4 tiles | Charge bonus (2× ATK on first hit) |
| Artillery | Siege | 3 tiles | 1 tile | Area damage (hits adjacent tiles) |

**Phase 1 exit criteria:** You can place 3 unit types on a grid, press "Battle," and watch them fight to a conclusion. It doesn't need to be fun yet — just functional.

---

## Phase 2: Game Loop & UI (Weeks 7–12)
*Goal: A playable loop — army building, battling, progressing.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 2.1 | Army builder screen — select and position units within a point budget | 🧑🤖 Both | Pre-battle army composition UI |
| 2.2 | Campaign map v1 — linear node progression (battle → battle) | 🧑🤖 Both | Map screen with 5–8 nodes |
| 2.3 | Enemy army AI — pre-built enemy compositions per campaign node | 🤖 Claude | Enemy army data for each battle |
| 2.4 | Unit unlock system — new units introduced at campaign milestones | 🤖 Claude | Unlock schedule + tutorial prompts |
| 2.5 | Battle results screen — XP, rewards, unit stats summary | 🧑🤖 Both | Post-battle flow |
| 2.6 | Save/load system — persist campaign progress locally | 🤖 Claude | PlayerPrefs or JSON save system |
| 2.7 | Main menu, settings, pause menu | 🧑🤖 Both | Navigation flow |
| 2.8 | UI framework — consistent pixel-art UI kit (buttons, panels, fonts) | 🤖 Claude | Reusable UI prefabs/specs |
| 2.9 | Sound effects — free SFX for attacks, movement, victory, defeat | 🤖 Claude | Sourced from freesound.org or similar |
| 2.10 | Background music — era-appropriate free tracks | 🤖 Claude | Sourced from free music libraries |

**Phase 2 exit criteria:** A player can start a campaign, build armies, fight through multiple battles with escalating difficulty, and save progress.

---

## Phase 3: Content & Depth (Weeks 13–20)
*Goal: Enough units, mechanics, and campaign content to be genuinely engaging.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 3.1 | Full unit roster design (12–16 units) | 🤖 Claude | Balanced unit stats, abilities, roles |
| 3.2 | Terrain system — tiles with combat modifiers (forest, hill, river, fortification) | 🧑🤖 Both | Terrain affects movement and combat |
| 3.3 | Commander abilities — per-army special powers (e.g., artillery barrage, forced march) | 🤖 Claude | 4–6 commander abilities |
| 3.4 | Formation bonuses — adjacency effects (line infantry in a row gets DEF bonus) | 🤖 Claude | Formation detection logic + bonuses |
| 3.5 | Full campaign — 20–30 battles across 3 acts with narrative context | 🤖 Claude | Campaign data, difficulty curve, story beats |
| 3.6 | Pixel art — commissioned or AI-assisted unit sprites, terrain tiles, UI elements | 🧑🤖 Both | Final art assets |
| 3.7 | Tutorial system — contextual tooltips and guided first battles | 🤖 Claude | Tutorial script + trigger logic |
| 3.8 | Balance testing spreadsheet — unit matchup matrix, win-rate tracking | 🤖 Claude | Balance spreadsheet + adjustment tools |
| 3.9 | Difficulty settings (recruit / veteran / marshal) | 🤖 Claude | Scaling logic for enemy stats and AI |

### Full Unit Roster (Draft)

| Unit | Era Reference | Role | Unlock |
|------|--------------|------|--------|
| Line Infantry | British Redcoats | Frontline | Start |
| Militia | Citizen soldiers | Cheap swarm | Start |
| Cavalry | Light Dragoons | Flanker | Battle 3 |
| Artillery | Field cannon | Siege/AoE | Battle 5 |
| Grenadier | Elite infantry | Heavy assault | Battle 8 |
| Rifleman | Baker rifle skirmishers | Long-range precision | Battle 10 |
| Hussar | Light cavalry | Scout/harass | Battle 12 |
| Cuirassier | Heavy cavalry | Armored charge | Battle 15 |
| Horse Artillery | Mobile guns | Mobile siege | Battle 17 |
| Sapper | Combat engineers | Terrain manipulation | Battle 19 |
| Old Guard | Napoleon's elite | Supreme infantry | Battle 22 |
| Rocket Battery | Congreve rockets | Unpredictable AoE | Battle 24 |
| Lancer | Polish lancers | Anti-cavalry | Battle 26 |
| Dragoon | Mounted infantry | Versatile | Battle 28 |

**Phase 3 exit criteria:** The campaign is completable end-to-end with a satisfying difficulty curve. At least 12 units feel distinct and balanced.

---

## Phase 4: Multiplayer (Weeks 21–28)
*Goal: Asynchronous PvP with rankings.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 4.1 | Backend selection and setup — PlayFab (free tier), Firebase, or Nakama | 🧑🤖 Both | Server running, SDK integrated |
| 4.2 | Player authentication — anonymous + optional email/social login | 🧑🤖 Both | Players can create accounts |
| 4.3 | Army submission — encode army composition + placement as JSON | 🤖 Claude | Army serialization system |
| 4.4 | Matchmaking — Elo-based pairing, queue submitted armies against similar rank | 🤖 Claude | Matchmaking logic |
| 4.5 | Async battle resolution — server or client resolves battle deterministically | 🤖 Claude | Deterministic battle engine (seeded RNG) |
| 4.6 | Battle replay — view how your army performed against opponent | 🧑🤖 Both | Replay viewer |
| 4.7 | Leaderboard — ranked tiers (Recruit → Marshal of the Empire) | 🤖 Claude | Elo system + rank thresholds |
| 4.8 | Anti-cheat basics — server-side army validation, point budget enforcement | 🤖 Claude | Validation logic |
| 4.9 | Multiplayer UI — queue status, match history, rank display | 🧑🤖 Both | Multiplayer screens |

### Recommended Backend: PlayFab (Free Tier)

PlayFab offers authentication, leaderboards, cloud script, and player data — all free up to 100K MAU. This covers your needs without spending a dollar until you have real traction. Firebase is a solid alternative if you prefer Google's ecosystem.

### Async Multiplayer Flow

```
Player A builds army → Submits to server
                                            → Server pairs A vs B by Elo
Player B builds army → Submits to server

Server resolves battle deterministically
        ↓
Both players can watch replay and see result
        ↓
Elo updated, leaderboard refreshed
```

**Phase 4 exit criteria:** Two real players can submit armies, get matched, and watch the result. Leaderboard updates correctly.

---

## Phase 5: Polish & Platform Prep (Weeks 29–36)
*Goal: The game feels finished. Ready for all three platforms.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 5.1 | Touch controls — mobile-friendly drag, zoom, tap interactions | 🧑🤖 Both | Works on phone screens |
| 5.2 | Responsive UI — scales from phone to desktop | 🧑🤖 Both | UI works at all resolutions |
| 5.3 | Performance optimization — target 60fps on mid-range phones | 🧑🤖 Both | Profiling + optimization pass |
| 5.4 | Particle effects — battle impact, unit death, ability activation | 🧑🤖 Both | Visual juice |
| 5.5 | Screen shake, hit flash, damage numbers | 🧑🤖 Both | Game feel polish |
| 5.6 | Music and SFX polish pass | 🤖 Claude | Final audio sourcing and mixing specs |
| 5.7 | Accessibility — colorblind mode, text scaling, unit labels | 🧑🤖 Both | Accessibility options |
| 5.8 | Localization framework (English first, structure for future languages) | 🤖 Claude | String table system |
| 5.9 | Analytics integration — track where players quit, what units they pick | 🧑🤖 Both | Unity Analytics or similar |
| 5.10 | Apple Developer Account ($99/yr) + Google Play Console ($25 one-time) | 🧑 David | Platform accounts |
| 5.11 | iOS build setup — Xcode, provisioning profiles, TestFlight | 🧑 David | iOS builds working |
| 5.12 | Android build setup — keystore, APK/AAB generation | 🧑 David | Android builds working |
| 5.13 | PC build — Steam page or itch.io listing | 🧑 David | PC distribution channel |

**Phase 5 exit criteria:** The game runs smoothly on all three platforms. Builds successfully deploy to test devices.

---

## Phase 6: Launch (Weeks 37–40)
*Goal: Game is live and players can find it.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 6.1 | Store listings — screenshots, description, keywords, icon | 🤖 Claude | Store copy and asset specs |
| 6.2 | Trailer / gameplay GIF | 🧑🤖 Both | 30-second gameplay capture |
| 6.3 | Beta test — invite 10–20 players, collect feedback | 🧑 David | Bug reports + balance feedback |
| 6.4 | Bug fix sprint based on beta feedback | 🧑🤖 Both | Stable build |
| 6.5 | Submit to App Store + Google Play | 🧑 David | Apps under review |
| 6.6 | Launch on itch.io / Steam (if applicable) | 🧑 David | PC version live |
| 6.7 | Launch announcement — Reddit, Twitter, indie game forums | 🧑🤖 Both | Marketing posts |
| 6.8 | Post-launch monitoring — crash reports, server health, player feedback | 🧑🤖 Both | Monitoring dashboard |

### Free Marketing Channels

- r/indiegaming, r/gamedev, r/PixelArt — dev log posts during development build audience early
- Twitter/X #indiedev #gamedev — share progress GIFs weekly
- itch.io — launch here first for feedback before mobile submission
- TIGSource, IndieDB — post a devlog
- Discord — create a server once you have a playable build

**Phase 6 exit criteria:** Game is live on at least one platform. Players can download and play.

---

## Phase 7: Post-Launch & Growth (Ongoing)
*Goal: Sustain and grow the game based on real player data.*

| # | Task | Owner | Deliverable |
|---|------|-------|-------------|
| 7.1 | Balance patches based on multiplayer win-rate data | 🤖 Claude | Balance adjustment recommendations |
| 7.2 | New unit releases (2–3 per content update) | 🤖 Claude | Unit designs + balance testing |
| 7.3 | Seasonal ranked seasons with reset + rewards | 🤖 Claude | Season structure design |
| 7.4 | Community feedback pipeline | 🧑 David | Discord / Reddit monitoring |
| 7.5 | Monetization system — Field Manuals (Warbond-style content packs), Sovereign/Battle Star currencies, Quartermaster's Shop, Weekly Challenges. **Code complete**; David to build UI | 🤖 Claude + 🧑 David | Monetization design + implementation (ethical, cosmetic-only). See Docs/MONETIZATION_STRATEGY.md |
| 7.6 | New campaign chapters | 🤖 Claude | Additional story + battles |

---

## Budget Breakdown (Estimated)

| Item | Cost | Notes |
|------|------|-------|
| Unity | $0 | Free (Personal license, <$200K revenue) |
| Apple Developer Account | $99/yr | Required for iOS |
| Google Play Console | $25 | One-time fee |
| PlayFab / Firebase | $0 | Free tier covers early growth |
| Art assets | $0–200 | Mix of free assets + AI-assisted pixel art |
| SFX / Music | $0 | Free libraries (freesound.org, incompetech, etc.) |
| itch.io | $0 | Free to publish |
| Steam | $100 | If you choose Steam (optional, can defer) |
| **Total** | **$124–424** | **Well within budget** |

---

## What Claude Will Produce (Deliverables List)

As we work through each phase, Claude will create these documents and assets:

1. **Game Design Document** — Complete mechanics, units, abilities, campaign, multiplayer rules
2. **Technical Design Document** — Architecture, system diagrams, folder structure, coding patterns
3. **Art Style Guide** — Pixel art specifications, color palette, tile sizes, sprite sheets
4. **Unit Balance Spreadsheet** — Stats, matchup matrices, cost curves
5. **Campaign Design** — All 30 battles with enemy compositions, unlock schedule, story
6. **C# Code Architecture** — Scripts for grid, battle engine, AI, save system, networking
7. **UI/UX Wireframes** — Screen layouts for every game screen
8. **Store Listing Copy** — App Store and Google Play descriptions, keywords
9. **Marketing Materials** — Reddit posts, devlog templates, social media copy
10. **Post-Launch Balance Reports** — Data-driven adjustment recommendations

---

## Immediate Next Steps

When you're ready to begin, here's the order:

1. **You:** Install Unity 2022 LTS + VS Code + Git
2. **You:** Do one Unity beginner tutorial (2–3 hours)
3. **Claude:** Delivers the full Game Design Document
4. **Claude:** Delivers the Technical Design Document with project structure
5. **Together:** Start Phase 1 — building the grid

---

*This is a living document. As we learn and iterate, we'll update phases, timelines, and priorities. The beauty of "no deadline" is that we can get each phase right before moving on.*
