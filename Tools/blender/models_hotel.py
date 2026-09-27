"""Level 5 - Terror Hotel custom models (1930s hotel).
  blender -b --factory-startup -P models_hotel.py
"""
import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import *  # noqa

SERIF = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "SpecialElite-Regular.ttf")


def raised_panel(name, w, h, loc, material, depth=0.018):
    """Classic raised/bevelled door panel on the front (-Y) face."""
    p = box(name, (w, depth, h), loc, material, bevel=depth * 0.8, segments=3)
    return p


@model
def build_Hotel_DoorLeaf():
    """Six-panel walnut door, pivot at hinge (left edge from front), bottom. 0.95 x 2.2 x 0.05."""
    w, h, t = 0.95, 2.2, 0.05
    box("slab", (w, t, h), (w / 2, 0, 0.005), "L5_Walnut", bevel=0.004)
    for side in (-1, 1):
        y = side * (t / 2 + 0.006)
        for row, (z, ph) in enumerate(((0.12, 0.55), (0.8, 0.6), (1.52, 0.55))):
            for col in range(2):
                x = 0.1 + col * (w - 0.2) / 2 + (w - 0.2) / 4
                raised_panel(f"p{side}{row}{col}", (w - 0.28) / 2, ph, (x, y, z), "L5_Walnut")
    # brass hardware
    for side in (-1, 1):
        y = side * (t / 2 + 0.004)
        box(f"plate{side}", (0.05, 0.006, 0.24), (w - 0.075, y, 0.9), "Brass", bevel=0.002)
        sphere(f"knob{side}", 0.028, (w - 0.075, side * (t / 2 + 0.06), 1.04), "Brass", scale=(1, 0.8, 1))
        cyl(f"shank{side}", 0.01, 0.05, (w - 0.075, side * (t / 2), 1.04), "Brass", 16, rot=(side * math.pi / 2, 0, 0))
        box(f"keyhole{side}", (0.006, 0.008, 0.016), (w - 0.075, side * (t / 2 + 0.008), 0.94), "PaintBlack")
    # number plate (front) - digits are separate prefabs placed by the level builder
    box("numplate", (0.2, 0.006, 0.09), (w / 2, -(t / 2 + 0.006), 1.62), "Brass", bevel=0.003)
    box("kickplate", (w - 0.04, 0.003, 0.18), (w / 2, -(t / 2 + 0.003), 0.02), "Brass")


@model
def build_Hotel_DoorFrame():
    """Moulded casing around a 0.95 x 2.2 opening in a 0.24m wall (both faces)."""
    w, h, d = 0.95, 2.2, 0.24
    for side in (-1, 1):
        y = side * (d / 2 + 0.012)
        for sx in (-1, 1):
            x = sx * (w / 2 + 0.06)
            box(f"cas{side}{sx}", (0.12, 0.024, h + 0.12), (x, y, 0), "L5_WoodPanel", bevel=0.006)
            cyl(f"bead{side}{sx}", 0.012, h, (sx * (w / 2 + 0.008), y + side * 0.01, 0), "L5_WoodPanel", 12)
            box(f"plinth{side}{sx}", (0.14, 0.04, 0.22), (x, y, 0), "L5_WoodPanel", bevel=0.008)
        box(f"head{side}", (w + 0.24, 0.024, 0.12), (0, y, h), "L5_WoodPanel", bevel=0.006)
        box(f"cornice{side}", (w + 0.36, 0.07, 0.05), (0, y + side * 0.02, h + 0.12), "L5_WoodPanel", bevel=0.012)
        box(f"frieze{side}", (w + 0.3, 0.04, 0.08), (0, y + side * 0.01, h + 0.17), "L5_WoodPanel", bevel=0.006)
    box("jambL", (0.02, d, h), (-w / 2 - 0.01, 0, 0), "L5_WoodPanel")
    box("jambR", (0.02, d, h), (w / 2 + 0.01, 0, 0), "L5_WoodPanel")
    box("head", (w + 0.04, d, 0.02), (0, 0, h), "L5_WoodPanel")


PLEX = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "IBMPlexMono-SemiBold.ttf")


def digit_model(ch):
    def build():
        text("d", ch, 0.07, "Brass", loc=(0, 0, 0.035), extrude=0.002, font=PLEX)
    build.__name__ = f"build_Digit_{ch}"
    return model(build)


@model
def build_Wainscot():
    """1m wide wood wainscot section (1.0m tall) with raised panel, cap rail and baseboard. Back at Y=0, front -Y."""
    box("board", (1.0, 0.018, 1.0), (0, -0.009, 0), "L5_WoodPanel")
    raised_panel("panel", 0.78, 0.62, (0, -0.024, 0.2), "L5_WoodPanel", depth=0.016)
    base_prof = [(0, 0), (-0.028, 0), (-0.028, 0.12), (-0.02, 0.135), (-0.022, 0.15), (0, 0.16)]
    extrude_profile("base", base_prof, 1.0, "L5_WoodPanel", loc=(-0.5, 0, 0))
    cap_prof = [(0, 0.95), (-0.035, 0.95), (-0.045, 0.965), (-0.04, 0.985), (-0.05, 1.0), (-0.05, 1.03), (0, 1.03)]
    extrude_profile("cap", cap_prof, 1.0, "L5_WoodPanel", loc=(-0.5, 0, 0))


@model
def build_CrownMolding():
    """1m crown molding, sits in the wall/ceiling corner: wall at Y=0 (back), ceiling at Z=0 (top)."""
    prof = [(0, -0.22), (-0.015, -0.21), (-0.02, -0.19), (-0.045, -0.16), (-0.06, -0.14), (-0.1, -0.1),
            (-0.13, -0.06), (-0.14, -0.04), (-0.17, -0.025), (-0.18, 0.0), (0, 0)]
    extrude_profile("crown", prof, 1.0, "PaintCream", loc=(-0.5, 0, 0), closed=True)
    box("dentil_band", (1.0, 0.02, 0.03), (0, -0.03, -0.25), "PaintCream")
    for i in range(10):
        box(f"dentil{i}", (0.035, 0.02, 0.03), (-0.45 + i * 0.1, -0.045, -0.25), "PaintCream")


@model
def build_Column_ArtDeco():
    """Square marble column with brass bands and stepped capital. 0.5 x 0.5 x 3.6."""
    H = 3.6
    box("base", (0.62, 0.62, 0.18), (0, 0, 0), "L5_Marble", bevel=0.01)
    box("base2", (0.56, 0.56, 0.08), (0, 0, 0.18), "Brass", bevel=0.006)
    box("shaft", (0.48, 0.48, H - 0.66), (0, 0, 0.26), "L5_Marble", bevel=0.008)
    for s in range(4):
        ang = s * math.pi / 2
        for k in (-1, 0, 1):
            off = k * 0.12
            x = math.cos(ang) * 0.245 - math.sin(ang) * off
            y = math.sin(ang) * 0.245 + math.cos(ang) * off
            box(f"flute{s}{k}", (0.03, 0.012, H - 1.0), (x, y, 0.45), "Brass", rot=(0, 0, ang + math.pi / 2), bevel=0.004)
    box("band", (0.52, 0.52, 0.05), (0, 0, H - 0.52), "Brass", bevel=0.005)
    box("cap1", (0.58, 0.58, 0.12), (0, 0, H - 0.4), "L5_Marble", bevel=0.01)
    box("cap2", (0.68, 0.68, 0.1), (0, 0, H - 0.28), "L5_Marble", bevel=0.01)
    box("cap3", (0.78, 0.78, 0.18), (0, 0, H - 0.18), "PaintCream", bevel=0.01)


@model
def build_Sconce_ArtDeco():
    """Wall sconce: brass back plate, fan-shaped frosted glass shade, warm bulb. Wall at Y=0, pivot at bulb height."""
    box("plate", (0.1, 0.012, 0.26), (0, -0.006, -0.16), "Brass", bevel=0.004)
    cyl("arm", 0.012, 0.12, (0, -0.012, -0.06), "Brass", 12, rot=(math.pi / 2, 0, 0))
    shade = lathe("shade", [(0.03, -0.03), (0.06, 0.0), (0.1, 0.07), (0.11, 0.1), (0.1, 0.1), (0.09, 0.07), (0.05, 0.0), (0.02, -0.03)],
                  "GlassFrosted", loc=(0, -0.13, -0.06), angle=math.pi, steps=24, rot=(0, 0, 0))
    lathe("cup", [(0.0, -0.06), (0.035, -0.06), (0.04, -0.03), (0.0, -0.03)], "Brass", loc=(0, -0.13, -0.06), steps=20)
    sphere("bulb", 0.028, (0, -0.13, 0.0), "EmissiveWarm", scale=(1, 1, 1.3))


@model
def build_Gramophone():
    """Horn gramophone on a walnut cabinet. Pivot bottom center, horn points toward -Y."""
    box("cabinet", (0.38, 0.38, 0.16), (0, 0, 0), "L5_Walnut", bevel=0.01)
    box("plinth", (0.4, 0.4, 0.02), (0, 0, 0), "L5_Walnut", bevel=0.004)
    cyl("platter", 0.14, 0.012, (0, 0.02, 0.16), "Rubber", 48)
    cyl("record", 0.13, 0.004, (0, 0.02, 0.172), "PaintBlack", 48)
    cyl("labelr", 0.04, 0.0045, (0, 0.02, 0.172), "PaintRed", 32)
    cyl("spindle", 0.004, 0.02, (0, 0.02, 0.172), "Steel", 8)
    box("crank", (0.012, 0.012, 0.1), (0.2, 0, 0.05), "Brass", rot=(0, math.radians(90), 0))
    cyl("arm_base", 0.02, 0.06, (0.13, 0.13, 0.16), "Brass", 16)
    # tone arm curving into the horn
    cyl("tonearm", 0.012, 0.2, (0.13, 0.13, 0.21), "Brass", 12, rot=(math.radians(90), 0, math.radians(35)), r2=0.018)
    horn = [(0.015, 0.0), (0.02, 0.06), (0.03, 0.14), (0.05, 0.24), (0.09, 0.34), (0.16, 0.42), (0.25, 0.47), (0.26, 0.475),
            (0.255, 0.48), (0.24, 0.475), (0.15, 0.43), (0.08, 0.35), (0.04, 0.25), (0.02, 0.15), (0.012, 0.06), (0.01, 0.0)]
    lathe("horn", horn, "Brass", loc=(0.02, 0.02, 0.32), rot=(math.radians(-70), 0, 0), steps=48)


@model
def build_ReceptionDesk():
    """3m reception counter: walnut body with fluted front, marble top, brass trim, bell."""
    L = 3.0
    box("body", (L, 0.6, 1.05), (0, 0, 0), "L5_Walnut", bevel=0.01)
    box("kick", (L - 0.04, 0.58, 0.1), (0, 0.02, 0), "PaintBlack")
    for i in range(24):
        x = -L / 2 + 0.1 + i * (L - 0.2) / 23
        box(f"flute{i}", (0.04, 0.02, 0.78), (x, -0.31, 0.16), "L5_Walnut", bevel=0.008)
    box("trim", (L + 0.02, 0.62, 0.03), (0, 0, 0.95), "Brass", bevel=0.004)
    box("top", (L + 0.12, 0.72, 0.05), (0, 0, 1.05), "L5_Marble", bevel=0.012)
    box("lower_shelf", (L - 0.1, 0.3, 0.03), (0, 0.2, 0.75), "L5_Walnut")
    lathe("bell", [(0.0, 0.0), (0.045, 0.0), (0.045, 0.01), (0.04, 0.015), (0.038, 0.03), (0.02, 0.05), (0.004, 0.055), (0.004, 0.065), (0.008, 0.07), (0, 0.072)],
          "Brass", loc=(0.6, -0.05, 1.1), steps=32)
    box("ledger", (0.32, 0.24, 0.03), (-0.5, 0.0, 1.1), "PaintRed", bevel=0.005)
    box("ledger_pages", (0.3, 0.22, 0.012), (-0.5, 0.0, 1.13), "Com_Paper")


@model
def build_Boiler():
    """Large vintage horizontal boiler with firebox door, gauges, valves and flue. Pivot bottom center. ~4m long."""
    L, R = 3.6, 0.85
    cyl("shell", R, L, (0, L / 2, R + 0.35), "L5_Rust", 48, rot=(math.pi / 2, 0, 0))
    for i in range(7):
        torus(f"band{i}", R + 0.01, 0.018, (0, -L / 2 + 0.2 + i * (L - 0.4) / 6, R + 0.35), "DarkMetal", rot=(math.pi / 2, 0, 0), seg=48)
    lathe("endcap", [(0, 0), (R, 0), (R * 0.95, -0.12), (R * 0.7, -0.2), (0, -0.24)], "L5_Rust", loc=(0, -L / 2, R + 0.35), rot=(-math.pi / 2, 0, 0))
    box("door", (0.6, 0.06, 0.5), (0, -L / 2 - 0.23, 0.55), "DarkMetal", bevel=0.02)
    box("latch", (0.2, 0.05, 0.04), (0.18, -L / 2 - 0.28, 0.8), "Brass")
    for x in (-0.55, 0.55):
        box(f"saddle{x}", (0.3, 0.5, R + 0.35), (x * 0, -L / 2 + 0.6 + (x > 0) * (L - 1.2), 0), "L5_Brick", bevel=0.01)
    cyl("flue", 0.22, 2.6, (0, L / 2 - 0.5, 2 * R + 0.3), "DarkMetal", 32)
    for i, (x, z) in enumerate(((-0.35, 1.9), (0.35, 1.9))):
        cyl(f"gauge{i}", 0.09, 0.05, (x, -L / 2 - 0.26, z - 0.6), "Brass", 32, rot=(math.pi / 2, 0, 0))
        cyl(f"face{i}", 0.078, 0.004, (x, -L / 2 - 0.312, z - 0.6), "PaintCream", 32, rot=(math.pi / 2, 0, 0))
        box(f"needle{i}", (0.004, 0.003, 0.06), (x, -L / 2 - 0.316, z - 0.6), "PaintBlack", rot=(0, math.radians(40 + i * 70), 0))
    for i, y in enumerate((-0.6, 0.4)):
        cyl(f"pipe{i}", 0.06, 1.2, (0.3, y, 2 * R + 0.3), "DarkMetal", 20)
        torus(f"wheel{i}", 0.12, 0.012, (0.3, y, 2 * R + 0.9), "PaintRed", seg=24)
        box(f"spoke{i}", (0.24, 0.015, 0.015), (0.3, y, 2 * R + 0.89), "PaintRed")
    for k in range(40):
        a = k / 40 * 2 * math.pi
        sphere(f"rivet{k}", 0.012, (math.cos(a) * (R - 0.04), -L / 2 - 0.005, R + 0.35 + math.sin(a) * (R - 0.04)), "DarkMetal", seg=8)


@model
def build_MahjongTable():
    """Square walnut table with green felt, and an unfinished mahjong game on it."""
    import random
    random.seed(37)
    box("top", (0.95, 0.95, 0.04), (0, 0, 0.72), "L5_Walnut", bevel=0.01)
    box("felt", (0.82, 0.82, 0.004), (0, 0, 0.76), "PaintGreen")
    for x in (-0.4, 0.4):
        for y in (-0.4, 0.4):
            cyl(f"leg{x}{y}", 0.035, 0.72, (x, y, 0), "L5_Walnut", 16, r2=0.025)
    box("apron", (0.86, 0.86, 0.1), (0, 0, 0.62), "L5_Walnut")
    # walls of tiles
    for side in range(4):
        ang = side * math.pi / 2
        for i in range(14):
            for layer in range(2):
                off = -0.26 + i * 0.04
                x, y = math.cos(ang) * 0.2 - math.sin(ang) * off, math.sin(ang) * 0.2 + math.cos(ang) * off
                if random.random() < 0.15:
                    continue
                box(f"w{side}{i}{layer}", (0.022, 0.038, 0.028), (x, y, 0.764 + layer * 0.028), "PlasticWhite", rot=(0, 0, ang), bevel=0.003)
    # racks and discards
    for side in range(4):
        ang = side * math.pi / 2
        for i in range(13):
            off = -0.26 + i * 0.042
            x, y = math.cos(ang) * 0.37 - math.sin(ang) * off, math.sin(ang) * 0.37 + math.cos(ang) * off
            box(f"h{side}{i}", (0.022, 0.038, 0.028), (x, y, 0.764), "PlasticWhite", rot=(math.radians(70), 0, ang), bevel=0.003)
    for i in range(22):
        x, y = random.uniform(-0.12, 0.12), random.uniform(-0.12, 0.12)
        box(f"d{i}", (0.038, 0.028, 0.022), (x, y, 0.764), "PlasticWhite", rot=(0, 0, random.uniform(0, 6.28)), bevel=0.003)


@model
def build_Elevator_ArtDeco():
    """Brass art-deco elevator frame with sunburst transom and floor-indicator dial. Wall at Y=0, opening 1.2 x 2.3."""
    w, h = 1.2, 2.3
    for sx in (-1, 1):
        box(f"pil{sx}", (0.16, 0.1, h + 0.5), (sx * (w / 2 + 0.08), -0.05, 0), "Brass", bevel=0.01)
        for k in range(3):
            box(f"step{sx}{k}", (0.2 - k * 0.04, 0.12 + k * 0.01, 0.06), (sx * (w / 2 + 0.08), -0.06, h + 0.5 + k * 0.06), "Brass", bevel=0.005)
    box("lintel", (w + 0.32, 0.1, 0.5), (0, -0.05, h), "L5_Walnut", bevel=0.008)
    for i in range(9):
        a = math.radians(-60 + i * 15)
        box(f"ray{i}", (0.012, 0.012, 0.36), (math.sin(a) * 0.05, -0.105, h + 0.03), "Brass", rot=(0, a, 0))
    cyl("dial", 0.14, 0.02, (0, -0.11, h + 0.3), "Brass", 40, rot=(math.pi / 2, 0, 0))
    cyl("dialface", 0.12, 0.004, (0, -0.131, h + 0.3), "PaintCream", 40, rot=(math.pi / 2, 0, 0))
    box("needle", (0.006, 0.003, 0.1), (0.03, -0.136, h + 0.33), "PaintBlack", rot=(0, math.radians(-30), 0))
    box("cab_back", (w, 0.02, h), (0, 0.9, 0), "L5_WoodPanel")
    box("cab_floor", (w, 0.9, 0.02), (0, 0.45, 0), "L5_HotelCarpet")
    box("cab_l", (0.02, 0.9, h), (-w / 2, 0.45, 0), "L5_WoodPanel")
    box("cab_r", (0.02, 0.9, h), (w / 2, 0.45, 0), "L5_WoodPanel")
    box("cab_top", (w, 0.9, 0.02), (0, 0.45, h), "PaintCream")
    box("cab_light", (0.5, 0.3, 0.01), (0, 0.45, h - 0.01), "EmissiveWarm")
    box("btnpanel", (0.14, 0.02, 0.5), (w / 2 - 0.12, 0.88, 1.0), "Brass", bevel=0.004)
    for r in range(6):
        for c in range(2):
            cyl(f"b{r}{c}", 0.013, 0.01, (w / 2 - 0.15 + c * 0.05, 0.87, 1.1 + r * 0.06), "PaintCream", 16, rot=(math.pi / 2, 0, 0))


@model
def build_Elevator_Gate():
    """Brass scissor gate (1.2 x 2.25). Pivot at its left edge: scaling X folds it toward the pivot."""
    w, h = 1.2, 2.25
    for i in range(12):
        box(f"bar{i}", (0.012, 0.012, h), (0.05 + i * (w - 0.1) / 11, 0, 0.02), "Brass")
    for i in range(10):
        for sgn in (-1, 1):
            box(f"x{i}{sgn}", (0.01, 0.01, 0.16), (0.1 + i * 0.11, 0, 1.0), "Brass", rot=(0, math.radians(35 * sgn), 0))
    box("top", (w, 0.02, 0.03), (w / 2, 0, h), "Brass")
    box("bottom", (w, 0.02, 0.03), (w / 2, 0, 0.02), "Brass")


@model
def build_Bookshelf_Hotel():
    """Tall walnut bookshelf filled with old books (procedural)."""
    import random
    random.seed(5)
    W, D, H = 1.2, 0.36, 2.2
    box("back", (W, 0.02, H), (0, D / 2 - 0.01, 0), "L5_Walnut")
    for sx in (-1, 1):
        box(f"side{sx}", (0.03, D, H), (sx * (W / 2 - 0.015), 0, 0), "L5_Walnut", bevel=0.004)
    box("crown", (W + 0.08, D + 0.04, 0.06), (0, -0.01, H), "L5_Walnut", bevel=0.01)
    colors = ["PaintRed", "PaintGreen", "PaintBlack", "L5_Velvet", "PlasticBeige", "PaintCream"]
    for s in range(6):
        z = 0.08 + s * 0.36
        box(f"shelf{s}", (W - 0.06, D - 0.02, 0.025), (0, 0, z), "L5_Walnut")
        if s == 5:
            continue
        x = -W / 2 + 0.05
        while x < W / 2 - 0.08:
            bw = random.uniform(0.025, 0.055)
            bh = random.uniform(0.2, 0.3)
            tilt = random.random() < 0.06
            box(f"book{s}_{x:.2f}", (bw, random.uniform(0.18, 0.24), bh), (x + bw / 2, 0.02, z + 0.025),
                random.choice(colors), bevel=0.003, rot=(0, math.radians(12) if tilt else 0, 0))
            x += bw + 0.003
            if random.random() < 0.04:
                x += 0.1


if __name__ == "__main__":
    only = [a for a in sys.argv if a.startswith("only=")]
    if only:
        for d in range(10):
            digit_model(str(d))()
        sys.exit(0)
    for fn in [build_Hotel_DoorLeaf, build_Hotel_DoorFrame, build_Wainscot, build_CrownMolding, build_Column_ArtDeco,
               build_Sconce_ArtDeco, build_Gramophone, build_ReceptionDesk, build_Boiler, build_MahjongTable,
               build_Elevator_ArtDeco, build_Elevator_Gate, build_Bookshelf_Hotel] + [digit_model(str(d)) for d in range(10)]:
        fn()
