# WarChess Marketing Templates

Last updated: 2026-04-03

This document contains ready-to-customize marketing templates for WarChess. Replace bracketed placeholders like [LINK], [DATE], and [VERSION] before posting.

---

## 1. Reddit Post Templates

### r/indiegaming — Dev Story Angle

**Title:** I'm a solo dev building a Napoleonic auto-battler with AI-assisted design. Here's WarChess.

**Body:**

Hey everyone,

I'm David, and for the past [TIMEFRAME] I've been building WarChess — a Napoleonic-era auto-battler where you build armies on a grid, deploy them, and watch them fight it out.

The twist: it's just me and Claude (an AI assistant) designing and building the whole thing. I handle the vision, code, and art direction. Claude helps me stress-test game design decisions, balance 14 different unit types, and catch the blind spots a solo dev inevitably has.

**What the game is:**
- Build your army from 14 Napoleonic-era unit types (line infantry, cavalry, artillery, skirmishers, and more)
- Deploy on a 10x10 grid — positioning and formation actually matter
- Watch your army fight automatically — the strategy is in the preparation
- 30-battle campaign across 3 acts
- Async multiplayer with a Star General tier system
- Pixel art, runs on PC and mobile
- No pay-to-win. Period.

**What makes it different from other auto-battlers:**
The battles are fully deterministic. Same army, same deployment, same enemy — same result, every time. There's no hidden RNG bail-out. If you lose, your strategy was wrong. If you win, you earned it.

I'd love to hear what you think. Happy to answer questions about solo dev life, working with AI as a design partner, or the game itself.

[SCREENSHOT/GIF]

[LINK TO WISHLIST/DEMO]

---

### r/gamedev — Mechanics Showcase Angle

**Title:** How I designed a deterministic auto-battle engine with no floating point math (and why it matters for async multiplayer)

**Body:**

Working on WarChess, a Napoleonic-era auto-battler, and I wanted to share a technical decision that shaped the entire project.

**The problem:** I wanted async multiplayer where your army gets stored in a pool and other players fight against it. But if battles aren't perfectly deterministic, replays break, disputes happen, and the tier system falls apart.

**The solution:** The entire battle engine runs on integer math only. No floats, no doubles, nowhere. All randomness uses seeded RNG. The engine runs headless — no Unity rendering dependencies — so I can simulate thousands of battles for balance testing without ever opening the game client.

**What this gets me:**
- Async multiplayer that actually works — your army fights the same way whether you're online or not
- A balance testing pipeline where I can pit every army composition against every other one
- Replay support for free — just store the seed and the inputs
- Battle logic that's fully unit-testable in plain C#

**The tradeoff:** Integer-only math means careful scaling. Damage formulas use basis-point multipliers (10000 = 1.0x) instead of floats. Flanking bonuses are stored as integer multipliers (front = 10000, side = 13000, rear = 20000). It's ugly in code but bulletproof in production.

The game has 14 unit types, Officers that modify units (each with one positive and one negative trait), and Commanders with army-wide abilities. All of it runs through this deterministic engine.

Happy to go deeper on any part of this. [LINK]

---

### r/PixelArt — Art Showcase Angle

**Title:** Pixel art Napoleonic soldiers for my auto-battler game — 14 unit types and counting

**Body:**

Working on WarChess, a Napoleonic-era auto-battler with pixel art. Here's a look at the unit roster:

[IMAGE GRID OF UNIT SPRITES]

The game has 14 unit types spanning infantry, cavalry, artillery, and specialist roles. Each one needed to read clearly on a 10x10 grid at mobile resolution, so silhouette and color coding do most of the heavy lifting.

[CLOSE-UP OF A FEW UNITS WITH IDLE/ATTACK ANIMATIONS]

Feedback welcome — especially on readability at small sizes. The game runs on phones so these need to work at pretty small scales.

[LINK TO GAME/WISHLIST]

---

## 2. Devlog Series Template

Use this format for numbered progress updates on itch.io, Reddit, or a personal blog.

---

**Title:** WarChess Devlog #[NUMBER]: [TOPIC]

**Opening — What happened this update:**

Brief 2-3 sentence summary of what was accomplished. Lead with the most interesting or visual thing.

**Section 1 — The main thing:**

Detailed write-up of the primary feature or milestone. Include screenshots or GIFs. Explain not just what you built, but why — what design problem it solves, what tradeoffs you made.

**Section 2 — Other progress:**

Bullet list of smaller changes, fixes, or improvements.

- Thing one
- Thing two
- Thing three

**Section 3 — What I learned / What went wrong:**

One honest paragraph about a challenge, mistake, or surprise. This is what makes devlogs worth reading.

**Next up:**

2-3 sentences about what you're working on next. Keep it concrete — "next week I'm implementing the Officer trait system" beats "more features coming soon."

**Closing:**

If you want to follow along: [LINKS TO DISCORD/TWITTER/WISHLIST]

Thanks for reading. Questions and feedback always welcome.

— David

---

## 3. Twitter/X Thread Template — Launch Announcement

**Tweet 1 (Hook):**
I've been building a game by myself for [TIMEFRAME].

Today I'm showing it to the world.

WarChess is a Napoleonic-era auto-battler with pixel art, deterministic battles, and zero pay-to-win.

Here's what it is and why I made it. Thread:

[KEY ART OR GIF]

**Tweet 2 (The pitch):**
You build an army from 14 Napoleonic-era unit types — line infantry, cavalry, artillery, skirmishers, and more.

You deploy them on a 10x10 grid.

Then you watch them fight.

The strategy is in the preparation. Every decision happens before the battle starts.

**Tweet 3 (What makes it different):**
Most auto-battlers have hidden RNG that can bail you out or screw you over.

WarChess battles are fully deterministic. Same inputs = same outputs. Every time.

If you lose, your strategy was the problem. Not luck.

**Tweet 4 (Campaign):**
The single-player campaign has 30 battles across 3 acts.

Act 1 is free. Acts 2-3 are a one-time purchase.

No season passes. No battle passes. No daily login rewards. You buy the game and you have the game.

**Tweet 5 (Multiplayer):**
Multiplayer is async. You build an army, submit it to the pool, and other players fight against it.

No waiting in lobbies. No rage-quitting opponents.

Climb the Star General tier system from 1-star to 5-star based on how your armies perform.

**Tweet 6 (Officers and Commanders):**
Every unit can have an Officer attached — a named character with one positive trait and one negative trait.

Your army also has a Commander with army-wide abilities.

This is where the real depth lives. Same units, different Officers, completely different outcomes.

**Tweet 7 (The tech):**
The battle engine runs on pure integer math. No floats. Seeded RNG only.

It runs headless — no rendering required — so I can simulate thousands of battles for balance testing.

This is how one person balances 14 unit types without shipping a broken meta.

**Tweet 8 (Solo dev + AI):**
I built this solo with AI-assisted design (Claude).

I make the decisions. The AI helps me stress-test them — poking holes in balance spreadsheets, catching edge cases, and asking "what happens when a player does X?"

It's not AI-generated. It's AI-assisted. There's a difference.

**Tweet 9 (Art):**
The art is pixel art because I love pixel art and because it scales from phone screens to desktop monitors without looking stretched or blurry.

[PIXEL ART SHOWCASE IMAGE]

**Tweet 10 (CTA):**
WarChess is coming to PC, iOS, and Android.

If any of this sounds interesting:
- Wishlist on [PLATFORM]: [LINK]
- Join the Discord: [LINK]
- Follow for devlogs: [LINK]

Thanks for reading. Back to work.

---

## 4. Press Release Template

FOR IMMEDIATE RELEASE

**WarChess: Napoleonic Auto-Battler Combines Deterministic Strategy with Ethical Monetization**

*Solo developer delivers 30-battle campaign and async multiplayer — no pay-to-win mechanics*

[CITY], [DATE] — Solo developer David today announced WarChess, a Napoleonic-era auto-battler for PC, iOS, and Android. The game features 14 historically-inspired unit types, a 30-battle single-player campaign, and an async multiplayer system built on a fully deterministic battle engine.

**Overview**

WarChess challenges players to build and deploy armies on a 10x10 grid, then watch them fight automatically. Victory depends entirely on army composition, unit placement, and strategic use of Officers and Commanders — named characters that modify individual units and entire armies, respectively.

The battle engine uses integer-only math and seeded randomness to guarantee identical outcomes from identical inputs. This deterministic foundation enables the game's async multiplayer mode, where players submit armies to a shared pool and climb a five-tier Star General ranking system based on performance.

**Key Features**

- 14 Napoleonic-era unit types across infantry, cavalry, artillery, and specialist roles
- 30-battle campaign across 3 acts with escalating difficulty and new unit unlocks
- Async multiplayer with the Star General tier system (1-star through 5-star)
- Officers with paired positive/negative traits that create meaningful unit customization
- Commanders with army-wide triggered abilities
- Fully deterministic battle engine — no hidden RNG
- Pixel art visual style designed for readability on mobile and desktop
- Ethical monetization: Act 1 is free, Acts 2-3 are a single purchase, no pay-to-win

**Availability**

WarChess is currently in development with a planned [EARLY ACCESS / BETA / LAUNCH] in [TIMEFRAME]. Act 1 will be available for free, with Acts 2-3 available as a one-time purchase of [PRICE].

**Press Kit**

A full press kit with screenshots, key art, and gameplay footage is available at [LINK].

**Contact**

David — [EMAIL]
[WEBSITE]
[TWITTER/X HANDLE]

###

---

## 5. Beta Signup Call-to-Action

### Discord Invite (Short)

WarChess is a Napoleonic auto-battler built by one person.

14 unit types. Deterministic battles. No pay-to-win.

Beta testing starts [TIMEFRAME]. Join the Discord to get early access and help shape the game.

[DISCORD INVITE LINK]

### Discord Invite (Longer — for landing pages or devlog footers)

WarChess is heading into beta and I need players who want to break it.

What you get:
- Early access to builds before public release
- Direct feedback channel — I read everything
- Your name in the credits if you want it
- A voice in balance decisions (14 unit types need a lot of testing)

What I need from you:
- Play the game and tell me what feels wrong
- Try to find broken army compositions
- Report bugs, even small ones

No NDA. No corporate nonsense. Just a solo dev who needs testers.

[DISCORD INVITE LINK]

### Email Signup (for a landing page)

**Get notified when WarChess launches.**

A Napoleonic auto-battler with 14 unit types, deterministic battles, and zero pay-to-win. Built by one developer. Coming to PC, iOS, and Android.

[EMAIL INPUT FIELD] [SIGN UP BUTTON]

No spam. One email at launch, one email if there's a beta. That's it.

---

## 6. Elevator Pitch

### One Sentence

WarChess is a Napoleonic-era auto-battler where you build armies on a grid and watch them fight in fully deterministic pixel-art battles — no hidden RNG, no pay-to-win.

### Three Sentences

WarChess is a Napoleonic auto-battler where the strategy happens before the battle starts. You pick from 14 unit types, assign Officers with unique traits, deploy on a 10x10 grid, and watch the fight play out — same inputs always produce the same result. It has a 30-battle campaign, async multiplayer with a tier system, and ethical monetization where Act 1 is free and the rest is a single purchase.

### One Paragraph

WarChess is a Napoleonic-era auto-battler for PC, iOS, and Android, built by a solo developer with AI-assisted design. Players build armies from 14 historically-inspired unit types — line infantry, cavalry, artillery, and specialists — then deploy them on a 10x10 grid and watch them fight automatically. Every battle is fully deterministic: no hidden dice rolls, no random bail-outs. Officers with paired positive and negative traits let you customize individual units, while Commanders grant army-wide abilities. The 30-battle campaign spans three acts with escalating difficulty and new unit unlocks. Async multiplayer lets you submit armies to a shared pool and climb five Star General tiers without waiting in lobbies. Monetization is straightforward: Act 1 is free, Acts 2 and 3 are a one-time purchase, and nothing you can buy makes your army stronger.

---

## 7. Feature Comparison

### Why WarChess vs Other Auto-Battlers

| Feature | Typical Auto-Battler | WarChess |
|---|---|---|
| **Battle outcome** | RNG-dependent, inconsistent results | Fully deterministic — same inputs, same result |
| **Monetization** | Season passes, loot boxes, power-gating | Act 1 free, Acts 2-3 one-time buy, no pay-to-win |
| **Multiplayer model** | Real-time, requires both players online | Async army pool — play on your schedule |
| **Unit customization** | Random item drops, random upgrades | Officers with defined positive + negative traits |
| **Theme** | Fantasy or sci-fi | Napoleonic-era historical |
| **Platform** | Usually one platform | PC, iOS, and Android |
| **Visual style** | 3D or stylized 2D | Pixel art designed for readability at all sizes |
| **Solo dev transparency** | Corporate marketing speak | One developer, open devlogs, honest communication |

### Why WarChess vs Traditional Strategy Games

| Feature | Traditional Strategy | WarChess |
|---|---|---|
| **Time commitment** | 30-60 min per match | Battles resolve in minutes |
| **Complexity** | Deep micro-management during battle | All decisions happen before the battle starts |
| **Accessibility** | Steep learning curve | Simple to learn — army building is intuitive |
| **Mobile support** | Often a compromised port | Designed mobile-first, scaled up for PC |
| **Multiplayer friction** | Finding opponents, waiting in lobbies, disconnects | Async — submit your army and check results later |

---

## Usage Notes

- Replace all [BRACKETED PLACEHOLDERS] before posting
- Attach screenshots, GIFs, or video wherever templates indicate — visual content dramatically increases engagement
- Adjust tone per platform: Reddit is more casual and conversational, press releases are formal, Twitter is punchy
- Post Reddit content during peak hours for target subreddits (typically weekday evenings US time)
- Cross-post devlogs to multiple platforms but adjust formatting for each
- Always include a clear call-to-action: wishlist link, Discord invite, or follow button
