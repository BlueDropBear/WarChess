# WarChess — Audio Sourcing Guide

## Overview

All audio for WarChess must be free (CC0 or CC-BY compatible). This guide maps each SoundEvent and MusicTrack to recommended sources, search terms, and specifications.

---

## 1. Free Audio Sources

### Sound Effects
| Source | License | URL | Notes |
|--------|---------|-----|-------|
| Freesound.org | CC0/CC-BY | freesound.org | Largest free SFX library. Filter by CC0. |
| OpenGameArt.org | CC0/CC-BY | opengameart.org | Game-specific packs. |
| Pixabay Sound Effects | Pixabay License (free) | pixabay.com/sound-effects | No attribution required. |
| Kenney.nl | CC0 | kenney.nl | UI sound packs, impact sounds. |
| Mixkit | Free License | mixkit.co | Curated free SFX. |

### Music
| Source | License | URL | Notes |
|--------|---------|-----|-------|
| Kevin MacLeod (incompetech.com) | CC-BY 3.0 | incompetech.com | Huge library, era-appropriate classical available. |
| OpenGameArt.org | CC0/CC-BY | opengameart.org | Search "orchestral", "military march". |
| Free Music Archive | Various CC | freemusicarchive.org | Filter by CC0/CC-BY. |
| Musopen | Public Domain | musopen.org | Classical recordings, public domain. |
| Purple Planet Music | Royalty Free | purple-planet.com | Free with attribution. |

---

## 2. SFX Requirements per SoundEvent

### Combat Sounds
| SoundEvent | Description | Search Terms | Duration |
|-----------|-------------|--------------|----------|
| UnitAttack | Musket/rifle shot, sharp crack | "musket fire", "flintlock shot", "rifle crack" | 0.3-0.8s |
| UnitDeath | Body falling, brief death sound | "body fall", "death thud", "soldier fall" | 0.5-1.0s |
| UnitCharge | Cavalry charge, hooves + yell | "horse gallop", "cavalry charge", "battle cry" | 1.0-2.0s |
| UnitAbility | Drum roll or bugle call | "military drum roll", "bugle call", "trumpet signal" | 0.5-1.5s |

### Battle Flow
| SoundEvent | Description | Search Terms | Duration |
|-----------|-------------|--------------|----------|
| BattleStart | War drum hit + horn blast | "war drum hit", "battle horn", "trumpet fanfare" | 1.0-2.0s |
| BattleWin | Triumphant fanfare, cheering | "victory fanfare", "crowd cheer military", "triumph" | 2.0-3.0s |
| BattleLose | Somber horn, low drum | "defeat sound", "somber horn", "funeral drum" | 1.5-2.5s |
| BattleDraw | Ambiguous tone, fade | "stalemate", "neutral outcome", "draw sound" | 1.0-2.0s |

### UI Sounds
| SoundEvent | Description | Search Terms | Duration |
|-----------|-------------|--------------|----------|
| UnitPlace | Solid placement thud | "piece place", "stone click", "chess piece" | 0.2-0.4s |
| ButtonClick | Crisp click | "button click", "UI click", "menu select" | 0.1-0.2s |
| ButtonBack | Soft reverse click | "button back", "UI cancel", "menu back" | 0.1-0.3s |
| MenuOpen | Parchment unroll | "scroll unfurl", "parchment open", "paper" | 0.3-0.6s |
| MenuClose | Parchment roll up | "scroll roll", "paper close" | 0.2-0.5s |
| ErrorBuzz | Short negative buzz | "error buzz", "wrong answer", "negative beep" | 0.2-0.4s |

### Reward Sounds
| SoundEvent | Description | Search Terms | Duration |
|-----------|-------------|--------------|----------|
| PurchaseSuccess | Coin clink + register | "coin sound", "purchase ding", "cash register" | 0.3-0.6s |
| DispatchBoxOpen | Chest/box opening with sparkle | "chest open", "treasure chest", "box unlock" | 0.5-1.0s |
| StarEarned | Bright ascending chime | "star earn", "achievement chime", "level up ding" | 0.3-0.6s |
| TierPromotion | Grand ascending fanfare | "rank up", "promotion fanfare", "level up grand" | 1.0-2.0s |
| OfficerLevelUp | Military rank announcement | "rank up short", "promotion sting" | 0.5-1.0s |
| DeployArmy | March drum + boot step | "march drum", "army deploy", "troop march" | 0.5-1.0s |

---

## 3. Music Requirements per Track

### Menu & Navigation
| MusicTrack | Mood | Tempo | Instruments | Duration |
|-----------|------|-------|-------------|----------|
| MainMenu | Grand, inviting, Napoleonic | Moderate (90-110 BPM) | Strings, fife, snare drum, French horn | 2-3 min loop |
| ArmyBuilder | Contemplative, strategic | Slow (70-90 BPM) | Solo piano or string quartet | 2-3 min loop |
| CampaignMap | Adventurous, map-exploring | Moderate (80-100 BPM) | Orchestral, light percussion | 2-3 min loop |
| Shop | Pleasant, relaxed | Moderate (85-95 BPM) | Harpsichord, light strings | 1-2 min loop |

### Battle
| MusicTrack | Mood | Tempo | Instruments | Duration |
|-----------|------|-------|-------------|----------|
| BattleCalm | Tense, strategic, pre-battle | Slow (60-80 BPM) | Strings tremolo, timpani, low brass | 2-3 min loop |
| BattleIntense | Urgent, chaotic, climactic | Fast (120-140 BPM) | Full orchestra, snare rolls, brass fanfares | 2-3 min loop |

### Outcome
| MusicTrack | Mood | Tempo | Instruments | Duration |
|-----------|------|-------|-------------|----------|
| Victory | Triumphant, celebratory | Moderate (100-120 BPM) | Brass fanfare, full orchestra, timpani | 30-60s (non-loop) |
| Defeat | Somber, reflective, dignified | Slow (50-70 BPM) | Solo cello or violin, muted brass | 30-60s (non-loop) |

### Recommended Kevin MacLeod Tracks (incompetech.com)
- **MainMenu:** "Heroic Age" or "Crusade"
- **BattleCalm:** "Prelude and Action" or "Dark Times"
- **BattleIntense:** "All This" or "Volatile Reaction"
- **Victory:** "Fanfare for Space" (fanfare section)
- **Defeat:** "Funeral March for Brass" or "Sad Trio"
- **CampaignMap:** "Scheming Weasel" (slower version) or "Investigations"

---

## 4. File Format Specifications

### Sound Effects
- **Format:** WAV (uncompressed for mobile performance)
- **Sample Rate:** 44,100 Hz
- **Bit Depth:** 16-bit
- **Channels:** Mono (spatial positioning not needed for 2D game)
- **Max file size:** 200 KB per SFX clip
- **Normalization:** Peak normalize to -3 dBFS

### Music
- **Format:** OGG Vorbis (compressed, good quality, Unity-native)
- **Sample Rate:** 44,100 Hz
- **Quality:** VBR ~160 kbps
- **Channels:** Stereo
- **Max file size:** 3 MB per track (aim for smaller on mobile)
- **Normalization:** RMS normalize to -14 LUFS (standard for game music)

### Naming Convention
```
Audio/
├── SFX/
│   ├── sfx_unit_attack.wav
│   ├── sfx_unit_death.wav
│   ├── sfx_unit_charge.wav
│   ├── sfx_battle_start.wav
│   ├── sfx_battle_win.wav
│   ├── sfx_battle_lose.wav
│   ├── sfx_button_click.wav
│   ├── sfx_dispatch_open.wav
│   └── ...
└── Music/
    ├── mus_main_menu.ogg
    ├── mus_army_builder.ogg
    ├── mus_campaign_map.ogg
    ├── mus_battle_calm.ogg
    ├── mus_battle_intense.ogg
    ├── mus_victory.ogg
    ├── mus_defeat.ogg
    └── mus_shop.ogg
```

---

## 5. License Compatibility

| License | Attribution Required | Commercial Use | Modification | Compatible |
|---------|---------------------|---------------|--------------|-----------|
| CC0 | No | Yes | Yes | Best choice |
| CC-BY 3.0/4.0 | Yes (in credits) | Yes | Yes | Good — add to credits screen |
| CC-BY-SA | Yes | Yes | Must share alike | OK for final assets, not for derived works |
| CC-BY-NC | Yes | **No** | Yes | **NOT compatible** (game is commercial) |
| Pixabay License | No | Yes | Yes | Good choice |
| Public Domain | No | Yes | Yes | Best choice |

### Attribution Template (for CC-BY assets)
Add to Settings > Credits screen:
```
AUDIO CREDITS
Sound effects from Freesound.org:
- "musket_fire.wav" by [username] (CC-BY 3.0)
- "cavalry_charge.wav" by [username] (CC-BY 3.0)

Music:
- "Track Name" by Kevin MacLeod (incompetech.com)
  Licensed under Creative Commons: By Attribution 3.0
  http://creativecommons.org/licenses/by/3.0/
```

---

## 6. Mobile Optimization Notes

- **Total audio budget:** Aim for under 15 MB total (SFX + music)
- **Music streaming:** Use Unity's `AudioClip.LoadType.Streaming` for music tracks to reduce memory
- **SFX preloading:** Use `AudioClip.LoadType.DecompressOnLoad` for frequently-used SFX
- **Compression override:** On mobile, Unity can further compress OGG. Use "Vorbis" quality 70% for mobile builds
- **Simultaneous sounds:** Limit to 8 concurrent audio sources to avoid performance issues on low-end devices
- **Volume ducking:** During battle, duck music to 50% volume when combat SFX play
