#!/usr/bin/env python3
"""Generate temporary placeholder art assets for WarChess.

Creates simple pixel-art placeholders for all unit types, terrain tiles,
UI elements, commander portraits, and icons. These are stand-in assets
for testing purposes — not final art.

Usage:
    python3 tools/generate_placeholder_art.py [--only units|terrain|ui|all] [--verbose]
"""

import argparse
import random
import sys
from pathlib import Path
from PIL import Image, ImageDraw

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
OUTPUT_BASE = PROJECT_ROOT / "Assets" / "Art"

TILE = 32
ICON = 16
PORTRAIT = 64
FRAMES_PER_SHEET = 14  # idle(2) + move(4) + attack(4) + death(4)
SHEET_W = TILE * FRAMES_PER_SHEET  # 448

# Faction colours (R, G, B, A)
PLAYER_BODY = (0xCC, 0x22, 0x22, 255)
ENEMY_BODY = (0x22, 0x44, 0xAA, 255)
TRIM = (255, 255, 255, 255)
OUTLINE = (0x3B, 0x28, 0x20, 255)
WEAPON = (80, 80, 80, 255)
SKIN = (0xE8, 0xC0, 0x98, 255)

UNIT_TYPES = [
    "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier",
    "Rifleman", "Hussar", "Cuirassier", "HorseArtillery", "Sapper",
    "OldGuard", "RocketBattery", "Lancer", "Dragoon",
]

TERRAIN = {
    "OpenField":     ((0x5A, 0x8C, 0x3A), (0x4A, 0x7A, 0x2A)),
    "Forest":        ((0x2D, 0x5A, 0x1E), (0x1E, 0x4A, 0x10)),
    "Hill":          ((0xA0, 0x80, 0x50), (0x8A, 0x6A, 0x3A)),
    "River":         ((0x3A, 0x6A, 0x9C), (0x2A, 0x5A, 0x8C)),
    "Bridge":        ((0x8A, 0x7A, 0x6A), (0x6A, 0x5A, 0x4A)),
    "Fortification": ((0x6A, 0x6A, 0x6A), (0x5A, 0x5A, 0x5A)),
    "Mud":           ((0x6A, 0x50, 0x30), (0x5A, 0x40, 0x20)),
    "Town":          ((0xB0, 0x90, 0x70), (0x90, 0x70, 0x50)),
}

COMMANDERS = {
    "Wellington": (0xCC, 0x22, 0x22),
    "Napoleon":   (0x22, 0x44, 0xAA),
    "Kutuzov":    (0x22, 0x88, 0x44),
    "Blucher":    (0x22, 0x33, 0x66),
    "Moore":      (0xCC, 0x22, 0x22),
    "Ney":        (0x22, 0x44, 0xAA),
}

FORMATIONS = ["BattleLine", "Battery", "CavalryWedge", "Square", "SkirmishScreen"]

PARCHMENT = (0xF2, 0xE8, 0xD0, 255)
DARK_BROWN = (0x3B, 0x28, 0x20, 255)

# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------

def new_frame(size=TILE):
    return Image.new("RGBA", (size, size), (0, 0, 0, 0))


def draw_infantry_base(draw, body, cx=16, by=28, h=14, hat_h=4, hat_w=6, musket=True):
    """Generic standing infantry figure. Returns nothing, draws in place."""
    # Legs
    draw.rectangle([cx - 4, by - 2, cx - 2, by], fill=body)
    draw.rectangle([cx + 1, by - 2, cx + 3, by], fill=body)
    # Body
    bx0, by0 = cx - 5, by - 2 - h
    bx1, by1 = cx + 4, by - 2
    draw.rectangle([bx0, by0, bx1, by1], fill=body)
    # Trim cross-belts
    draw.line([bx0, by0 + 2, bx1, by1 - 2], fill=TRIM)
    draw.line([bx1, by0 + 2, bx0, by1 - 2], fill=TRIM)
    # Head
    draw.rectangle([cx - 3, by0 - 4, cx + 2, by0 - 1], fill=SKIN)
    # Hat
    hx0, hx1 = cx - hat_w // 2, cx + hat_w // 2
    draw.rectangle([hx0, by0 - 4 - hat_h, hx1, by0 - 4], fill=body)
    # Musket
    if musket:
        draw.line([cx + 5, by0, cx + 5, by0 - h + 2], fill=WEAPON, width=1)
    # Outline
    draw.rectangle([bx0, by0, bx1, by1], outline=OUTLINE)


def draw_mounted_base(draw, body, cx=16, by=28, rider_h=8, horse_w=18, horse_h=10):
    """Generic mounted figure."""
    # Horse body
    hx0 = cx - horse_w // 2
    hy0 = by - horse_h
    draw.ellipse([hx0, hy0, hx0 + horse_w, by], fill=body, outline=OUTLINE)
    # Horse legs
    for lx in [hx0 + 2, hx0 + 5, hx0 + horse_w - 6, hx0 + horse_w - 3]:
        draw.rectangle([lx, by, lx + 1, by + 2], fill=body)
    # Horse head
    draw.rectangle([hx0 + horse_w - 2, hy0 - 3, hx0 + horse_w + 2, hy0], fill=body)
    # Rider body
    rx0, ry0 = cx - 3, hy0 - rider_h
    draw.rectangle([rx0, ry0, rx0 + 6, hy0], fill=body, outline=OUTLINE)
    # Rider head
    draw.rectangle([cx - 2, ry0 - 4, cx + 2, ry0 - 1], fill=SKIN)
    # Trim belt
    draw.line([rx0, hy0 - 2, rx0 + 6, hy0 - 2], fill=TRIM)


# ---------------------------------------------------------------------------
# Unit silhouette drawers — each returns a 32×32 RGBA Image
# ---------------------------------------------------------------------------

def _draw_unit(unit_type, body):
    img = new_frame()
    d = ImageDraw.Draw(img)

    if unit_type == "LineInfantry":
        draw_infantry_base(d, body, hat_h=5, hat_w=6)

    elif unit_type == "Militia":
        draw_infantry_base(d, body, h=12, hat_h=3, hat_w=8)
        # Round hat (overwrite square hat)
        d.ellipse([13, 5, 19, 10], fill=body, outline=OUTLINE)

    elif unit_type == "Cavalry":
        draw_mounted_base(d, body)
        # Saber
        d.line([20, 8, 24, 4], fill=WEAPON, width=1)

    elif unit_type == "Artillery":
        # Cannon barrel
        d.rectangle([8, 18, 26, 22], fill=WEAPON, outline=OUTLINE)
        # Wheels
        d.ellipse([6, 22, 14, 30], fill=body, outline=OUTLINE)
        d.ellipse([18, 22, 26, 30], fill=body, outline=OUTLINE)
        # Carriage
        d.polygon([(10, 18), (22, 18), (16, 14)], fill=body, outline=OUTLINE)

    elif unit_type == "Grenadier":
        draw_infantry_base(d, body, hat_h=8, hat_w=7)
        # Tall bearskin rounded top
        d.ellipse([12, 1, 19, 7], fill=body)

    elif unit_type == "Rifleman":
        draw_infantry_base(d, body, h=12, hat_h=3, hat_w=7, musket=False)
        # Long rifle extended forward
        d.line([20, 14, 28, 10], fill=WEAPON, width=2)

    elif unit_type == "Hussar":
        draw_mounted_base(d, body, rider_h=7, horse_w=16, horse_h=9)
        # Plume on hat
        d.line([16, 3, 16, 0], fill=(255, 200, 0, 255), width=2)
        # Saber
        d.line([20, 8, 24, 5], fill=WEAPON, width=1)

    elif unit_type == "Cuirassier":
        draw_mounted_base(d, body, rider_h=9, horse_w=20, horse_h=11)
        # Armor breastplate (wider torso trim)
        d.rectangle([13, 7, 19, 15], fill=TRIM, outline=OUTLINE)
        # Helmet crest
        d.line([14, 2, 18, 2], fill=body, width=2)

    elif unit_type == "HorseArtillery":
        # Small cannon left
        d.rectangle([4, 20, 14, 23], fill=WEAPON, outline=OUTLINE)
        d.ellipse([3, 23, 9, 29], fill=body, outline=OUTLINE)
        # Horse right
        d.ellipse([14, 16, 28, 26], fill=body, outline=OUTLINE)
        # Horse head
        d.rectangle([26, 12, 30, 16], fill=body)
        # Traces
        d.line([14, 22, 10, 22], fill=WEAPON)

    elif unit_type == "Sapper":
        draw_infantry_base(d, body, hat_h=4, hat_w=6, musket=False)
        # Pickaxe over shoulder
        d.line([19, 6, 24, 2], fill=WEAPON, width=2)
        d.line([23, 1, 26, 3], fill=WEAPON, width=1)

    elif unit_type == "OldGuard":
        draw_infantry_base(d, body, hat_h=10, hat_w=7)
        # Very tall bearskin
        d.ellipse([12, 0, 19, 5], fill=body)
        # Bayonet
        d.line([21, 6, 21, 1], fill=WEAPON, width=1)
        d.point([21, 0], fill=TRIM)

    elif unit_type == "RocketBattery":
        # A-frame tripod
        d.line([10, 28, 16, 12], fill=WEAPON, width=2)
        d.line([22, 28, 16, 12], fill=WEAPON, width=2)
        d.line([10, 22, 22, 22], fill=WEAPON, width=1)
        # Rocket tube
        d.rectangle([14, 6, 18, 14], fill=body, outline=OUTLINE)
        # Rocket tip
        d.polygon([(14, 6), (18, 6), (16, 2)], fill=TRIM)

    elif unit_type == "Lancer":
        draw_mounted_base(d, body, rider_h=7, horse_w=16, horse_h=9)
        # Lance
        d.line([17, 14, 17, 0], fill=WEAPON, width=1)
        # Pennant
        d.polygon([(17, 1), (17, 4), (21, 2)], fill=body)

    elif unit_type == "Dragoon":
        draw_mounted_base(d, body, rider_h=8, horse_w=17, horse_h=10)
        # Carbine across body
        d.line([12, 10, 22, 10], fill=WEAPON, width=2)

    return img


# ---------------------------------------------------------------------------
# Animation frame generation
# ---------------------------------------------------------------------------

def _apply_offset(img, dx, dy, alpha=255):
    """Shift image contents by (dx, dy) pixels and optionally apply alpha."""
    result = new_frame(img.width)
    result.paste(img, (dx, dy))
    if alpha < 255:
        r, g, b, a = result.split()
        a = a.point(lambda v: min(v, alpha))
        result = Image.merge("RGBA", (r, g, b, a))
    return result


def generate_frames(unit_type, body):
    """Generate 14 animation frames for a unit type + faction colour."""
    base = _draw_unit(unit_type, body)
    frames = []
    # Idle: 2 frames (base, +1px up)
    frames.append(base.copy())
    frames.append(_apply_offset(base, 0, -1))
    # Move: 4 frames (bob pattern)
    for dy in [0, -1, 0, -1]:
        frames.append(_apply_offset(base, 0, dy))
    # Attack: 4 frames (wind-up, strike, recoil, return)
    frames.append(_apply_offset(base, -2, 0))
    frames.append(_apply_offset(base, 3, 0))
    frames.append(_apply_offset(base, -1, 0))
    frames.append(base.copy())
    # Death: 4 frames (hit, fall, settle, fade)
    frames.append(_apply_offset(base, 1, 0))
    frames.append(_apply_offset(base, 2, 2))
    frames.append(_apply_offset(base, 2, 4))
    frames.append(_apply_offset(base, 2, 4, alpha=128))
    return frames


def assemble_sheet(frames):
    """Combine frames into a horizontal sprite sheet."""
    sheet = Image.new("RGBA", (SHEET_W, TILE), (0, 0, 0, 0))
    for i, frame in enumerate(frames):
        sheet.paste(frame, (i * TILE, 0))
    return sheet


# ---------------------------------------------------------------------------
# Unit icons (16×16)
# ---------------------------------------------------------------------------

def generate_unit_icon(unit_type, body):
    """Simplified 16×16 icon for a unit type."""
    # Shrink the 32×32 base frame using nearest-neighbor
    big = _draw_unit(unit_type, body)
    icon = big.resize((ICON, ICON), Image.NEAREST)
    return icon


# ---------------------------------------------------------------------------
# Terrain tiles
# ---------------------------------------------------------------------------

def generate_terrain(name, primary, secondary):
    rng = random.Random(hash(name) & 0xFFFFFFFF)
    img = Image.new("RGBA", (TILE, TILE), primary + (255,))
    d = ImageDraw.Draw(img)
    sec = secondary + (255,)

    if name == "OpenField":
        # Grass tufts
        for _ in range(20):
            x, y = rng.randint(0, 31), rng.randint(0, 31)
            d.point([x, y], fill=sec)

    elif name == "Forest":
        # Tree triangles
        for tx in [6, 16, 26]:
            ty = rng.randint(8, 16)
            d.polygon([(tx, ty - 8), (tx - 5, ty + 4), (tx + 5, ty + 4)], fill=sec)
            d.rectangle([tx - 1, ty + 4, tx + 1, ty + 8], fill=(0x5A, 0x3A, 0x1E, 255))

    elif name == "Hill":
        # Contour lines
        for y in range(0, 32, 6):
            d.line([(0, y), (31, y + 2)], fill=sec, width=1)

    elif name == "River":
        # Wavy lines
        for y in range(0, 32, 5):
            pts = [(x, y + (2 if (x // 4) % 2 == 0 else 0)) for x in range(0, 32, 2)]
            d.line(pts, fill=sec, width=1)

    elif name == "Bridge":
        # Planks
        img_base = Image.new("RGBA", (TILE, TILE), (0x3A, 0x6A, 0x9C, 255))  # water
        d_base = ImageDraw.Draw(img_base)
        d_base.rectangle([6, 0, 25, 31], fill=primary + (255,))
        for y in range(0, 32, 4):
            d_base.line([(6, y), (25, y)], fill=sec, width=1)
        img = img_base

    elif name == "Fortification":
        # Crenellations
        for x in range(0, 32, 6):
            d.rectangle([x, 0, x + 3, 6], fill=sec)
        d.rectangle([0, 6, 31, 31], fill=primary + (255,))
        d.line([(0, 6), (31, 6)], fill=sec)

    elif name == "Mud":
        # Splotches
        for _ in range(15):
            x, y = rng.randint(0, 28), rng.randint(0, 28)
            d.ellipse([x, y, x + 3, y + 3], fill=sec)

    elif name == "Town":
        # Small buildings
        for bx in [3, 14, 24]:
            by = rng.randint(10, 18)
            d.rectangle([bx, by, bx + 8, by + 10], fill=sec, outline=OUTLINE)
            # Roof
            d.polygon([(bx - 1, by), (bx + 9, by), (bx + 4, by - 5)], fill=primary + (255,), outline=OUTLINE)

    return img


# ---------------------------------------------------------------------------
# UI elements
# ---------------------------------------------------------------------------

def generate_button(state):
    img = Image.new("RGBA", (48, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if state == "Normal":
        d.rounded_rectangle([0, 0, 47, 15], radius=3, fill=PARCHMENT, outline=DARK_BROWN, width=2)
    elif state == "Hover":
        hover = (0xE0, 0xD4, 0xB8, 255)
        d.rounded_rectangle([0, 0, 47, 15], radius=3, fill=hover, outline=DARK_BROWN, width=2)
    elif state == "Pressed":
        d.rounded_rectangle([0, 0, 47, 15], radius=3, fill=DARK_BROWN, outline=PARCHMENT, width=2)
    return img


def generate_panel():
    rng = random.Random(42)
    img = Image.new("RGBA", (64, 64), PARCHMENT)
    d = ImageDraw.Draw(img)
    # Subtle noise
    for _ in range(80):
        x, y = rng.randint(0, 63), rng.randint(0, 63)
        v = rng.randint(-10, 10)
        c = tuple(max(0, min(255, PARCHMENT[i] + v)) for i in range(3)) + (255,)
        d.point([x, y], fill=c)
    # Border
    d.rectangle([0, 0, 63, 63], outline=DARK_BROWN, width=2)
    return img


def generate_health_bar(color_name):
    colors = {
        "Green": (0x44, 0xAA, 0x44, 255),
        "Yellow": (0xCC, 0xAA, 0x22, 255),
        "Red": (0xCC, 0x22, 0x22, 255),
        "Background": DARK_BROWN,
    }
    img = Image.new("RGBA", (32, 4), colors[color_name])
    return img


# ---------------------------------------------------------------------------
# Commander portraits
# ---------------------------------------------------------------------------

def generate_portrait(name, uniform_color):
    img = Image.new("RGBA", (PORTRAIT, PORTRAIT), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    uc = uniform_color + (255,)

    # Background
    d.rectangle([0, 0, 63, 63], fill=(0x1A, 0x20, 0x40, 255))

    # Uniform body
    d.rectangle([16, 34, 48, 60], fill=uc, outline=OUTLINE)
    # Trim on uniform
    d.line([32, 34, 32, 60], fill=TRIM, width=1)
    d.line([16, 44, 48, 44], fill=TRIM, width=1)

    # Face
    d.ellipse([22, 12, 42, 34], fill=SKIN, outline=OUTLINE)
    # Eyes
    d.rectangle([27, 20, 29, 22], fill=OUTLINE)
    d.rectangle([35, 20, 37, 22], fill=OUTLINE)
    # Mouth
    d.line([29, 27, 35, 27], fill=OUTLINE)

    # Per-commander distinguishing features
    if name == "Wellington":
        # Bicorne horizontal
        d.polygon([(18, 12), (46, 12), (42, 6), (22, 6)], fill=OUTLINE)
    elif name == "Napoleon":
        # Bicorne vertical (front-facing)
        d.polygon([(26, 12), (38, 12), (32, 2)], fill=OUTLINE)
    elif name == "Kutuzov":
        # Fur hat (round)
        d.ellipse([20, 4, 44, 16], fill=(0x66, 0x55, 0x44, 255), outline=OUTLINE)
        # Eye patch
        d.rectangle([27, 20, 30, 23], fill=OUTLINE)
    elif name == "Blucher":
        # Prussian shako
        d.rectangle([24, 2, 40, 14], fill=OUTLINE)
        d.rectangle([26, 0, 38, 4], fill=uc)
        # Mustache
        d.line([28, 25, 36, 25], fill=OUTLINE, width=2)
    elif name == "Moore":
        # Bare head (just hair line)
        d.arc([22, 8, 42, 18], 180, 0, fill=OUTLINE, width=2)
    elif name == "Ney":
        # Shako with plume
        d.rectangle([24, 2, 40, 14], fill=uc, outline=OUTLINE)
        d.line([38, 2, 42, -2], fill=(255, 200, 0, 255), width=3)
        # Sideburns
        d.rectangle([22, 22, 24, 30], fill=OUTLINE)
        d.rectangle([40, 22, 42, 30], fill=OUTLINE)

    # Border
    d.rectangle([0, 0, 63, 63], outline=OUTLINE, width=2)
    return img


# ---------------------------------------------------------------------------
# Formation icons
# ---------------------------------------------------------------------------

def generate_formation_icon(name):
    img = new_frame(ICON)
    d = ImageDraw.Draw(img)
    c = TRIM

    if name == "BattleLine":
        for x in [2, 6, 10]:
            d.rectangle([x, 6, x + 3, 9], fill=c, outline=OUTLINE)
    elif name == "Battery":
        d.ellipse([2, 5, 7, 10], fill=c, outline=OUTLINE)
        d.ellipse([9, 5, 14, 10], fill=c, outline=OUTLINE)
    elif name == "CavalryWedge":
        d.polygon([(8, 2), (2, 10), (14, 10)], fill=c, outline=OUTLINE)
    elif name == "Square":
        for pos in [(3, 3), (9, 3), (3, 9), (9, 9)]:
            d.rectangle([pos[0], pos[1], pos[0] + 3, pos[1] + 3], fill=c, outline=OUTLINE)
    elif name == "SkirmishScreen":
        for pos in [(2, 3), (7, 8), (12, 4), (4, 12), (11, 11)]:
            d.ellipse([pos[0], pos[1], pos[0] + 2, pos[1] + 2], fill=c)

    return img


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Generate placeholder art assets for WarChess")
    parser.add_argument("--only", choices=["units", "terrain", "ui", "all"], default="all")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args()

    generated = 0

    def save(img, path):
        nonlocal generated
        path.parent.mkdir(parents=True, exist_ok=True)
        img.save(str(path))
        generated += 1
        if args.verbose:
            print(f"  {path.relative_to(PROJECT_ROOT)}  ({img.width}x{img.height})")

    factions = {"Player": PLAYER_BODY, "Enemy": ENEMY_BODY}

    # --- Units ---
    if args.only in ("units", "all"):
        print("Generating unit sprite sheets...")
        units_dir = OUTPUT_BASE / "Units"
        icons_dir = units_dir / "Icons"
        for ut in UNIT_TYPES:
            for faction, color in factions.items():
                frames = generate_frames(ut, color)
                sheet = assemble_sheet(frames)
                save(sheet, units_dir / f"{ut}_{faction}.png")
            # Icon (use player colour)
            icon = generate_unit_icon(ut, PLAYER_BODY)
            save(icon, icons_dir / f"{ut}_Icon.png")
        print(f"  -> {len(UNIT_TYPES) * 2} sheets + {len(UNIT_TYPES)} icons")

    # --- Terrain ---
    if args.only in ("terrain", "all"):
        print("Generating terrain tiles...")
        terrain_dir = OUTPUT_BASE / "Terrain"
        for name, (pri, sec) in TERRAIN.items():
            tile = generate_terrain(name, pri, sec)
            save(tile, terrain_dir / f"{name}.png")
        print(f"  -> {len(TERRAIN)} tiles")

    # --- UI ---
    if args.only in ("ui", "all"):
        print("Generating UI elements...")
        ui_dir = OUTPUT_BASE / "UI"

        # Buttons
        for state in ["Normal", "Hover", "Pressed"]:
            save(generate_button(state), ui_dir / f"Button_{state}.png")

        # Panel
        save(generate_panel(), ui_dir / "Panel_Background.png")

        # Health bars
        for color_name in ["Green", "Yellow", "Red", "Background"]:
            save(generate_health_bar(color_name), ui_dir / f"HealthBar_{color_name}.png")

        # Commander portraits
        print("Generating commander portraits...")
        portraits_dir = ui_dir / "Portraits"
        for name, color in COMMANDERS.items():
            save(generate_portrait(name, color), portraits_dir / f"{name}.png")

        # Formation icons
        print("Generating formation icons...")
        formations_dir = ui_dir / "Formations"
        for name in FORMATIONS:
            save(generate_formation_icon(name), formations_dir / f"{name}.png")

        ui_count = 3 + 1 + 4 + len(COMMANDERS) + len(FORMATIONS)
        print(f"  -> {ui_count} UI assets")

    print(f"\nDone! Generated {generated} files under Assets/Art/")


if __name__ == "__main__":
    main()
