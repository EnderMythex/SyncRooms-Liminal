"""Level 4 - Abandoned Office custom models + shared items.
  blender -b --factory-startup -P models_office.py
"""
import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import *  # noqa

FONT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "IBMPlexMono-SemiBold.ttf")


@model
def build_WaterCooler():
    # Cabinet
    box("cab", (0.32, 0.32, 0.95), (0, 0, 0), "PlasticBeige", bevel=0.012)
    box("recess", (0.24, 0.02, 0.2), (0, -0.155, 0.62), "PlasticBlack", bevel=0.005)
    box("tray", (0.22, 0.1, 0.02), (0, -0.19, 0.6), "Steel", bevel=0.004)
    for x, c in ((-0.05, "PaintRed"), (0.05, "PlasticBlue")):
        box("tap" + c, (0.035, 0.05, 0.035), (x, -0.18, 0.76), c, bevel=0.006)
    box("kick", (0.3, 0.3, 0.05), (0, 0.005, 0), "PlasticBlack")
    cyl("neck_ring", 0.075, 0.03, (0, 0, 0.95), "PlasticWhite", 32)
    # 5 gallon jug (Almond Water) upside down
    prof = [(0.0, 0.98), (0.03, 0.98), (0.03, 1.04), (0.085, 1.07), (0.135, 1.1), (0.14, 1.16), (0.132, 1.2),
            (0.14, 1.24), (0.14, 1.34), (0.132, 1.38), (0.14, 1.42), (0.135, 1.44), (0.1, 1.47), (0.0, 1.475)]
    lathe("jug", prof, "PlasticBlue", steps=40)
    liquid = [(0.0, 1.0), (0.026, 1.0), (0.026, 1.04), (0.08, 1.07), (0.13, 1.1), (0.13, 1.33), (0.0, 1.33)]
    lathe("liquid", liquid, "Liquid", steps=32)


@model
def build_VendingMachine():
    W, D, H = 0.92, 0.82, 1.86
    box("body", (W, D, H), (0, 0, 0), "PaintRed", bevel=0.015)
    box("front_frame", (W - 0.04, 0.03, H - 0.1), (0, -D / 2 - 0.005, 0.05), "PaintBlack", bevel=0.006)
    # window + shelves with bottles
    box("glass", (0.6, 0.012, 1.25), (-0.1, -D / 2 - 0.022, 0.5), "Glass")
    box("back", (0.6, 0.02, 1.25), (-0.1, -D / 2 + 0.25, 0.5), "EmissivePanel")
    for row in range(5):
        z = 0.52 + row * 0.24
        box(f"shelf{row}", (0.6, 0.25, 0.012), (-0.1, -D / 2 + 0.13, z), "Steel")
        for i in range(6):
            x = -0.36 + i * 0.1 + 0.02
            prof = [(0, 0), (0.028, 0), (0.03, 0.01), (0.03, 0.13), (0.016, 0.17), (0.012, 0.19), (0, 0.19)]
            lathe(f"b{row}_{i}", prof, "PlasticBlue", loc=(x, -D / 2 + 0.1, z + 0.012), steps=16)
            lathe(f"cap{row}_{i}", [(0, 0.19), (0.013, 0.19), (0.013, 0.205), (0, 0.205)], "PlasticWhite", loc=(x, -D / 2 + 0.1, z + 0.012), steps=12)
    # control panel
    box("panel", (0.2, 0.02, 0.55), (0.33, -D / 2 - 0.02, 1.1), "PlasticBlack", bevel=0.004)
    for r in range(4):
        for c in range(3):
            box(f"key{r}{c}", (0.035, 0.012, 0.03), (0.28 + c * 0.05, -D / 2 - 0.034, 1.45 - r * 0.05), "Steel", bevel=0.003)
    box("display", (0.14, 0.01, 0.05), (0.33, -D / 2 - 0.032, 1.55), "EmissiveExit")
    box("coin", (0.03, 0.012, 0.06), (0.33, -D / 2 - 0.034, 1.25), "Steel")
    box("flap", (0.6, 0.03, 0.18), (-0.1, -D / 2 - 0.01, 0.18), "PlasticBlack", bevel=0.01)
    t = text("logo", "ALMOND WATER", 0.07, "PlasticWhite", loc=(-0.1, -D / 2 - 0.03, 1.9 - 0.08), font=FONT)


@model
def build_Troffer():
    """Recessed 60x120 fluorescent troffer, flush with ceiling. Pivot at ceiling plane (top)."""
    W, L = 0.6, 1.2
    box("housing", (W, L, 0.08), (0, 0, -0.08), "PlasticWhite", bevel=0.004)
    box("diffuser", (W - 0.06, L - 0.06, 0.005), (0, 0, -0.084), "EmissivePanel")
    for x in (-0.15, 0, 0.15):
        box("louver%.2f" % x, (0.012, L - 0.06, 0.02), (x, 0, -0.1), "Steel")
    box("trim", (W + 0.02, L + 0.02, 0.006), (0, 0, -0.086), "PlasticWhite")
    # hollow: cut diffuser window from trim
    cut = box("cut", (W - 0.06, L - 0.06, 0.05), (0, 0, -0.11), "PlasticWhite")
    boolean(bpy.data.objects["trim"], cut)


@model
def build_Office_DoorFrame():
    """Metal door frame for a 0.9 x 2.1 opening in a 0.14 wall. Opening centered on X=0, bottom at 0."""
    w, h, t, d = 0.9, 2.1, 0.05, 0.16
    box("l", (t, d, h + t), (-w / 2 - t / 2, 0, 0), "L4_Metal", bevel=0.004)
    box("r", (t, d, h + t), (w / 2 + t / 2, 0, 0), "L4_Metal", bevel=0.004)
    box("top", (w + 2 * t, d, t), (0, 0, h), "L4_Metal", bevel=0.004)


@model
def build_Office_DoorLeaf():
    """Door leaf, pivot at the hinge (left edge seen from the front), bottom."""
    w, h, t = 0.88, 2.08, 0.045
    box("leaf", (w, t, h), (w / 2, 0, 0.01), "L4_Door", bevel=0.003)
    box("kick", (w - 0.02, t + 0.004, 0.2), (w / 2, 0, 0.02), "Steel")
    # lever handles both sides
    for s in (-1, 1):
        cyl("rose%d" % s, 0.03, 0.012, (w - 0.08, s * (t / 2 + 0.006), 1.0), "Steel", 24, rot=(math.pi / 2, 0, 0))
        box("lever%d" % s, (0.13, 0.02, 0.018), (w - 0.13, s * (t / 2 + 0.045), 0.991), "Steel", bevel=0.006)
        cyl("stem%d" % s, 0.01, 0.045, (w - 0.08, s * (t / 2), 1.0), "Steel", 16, rot=(s * math.pi / 2, 0, 0))
    # small wired-glass window
    box("win", (0.2, t + 0.004, 0.55), (w / 2, 0, 1.35), "GlassFrosted")
    for dz in (0, 0.55):
        box("wtrim%d" % int(dz * 10), (0.24, t + 0.012, 0.02), (w / 2, 0, 1.34 + dz), "Steel")


@model
def build_CubicleWall():
    """1.6m x 1.35m fabric partition with aluminium trim. Centered on X, thickness along Y."""
    w, h, t = 1.6, 1.35, 0.065
    box("panel", (w - 0.04, t - 0.01, h - 0.08), (0, 0, 0.04), "L4_Carpet", bevel=0.004)
    box("cap", (w, t, 0.03), (0, 0, h - 0.03), "Steel", bevel=0.008)
    for x in (-w / 2 + 0.02, w / 2 - 0.02):
        box("post%.1f" % x, (0.04, t + 0.004, h), (x, 0, 0), "Steel", bevel=0.006)
    box("base", (w, t + 0.01, 0.05), (0, 0, 0), "PlasticBlack")


@model
def build_ExitSign():
    """Ceiling/wall mounted illuminated EXIT sign. Pivot at top (mounting)."""
    box("housing", (0.36, 0.06, 0.16), (0, 0, -0.18), "PlasticWhite", bevel=0.006)
    box("face", (0.33, 0.004, 0.13), (0, -0.031, -0.165), "EmissiveExit")
    text("t", "SORTIE", 0.07, "PlasticWhite", loc=(0, -0.034, -0.1), extrude=0.001, font=FONT)
    box("mount", (0.05, 0.04, 0.02), (0, 0, -0.02), "PlasticWhite")


@model
def build_CeilingVent():
    """60x60 return-air grille, flush with ceiling. Pivot at ceiling plane."""
    box("frame", (0.6, 0.6, 0.02), (0, 0, -0.02), "PlasticWhite", bevel=0.004)
    box("dark", (0.54, 0.54, 0.01), (0, 0, 0.0), "PlasticBlack")
    for i in range(12):
        box("slat%d" % i, (0.54, 0.008, 0.03), (0, -0.25 + i * 0.045, -0.035), "PlasticWhite", rot=(math.radians(35), 0, 0))
    cut = box("cut", (0.54, 0.54, 0.1), (0, 0, -0.06), "PlasticWhite")
    boolean(bpy.data.objects["frame"], cut)


@model
def build_OfficeWindow():
    """Blacked-out office window 1.4 x 1.2, set in a wall 0.14 thick. Pivot bottom center of opening."""
    w, h = 1.4, 1.2
    box("sill", (w + 0.1, 0.22, 0.03), (0, -0.03, -0.03), "PaintOffWhite", bevel=0.006)
    for x in (-w / 2, w / 2):
        box("jamb%.1f" % x, (0.05, 0.12, h), (x, 0, 0), "L4_Metal")
    box("head", (w + 0.05, 0.12, 0.05), (0, 0, h - 0.05), "L4_Metal")
    box("mull", (0.04, 0.1, h), (0, 0, 0), "L4_Metal")
    box("glass", (w, 0.01, h), (0, -0.02, 0), "Glass")
    box("black", (w, 0.01, h), (0, 0.03, 0), "BlackOut")
    # taped paper on the blackout (someone covered it)
    box("tape1", (0.15, 0.004, 0.03), (-0.45, 0.022, h - 0.12), "PlasticBeige", rot=(0, math.radians(20), 0))
    box("tape2", (0.15, 0.004, 0.03), (0.4, 0.022, 0.1), "PlasticBeige", rot=(0, math.radians(-15), 0))


@model
def build_Elevator_Modern():
    """Elevator doors + frame + call panel, flush with a wall at Y=0 (wall behind, +Y). Opening 1.1 x 2.2."""
    w, h = 1.1, 2.2
    box("fl", (0.12, 0.08, h + 0.12), (-w / 2 - 0.06, -0.04, 0), "ElevatorSteel", bevel=0.01)
    box("fr", (0.12, 0.08, h + 0.12), (w / 2 + 0.06, -0.04, 0), "ElevatorSteel", bevel=0.01)
    box("ft", (w + 0.24, 0.08, 0.12), (0, -0.04, h), "ElevatorSteel", bevel=0.01)
    box("dl", (w / 2 - 0.005, 0.03, h), (-w / 4, 0.02, 0), "ElevatorSteel", bevel=0.003)
    box("dr", (w / 2 - 0.005, 0.03, h), (w / 4, 0.02, 0), "ElevatorSteel", bevel=0.003)
    box("gap", (0.01, 0.03, h), (0, 0.03, 0), "PlasticBlack")
    box("sill", (w + 0.1, 0.12, 0.02), (0, -0.02, 0), "Steel")
    box("panel", (0.1, 0.02, 0.26), (w / 2 + 0.3, -0.01, 1.0), "ElevatorSteel", bevel=0.004)
    cyl("btn_up", 0.018, 0.012, (w / 2 + 0.3, -0.02, 1.18), "EmissiveWarm", 20, rot=(math.pi / 2, 0, 0))
    cyl("btn_dn", 0.018, 0.012, (w / 2 + 0.3, -0.02, 1.08), "Steel", 20, rot=(math.pi / 2, 0, 0))
    box("indicator", (0.3, 0.02, 0.07), (0, -0.09, h + 0.03), "PlasticBlack", bevel=0.004)
    text("ind", "4", 0.05, "EmissiveRed", loc=(0, -0.101, h + 0.066), extrude=0.001, font=FONT)


@model
def build_OutletSwitch():
    """Wall outlet plate (small). Pivot at wall surface."""
    box("plate", (0.08, 0.008, 0.12), (0, -0.004, 0), "PlasticWhite", bevel=0.002)
    for z in (0.03, 0.08):
        box("hole%.2f" % z, (0.006, 0.004, 0.015), (-0.012, -0.009, z), "PlasticBlack")
        box("hole2%.2f" % z, (0.006, 0.004, 0.015), (0.012, -0.009, z), "PlasticBlack")


@model
def build_OfficeDesk():
    """Simple melamine office desk with modesty panel (complements the Poly Haven metal desk)."""
    box("top", (1.6, 0.8, 0.03), (0, 0, 0.72), "PlasticBeige", bevel=0.004)
    for x in (-0.78, 0.78):
        box("side%.1f" % x, (0.03, 0.76, 0.72), (x, 0, 0), "PlasticBeige", bevel=0.003)
    box("modesty", (1.53, 0.02, 0.45), (0, 0.33, 0.27), "PlasticBeige")
    box("drawers", (0.42, 0.7, 0.6), (0.55, 0, 0.1), "L4_Metal", bevel=0.004)
    for i in range(3):
        box("dfront%d" % i, (0.4, 0.01, 0.19), (0.55, -0.355, 0.11 + i * 0.2), "L4_Metal", bevel=0.003)
        box("dhandle%d" % i, (0.12, 0.015, 0.012), (0.55, -0.365, 0.26 + i * 0.2), "Steel", bevel=0.003)


# ================================================================== ITEMS
@model
def build_Item_AlmondWater():
    """500ml PET bottle of Almond Water with a label. Pivot bottom center."""
    prof = [(0, 0), (0.028, 0), (0.032, 0.006), (0.033, 0.02), (0.031, 0.03), (0.033, 0.04), (0.033, 0.15),
            (0.03, 0.17), (0.018, 0.2), (0.0135, 0.215), (0.0135, 0.225), (0, 0.225)]
    lathe("bottle", prof, "PlasticBlue", steps=32)
    lathe("water", [(0, 0.004), (0.03, 0.004), (0.031, 0.02), (0.031, 0.15), (0.028, 0.165), (0, 0.165)], "Liquid", steps=24)
    lathe("cap", [(0, 0.222), (0.0145, 0.222), (0.0145, 0.24), (0.012, 0.243), (0, 0.243)], "PlasticWhite", steps=20)
    lab = lathe("label", [(0.0336, 0.06), (0.0336, 0.13)], "AlmondLabel", steps=32)
    cyl_uv(lab, 0.06, 0.13)
    return ["label"]  # keep cylindrical UVs on the label


def make_label():
    """Almond Water label texture (wraps around the bottle: 0.211m x 0.07m)."""
    brown, cream, gold = (0.23, 0.13, 0.07), (0.95, 0.91, 0.8), (0.72, 0.52, 0.22)
    W, H = 0.211, 0.07
    els = []
    for k in range(2):  # printed twice around the bottle
        x0 = W * (0.25 + 0.5 * k)
        els += [
            {"type": "rect", "x": x0, "y": H * 0.5, "w": W * 0.46, "h": H * 0.78, "color": (1, 0.98, 0.93)},
            {"type": "ellipse", "x": x0 - 0.034, "y": H * 0.52, "w": 0.012, "h": 0.02, "color": gold, "rot": 0.5},
            {"type": "text", "text": "ALMOND", "size": 0.013, "x": x0 + 0.008, "y": H * 0.62, "color": brown, "font": FONT},
            {"type": "text", "text": "WATER", "size": 0.013, "x": x0 + 0.008, "y": H * 0.44, "color": brown, "font": FONT},
            {"type": "text", "text": "Natural · 500 mL", "size": 0.0048, "x": x0, "y": H * 0.24, "color": brown, "font": FONT},
            {"type": "rect", "x": x0, "y": H * 0.9, "w": W * 0.46, "h": 0.002, "color": gold},
            {"type": "rect", "x": x0, "y": H * 0.1, "w": W * 0.46, "h": 0.002, "color": gold},
        ]
    render_graphic(os.path.join(OUT, "AlmondLabel_Albedo.png"), W, H, els, bg=cream)


@model
def build_Item_Battery():
    lathe("body", [(0, 0), (0.016, 0), (0.017, 0.002), (0.017, 0.058), (0.016, 0.06), (0, 0.06)], "PaintBlack", steps=24)
    lathe("top", [(0, 0.06), (0.012, 0.06), (0.012, 0.062), (0.005, 0.062), (0.005, 0.066), (0, 0.066)], "Brass", steps=20)
    lathe("band", [(0.0172, 0.035), (0.0172, 0.057)], "Brass", steps=24)


@model
def build_Item_Keycard():
    box("card", (0.054, 0.086, 0.0012), (0, 0, 0), "PlasticWhite", bevel=0.0006, segments=1)
    box("stripe", (0.054, 0.012, 0.0004), (0, 0.025, 0.0012), "PlasticBlack")
    box("photo", (0.018, 0.022, 0.0003), (-0.012, -0.015, 0.0012), "PlasticBlue")


@model
def build_Item_HotelKey():
    """Brass skeleton key + oval tag."""
    torus("bow", 0.012, 0.003, (0, 0, 0.004), "Brass", rot=(0, 0, 0), seg=24, mseg=8)
    cyl("shaft", 0.0028, 0.055, (0.012, 0, 0.004), "Brass", 12, rot=(0, math.pi / 2, 0))
    box("bit1", (0.006, 0.003, 0.01), (0.062, 0, -0.002), "Brass")
    box("bit2", (0.004, 0.003, 0.007), (0.054, 0, -0.001), "Brass")
    ring = torus("ring", 0.007, 0.0012, (-0.018, 0, 0.004), "Steel", seg=16, mseg=6)
    tag = cyl("tag", 0.022, 0.004, (-0.045, 0, 0.002), "L5_Walnut", 24)
    tag.scale = (1.4, 0.8, 1)
    text("num", "237", 0.014, "Brass", loc=(-0.045, 0, 0.0065), rot=(0, 0, 0), extrude=0.0006, font=FONT)


@model
def build_Item_Note():
    box("paper", (0.148, 0.21, 0.0008), (0, 0, 0), "Com_Paper")
    box("fold", (0.148, 0.002, 0.0012), (0, 0.035, 0.0005), "Com_Paper")


if __name__ == "__main__":
    make_label()
    for fn in [build_WaterCooler, build_VendingMachine, build_Troffer, build_Office_DoorFrame, build_Office_DoorLeaf,
               build_CubicleWall, build_ExitSign, build_CeilingVent, build_OfficeWindow, build_Elevator_Modern,
               build_OutletSwitch, build_OfficeDesk, build_Item_AlmondWater, build_Item_Battery, build_Item_Keycard,
               build_Item_HotelKey, build_Item_Note]:
        fn()
