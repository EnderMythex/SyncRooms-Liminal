"""Level 94 - Motion: 1930s town + Level 37 - Sublimity pool modules.
  blender -b --factory-startup -P models_town.py
"""
import math
import os
import random
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import *  # noqa

PLEX = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "IBMPlexMono-SemiBold.ttf")
SERIF = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "SpecialElite-Regular.ttf")


# ================================================================== HOUSES
def window(name, x, y, z, w, h, facing, keep, curtains=True, shutters=None):
    """Double-hung window centered at (x, z) on a wall whose outer face is at `y` (facing -Y if facing=-1)."""
    s = facing
    parts = []
    parts.append(box(name + "_sill", (w + 0.16, 0.12, 0.05), (x, y + s * 0.05, z - 0.05), "PaintOffWhite", bevel=0.008))
    for sx in (-1, 1):
        parts.append(box(name + f"_jamb{sx}", (0.07, 0.08, h), (x + sx * (w / 2 + 0.035), y + s * 0.02, z), "PaintOffWhite", bevel=0.005))
    parts.append(box(name + "_head", (w + 0.2, 0.09, 0.09), (x, y + s * 0.025, z + h), "PaintOffWhite", bevel=0.008))
    parts.append(box(name + "_meet", (w, 0.05, 0.045), (x, y - s * 0.02, z + h / 2), "PaintOffWhite"))
    for i in (1, 2):
        parts.append(box(name + f"_mun{i}", (0.025, 0.04, h / 2), (x - w / 2 + i * w / 3, y - s * 0.02, z + h / 2), "PaintOffWhite"))
    parts.append(box(name + "_glass", (w, 0.01, h), (x, y - s * 0.03, z), "Glass"))
    if curtains:
        for sx in (-1, 1):
            parts.append(box(name + f"_cur{sx}", (w * 0.3, 0.02, h * 0.95), (x + sx * w * 0.35, y - s * 0.1, z + 0.02), "PaintCream"))
    if shutters:
        for sx in (-1, 1):
            sh = box(name + f"_sh{sx}", (w / 2, 0.04, h + 0.05), (x + sx * (w * 0.75 + 0.12), y + s * 0.03, z - 0.02), shutters, bevel=0.006)
            for k in range(6):
                parts.append(box(name + f"_lv{sx}{k}", (w / 2 - 0.08, 0.012, 0.02), (x + sx * (w * 0.75 + 0.12), y + s * 0.055, z + 0.12 + k * h / 6.5), shutters))
    return parts


def roof_slab(name, length, width, thickness, loc, rot, material):
    ob = box(name, (length, width, thickness), (0, 0, 0), material)
    local_planar_uv(ob)
    ob.location = loc
    ob.rotation_euler = rot
    return ob


def house(name, siding, roof, door_color, shutter, w=7.0, d=9.0, wall_h=2.9, pitch=40, chimney=True, seed=0):
    random.seed(seed)
    keep = []
    base = 0.55
    box("found", (w + 0.1, d + 0.1, base), (0, 0, 0), "L94_Stone" if siding != "L94_Brick" else "L94_Brick")
    t = 0.18
    walls = [
        box("wf", (w, t, wall_h), (0, -d / 2 + t / 2, base), siding),
        box("wb", (w, t, wall_h), (0, d / 2 - t / 2, base), siding),
        box("wl", (t, d, wall_h), (-w / 2 + t / 2, 0, base), siding),
        box("wr", (t, d, wall_h), (w / 2 - t / 2, 0, base), siding),
    ]
    # gables (front and back), ridge along Y
    rh = math.tan(math.radians(pitch)) * w / 2
    for y, nm in ((-d / 2 + t, "gf"), (d / 2, "gb")):
        g = polygon_prism(nm, [(-w / 2, 0), (w / 2, 0), (0, rh)], t, siding, loc=(0, y, base + wall_h), rot=(math.pi / 2, 0, 0))
    # window & door cutouts on front
    front_y = -d / 2
    door_x = -w * 0.22
    cut = box("door_cut", (1.0, 0.6, 2.15), (door_x, front_y, base), siding)
    boolean(walls[0], cut)
    cutter = box("win_cut", (1.3, 0.6, 1.4), (w * 0.2, front_y, base + 0.85), siding)
    boolean(walls[0], cutter)
    window("fw1", w * 0.2, front_y, base + 0.85, 1.3, 1.4, -1, keep, shutters=shutter)
    # gable window
    cutter = box("gwin_cut", (0.8, 0.6, 0.9), (0, front_y + 0.1, base + wall_h + 0.25), siding)
    g = bpy.data.objects["gf"]
    boolean(g, cutter)
    window("gw", 0, front_y + t - 0.02, base + wall_h + 0.25, 0.8, 0.9, -1, keep, curtains=False)
    # side windows
    for sx, nm in ((-1, "wl"), (1, "wr")):
        for k, yy in enumerate((-d * 0.22, d * 0.2)):
            c = box(f"sc{sx}{k}", (0.6, 1.1, 1.3), (sx * w / 2, yy, base + 0.9), siding)
            boolean(bpy.data.objects[nm], c)
            wob = window(f"sw{sx}{k}", sx * w / 2, 0, base + 0.9, 1.1, 1.3, -1, keep, shutters=None)
            for p in wob:  # rotate window parts around the wall axis
                p.location = (sx * w / 2 + (p.location.y) * -sx, yy + (p.location.x - sx * w / 2) * sx, p.location.z)
                p.rotation_euler = (0, 0, sx * math.pi / 2)
    # back windows
    c = box("bc", (1.2, 0.6, 1.3), (w * 0.15, d / 2, base + 0.9), siding)
    boolean(walls[1], c)
    window("bw", w * 0.15, d / 2, base + 0.9, 1.2, 1.3, 1, keep)

    for sx in (-1, 1):
        box(f"dtrim{sx}", (0.1, 0.06, 2.2), (door_x + sx * 0.55, front_y - 0.01, base), "PaintOffWhite", bevel=0.006)
    box("dtrimh", (1.2, 0.08, 0.14), (door_x, front_y - 0.02, base + 2.18), "PaintOffWhite", bevel=0.008)

    # porch + steps + little roof
    box("porch", (2.2, 1.6, base), (door_x, front_y - 0.8, 0), "L94_Stone", bevel=0.01)
    for k in range(3):
        box(f"step{k}", (1.4, 0.32, base - (k + 1) * 0.18), (door_x, front_y - 1.6 - 0.16 - 0.32 * k, 0), "L94_Stone", bevel=0.008)
    for sx in (-1, 1):
        cyl(f"post{sx}", 0.07, 2.35, (door_x + sx * 0.95, front_y - 1.45, base), "PaintOffWhite", 16)
    prh = 0.55
    for sx in (-1, 1):
        roof_slab(f"proof{sx}", 1.25, 1.9, 0.08, (door_x + sx * 0.55, front_y - 0.75, base + 2.45 + prh / 2),
                  (0, sx * math.radians(35), 0), roof)
    # main roof slabs (ridge along Y), each slab runs from the ridge down past the eave
    over = 0.45
    pr = math.radians(pitch)
    slope_len = (w / 2) / math.cos(pr) + over
    z_r = base + wall_h + rh
    for sx in (-1, 1):
        ob = roof_slab(f"roof{sx}", slope_len, d + 0.7, 0.14, (0, 0, 0), (0, 0, 0), roof)
        ob.rotation_euler = (0, sx * pr, 0)
        ob.location = (sx * (slope_len / 2) * math.cos(pr), 0, z_r - (slope_len / 2) * math.sin(pr) + 0.02)
        keep.append(ob.name)
        for y in (-d / 2 - 0.36, d / 2 + 0.36):
            bb = box(f"barge{y:.1f}{sx}", (slope_len, 0.05, 0.22), (0, 0, 0), "PaintOffWhite")
            bb.rotation_euler = (0, sx * pr, 0)
            bb.location = (sx * (slope_len / 2) * math.cos(pr), y, z_r - (slope_len / 2) * math.sin(pr) - 0.14)
        box(f"gutter{sx}", (0.12, d + 0.7, 0.12), (sx * slope_len * math.cos(pr), 0, z_r - slope_len * math.sin(pr) - 0.12), "PaintOffWhite")
    box("ridge", (0.2, d + 0.72, 0.12), (0, 0, z_r + 0.02), "PaintOffWhite", bevel=0.03)
    if chimney:
        box("chimney", (0.7, 0.9, rh + 1.2), (w * 0.25, d * 0.2, base + wall_h), "L94_Brick")
        box("chimcap", (0.82, 1.02, 0.1), (w * 0.25, d * 0.2, base + wall_h + rh + 1.2), "L94_Stone", bevel=0.01)
    # corner boards
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(f"corner{sx}{sy}", (0.12, 0.12, wall_h), (sx * (w / 2 - 0.04), sy * (d / 2 - 0.04), base), "PaintOffWhite")
    box("frieze", (w + 0.1, d + 0.1, 0.18), (0, 0, base + wall_h - 0.18), "PaintOffWhite")
    cut = box("frieze_cut", (w - 0.1, d - 0.1, 0.4), (0, 0, base + wall_h - 0.3), "PaintOffWhite")
    boolean(bpy.data.objects["frieze"], cut)
    # hollow the frieze & block interior visibility: interior floor/ceiling dark box
    # enterable interior: wooden floor + ceiling (the door leaf is a separate model: House_Door)
    box("floor_in", (w - 0.36, d - 0.36, 0.04), (0, 0, base), "L94_Wood")
    box("ceiling_in", (w - 0.36, d - 0.36, 0.05), (0, 0, base + wall_h - 0.05), "PaintOffWhite")
    return keep


def make_house(name, siding, roof, door, shutter, seed, **kw):
    def build():
        return house(name, siding, roof, door, shutter, seed=seed, **kw)
    build.__name__ = "build_" + name
    return model(build)


@model
def build_House_Door():
    """Front door leaf, pivot at the hinge (bottom), extends +X."""
    box("door", (0.92, 0.05, 2.08), (0.46, 0, 0), "PaintRed", bevel=0.006)
    for k in range(3):
        for sx in (-1, 1):
            box(f"dp{k}{sx}", (0.32, 0.015, 0.45), (0.46 + sx * 0.19, -0.03, 0.2 + k * 0.62), "PaintRed", bevel=0.01)
            box(f"dpb{k}{sx}", (0.32, 0.015, 0.45), (0.46 + sx * 0.19, 0.03, 0.2 + k * 0.62), "PaintRed", bevel=0.01)
    box("dwin", (0.5, 0.06, 0.3), (0.46, 0, 1.6), "Glass")
    sphere("knob", 0.03, (0.81, -0.05, 1.0), "Brass")
    sphere("knob2", 0.03, (0.81, 0.05, 1.0), "Brass")


# ================================================================== TOWN PROPS
@model
def build_WaterTower():
    H = 13.0
    R = 3.4
    legs = []
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        x, y = math.cos(a) * 3.2, math.sin(a) * 3.2
        cyl(f"leg{k}", 0.16, H + 0.4, (x, y, 0), "L94_PaintedMetal", 12)
        cyl(f"foot{k}", 0.35, 0.5, (x, y, 0), "L94_Stone", 16)
    for lvl in (3.2, 6.8, 10.2):
        for k in range(4):
            a1 = math.pi / 4 + k * math.pi / 2
            a2 = a1 + math.pi / 2
            r = 3.2
            x1, y1, x2, y2 = math.cos(a1) * r, math.sin(a1) * r, math.cos(a2) * r, math.sin(a2) * r
            mx, my = (x1 + x2) / 2, (y1 + y2) / 2
            L = math.hypot(x2 - x1, y2 - y1)
            ang = math.atan2(y2 - y1, x2 - x1)
            box(f"hbrace{lvl}{k}", (L, 0.1, 0.1), (mx, my, lvl), "L94_PaintedMetal", rot=(0, 0, ang))
            for sgn in (-1, 1):
                b = box(f"xbrace{lvl}{k}{sgn}", (math.hypot(L, 3.4), 0.05, 0.05), (mx, my, lvl - 1.7), "L94_PaintedMetal", rot=(0, sgn * math.atan2(3.4, L), ang))
    cyl("riser", 0.45, H, (0, 0, 0), "L94_PaintedMetal", 24)
    cyl("tank", R, 5.2, (0, 0, H), "L94_PaintedMetal", 48, bevel=0.05)
    lathe("bottom", [(0, H - 1.0), (1.2, H - 0.8), (2.6, H - 0.4), (R, H)], "L94_PaintedMetal", steps=48)
    lathe("roof", [(R + 0.15, H + 5.2), (R + 0.1, H + 5.35), (1.2, H + 6.8), (0.2, H + 7.2), (0, H + 7.25)], "L94_PaintedMetal", steps=48)
    sphere("finial", 0.25, (0, 0, H + 7.35), "L94_PaintedMetal")
    torus("walk", R + 0.6, 0.06, (0, 0, H + 0.1), "L94_PaintedMetal", seg=64)
    torus("rail", R + 0.95, 0.04, (0, 0, H + 1.1), "L94_PaintedMetal", seg=64)
    lathe("deck", [(R, H + 0.05), (R + 1.0, H + 0.05), (R + 1.0, H + 0.12), (R, H + 0.12)], "L94_PaintedMetal", steps=48)
    for k in range(28):
        a = k / 28 * 2 * math.pi
        cyl(f"rp{k}", 0.025, 1.05, (math.cos(a) * (R + 0.95), math.sin(a) * (R + 0.95), H + 0.1), "L94_PaintedMetal", 8)
    for k in range(int(H / 0.35)):
        box(f"rung{k}", (0.5, 0.04, 0.04), (0, -(R + 0.8), 0.4 + k * 0.35), "L94_PaintedMetal")
    for sx in (-1, 1):
        box(f"lrail{sx}", (0.05, 0.05, H), (sx * 0.25, -(R + 0.8), 0), "L94_PaintedMetal")
    word = "MOTION"
    step = math.radians(13)
    for i, ch in enumerate(word):
        a = (i - (len(word) - 1) / 2) * step
        text(f"ch{i}", ch, 1.3, "PaintOffWhite", loc=(math.sin(a) * (R + 0.01), -math.cos(a) * (R + 0.01), H + 2.6),
             extrude=0.02, rot=(math.pi / 2, 0, a), font=SERIF)


@model
def build_Fountain():
    """Three-tier travertine fountain. Basin r=3.2. Water surfaces are added in Unity (HDRP water)."""
    lathe("basin", [(0, 0), (3.3, 0), (3.3, 0.55), (3.35, 0.62), (3.35, 0.7), (3.0, 0.7), (3.0, 0.62), (2.95, 0.15), (0, 0.15)], "L94_Stone", steps=72)
    lathe("pedestal", [(0, 0.15), (0.75, 0.15), (0.7, 0.3), (0.45, 0.5), (0.4, 1.4), (0.5, 1.5), (0, 1.5)], "L94_Stone", steps=48)
    lathe("bowl1", [(0, 1.45), (0.5, 1.45), (1.4, 1.7), (1.8, 1.95), (1.85, 2.05), (1.75, 2.05), (1.3, 1.85), (0, 1.85)], "L94_Stone", steps=64)
    lathe("stem2", [(0, 1.85), (0.3, 1.85), (0.22, 2.1), (0.2, 2.8), (0.3, 2.9), (0, 2.9)], "L94_Stone", steps=40)
    lathe("bowl2", [(0, 2.85), (0.3, 2.85), (0.8, 3.05), (1.05, 3.25), (1.08, 3.32), (0.98, 3.32), (0.7, 3.15), (0, 3.15)], "L94_Stone", steps=56)
    lathe("finial", [(0, 3.15), (0.16, 3.15), (0.1, 3.4), (0.14, 3.6), (0.2, 3.75), (0.12, 3.95), (0.05, 4.05), (0, 4.1)], "L94_Stone", steps=32)
    # falling water sheets (Almond Water is milky)
    lathe("fall1", [(1.8, 2.02), (1.85, 1.6), (1.9, 1.0), (2.0, 0.55), (2.02, 0.5), (1.97, 0.55), (1.87, 1.0), (1.82, 1.6), (1.77, 2.02)], "Liquid", steps=64)
    lathe("fall2", [(1.05, 3.3), (1.08, 2.8), (1.12, 2.3), (1.15, 2.0), (1.12, 2.0), (1.09, 2.3), (1.05, 2.8), (1.02, 3.3)], "Liquid", steps=48)
    lathe("jet", [(0.03, 4.05), (0.04, 4.3), (0.02, 4.45), (0, 4.5)], "Liquid", steps=12)
    # decorative lion masks on the pedestal (simple spouts)
    for k in range(4):
        a = k * math.pi / 2
        cyl(f"spout{k}", 0.06, 0.4, (math.cos(a) * 0.4, math.sin(a) * 0.4, 1.1), "Brass", 12, rot=(math.pi / 2, 0, a + math.pi / 2))


@model
def build_SirenPole():
    """Wooden utility pole with a 1930s multi-horn air-raid siren and loudspeakers."""
    cyl("pole", 0.16, 9.0, (0, 0, 0), "L94_Wood", 16, r2=0.12)
    box("arm", (2.2, 0.14, 0.14), (0, 0, 7.6), "L94_Wood")
    for sx in (-1, 1):
        for k in range(3):
            cyl(f"ins{sx}{k}", 0.04, 0.12, (sx * (0.4 + k * 0.3), 0, 7.74), "PlasticWhite", 12)
    cyl("siren_body", 0.35, 0.55, (0, 0, 9.0), "L94_PaintedMetal", 32)
    lathe("siren_cap", [(0.36, 9.55), (0.3, 9.7), (0.1, 9.8), (0, 9.82)], "L94_PaintedMetal", steps=32)
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        horn = lathe(f"horn{k}", [(0.07, 0), (0.1, 0.25), (0.2, 0.5), (0.32, 0.62), (0.3, 0.63), (0.18, 0.52), (0.08, 0.26), (0.05, 0)],
                     "L94_PaintedMetal", steps=24)
        horn.location = (math.cos(a) * 0.3, math.sin(a) * 0.3, 9.25)
        horn.rotation_euler = (0, math.pi / 2, a)
    for k in range(2):
        a = k * math.pi
        sp = lathe(f"spk{k}", [(0.05, 0), (0.12, 0.2), (0.22, 0.35), (0.21, 0.36), (0.1, 0.21), (0.03, 0)], "PaintBlack", steps=20)
        sp.location = (math.cos(a) * 0.2, math.sin(a) * 0.2, 6.6)
        sp.rotation_euler = (0, math.radians(100), a)
    box("junction", (0.3, 0.2, 0.4), (0, -0.18, 2.4), "L94_PaintedMetal", bevel=0.02)


@model
def build_PicketFence():
    """2m white picket fence section, centered on X."""
    for x in (-1.0, 1.0):
        box(f"post{x}", (0.1, 0.1, 1.15), (x, 0, 0), "PaintOffWhite", bevel=0.01)
        box(f"postcap{x}", (0.14, 0.14, 0.05), (x, 0, 1.15), "PaintOffWhite", bevel=0.01)
    for z in (0.25, 0.8):
        box(f"rail{z}", (2.0, 0.04, 0.09), (0, 0.05, z), "PaintOffWhite")
    n = 14
    for i in range(n):
        x = -0.93 + i * (1.86 / (n - 1))
        polygon_prism(f"picket{i}", [(-0.035, 0), (0.035, 0), (0.035, 0.95), (0, 1.02), (-0.035, 0.95)], 0.022, "PaintOffWhite",
                      loc=(x, 0.0, 0.05), rot=(math.pi / 2, 0, 0))


@model
def build_Mailbox():
    box("post", (0.09, 0.09, 1.05), (0, 0, 0), "L94_Wood")
    box("box", (0.24, 0.5, 0.2), (0, 0, 1.05), "L94_PaintedMetal", bevel=0.02)
    cyl("top", 0.12, 0.5, (0, 0.25, 1.25), "L94_PaintedMetal", 24, rot=(math.pi / 2, 0, 0))
    box("flag", (0.02, 0.2, 0.06), (0.14, 0.05, 1.3), "PaintRed")
    box("flagpole", (0.015, 0.015, 0.25), (0.14, 0.14, 1.1), "PaintRed")


@model
def build_VintageVan():
    """1930s delivery van 'Almond Water Co.'. Front toward -Y."""
    # chassis & wheels
    for y in (-1.35, 1.25):
        for sx in (-1, 1):
            torus(f"tyre{y}{sx}", 0.3, 0.12, (sx * 0.82, y, 0.42), "Rubber", rot=(0, math.pi / 2, 0), seg=32, mseg=12)
            cyl(f"hub{y}{sx}", 0.24, 0.12, (sx * 0.82 - sx * 0.06, y, 0.42), "PaintRed", 24, rot=(0, math.pi / 2, 0))
            cyl(f"cap{y}{sx}", 0.08, 0.14, (sx * 0.82 - sx * 0.07, y, 0.42), "Com_Chrome", 16, rot=(0, math.pi / 2, 0))
    box("chassis", (1.3, 4.2, 0.2), (0, 0, 0.35), "PaintBlack")
    # fenders (flat-top 1930s style)
    for y in (-1.35, 1.25):
        for sx in (-1, 1):
            box(f"fender{y}{sx}", (0.34, 1.0, 0.06), (sx * 0.82, y, 0.86), "PaintBlack", bevel=0.025)
            box(f"fenderf{y}{sx}", (0.34, 0.06, 0.3), (sx * 0.82, y - 0.5, 0.6), "PaintBlack", bevel=0.02, rot=(math.radians(-25), 0, 0))
    box("runboard", (0.3, 1.6, 0.05), (0.85, -0.05, 0.45), "PaintBlack")
    box("runboard2", (0.3, 1.6, 0.05), (-0.85, -0.05, 0.45), "PaintBlack")
    # hood + grille
    box("hood", (0.95, 1.1, 0.55), (0, -1.55, 0.6), "PaintCream", bevel=0.08)
    box("grille", (0.7, 0.05, 0.6), (0, -2.12, 0.55), "Com_Chrome", bevel=0.02)
    for i in range(10):
        box(f"gbar{i}", (0.02, 0.06, 0.52), (-0.3 + i * 0.066, -2.14, 0.59), "PaintBlack")
    for sx in (-1, 1):
        sphere(f"lamp{sx}", 0.13, (sx * 0.62, -2.0, 1.05), "Com_Chrome")
        cyl(f"lens{sx}", 0.1, 0.02, (sx * 0.62, -2.12, 1.05), "Glass", 24, rot=(math.pi / 2, 0, 0))
        box(f"lampbar{sx}", (0.35, 0.04, 0.04), (sx * 0.45, -1.95, 0.95), "Com_Chrome")
    box("bumper", (1.9, 0.1, 0.12), (0, -2.3, 0.4), "Com_Chrome", bevel=0.04)
    box("bumper_r", (1.9, 0.1, 0.12), (0, 2.15, 0.4), "Com_Chrome", bevel=0.04)
    # cab + box body
    box("cab", (1.7, 1.0, 1.55), (0, -0.55, 0.55), "PaintCream", bevel=0.06)
    box("windshield", (1.4, 0.03, 0.6), (0, -1.06, 1.35), "Glass")
    for sx in (-1, 1):
        box(f"sidewin{sx}", (0.03, 0.6, 0.5), (sx * 0.855, -0.6, 1.38), "Glass")
    box("body", (1.8, 2.7, 1.95), (0, 0.8, 0.5), "PaintCream", bevel=0.07)
    box("roof", (1.84, 3.8, 0.08), (0, 0.25, 2.45), "PaintCream", bevel=0.04)
    box("stripe_l", (0.01, 2.6, 0.12), (0.905, 0.8, 0.8), "PaintRed")
    box("stripe_r", (0.01, 2.6, 0.12), (-0.905, 0.8, 0.8), "PaintRed")
    for sx in (-1, 1):
        t1 = text(f"logo{sx}", "ALMOND WATER Co.", 0.22, "PaintRed", loc=(sx * 0.91, 0.8, 1.6), extrude=0.004, rot=(math.pi / 2, 0, sx * math.pi / 2), font=PLEX)
        t2 = text(f"logo2{sx}", "Fresh Daily · Level 94", 0.1, "PaintRed", loc=(sx * 0.91, 0.8, 1.3), extrude=0.003, rot=(math.pi / 2, 0, sx * math.pi / 2), font=PLEX)
    box("backdoor_l", (0.86, 0.02, 1.7), (-0.44, 2.16, 0.62), "PaintCream", bevel=0.02)
    box("backdoor_r", (0.86, 0.02, 1.7), (0.44, 2.16, 0.62), "PaintCream", bevel=0.02)
    for sx in (-1, 1):
        box(f"bhandle{sx}", (0.03, 0.04, 0.2), (sx * 0.08, 2.18, 1.4), "Com_Chrome")
        box(f"tail{sx}", (0.1, 0.05, 0.1), (sx * 0.8, 2.16, 0.7), "EmissiveRed")
    cyl("spare", 0.38, 0.14, (0, 2.22, 1.0), "Rubber", 32, rot=(math.pi / 2, 0, 0))
    cyl("steer", 0.2, 0.03, (0.4, -0.9, 1.25), "PaintBlack", 24, rot=(math.radians(60), 0, 0))


@model
def build_FloatingCastle():
    """Distant castle floating on an upside-down glass mountain (~60m). Pivot at castle ground level."""
    random.seed(94)
    lathe("mountain", [(0, -38), (4, -32), (10, -24), (18, -14), (24, -6), (27, -2), (28, 0), (0, 0)], "GlassFrosted", steps=9)
    box("court", (34, 30, 1.5), (0, 0, -1.5), "L94_Stone")
    # curtain walls
    for (x, y, w, d) in ((0, -14, 30, 2), (0, 14, 30, 2), (-15, 0, 2, 28), (15, 0, 2, 28)):
        box(f"wall{x}{y}", (w, d, 9), (x, y, 0), "L94_Stone")
        n = int(max(w, d) / 1.6)
        for i in range(n):
            if w > d:
                box(f"cren{x}{y}{i}", (0.9, d, 1.1), (x - w / 2 + 0.8 + i * 1.6, y, 9), "L94_Stone")
            else:
                box(f"cren{x}{y}{i}", (d, 0.9, 1.1), (x, y - d / 2 + 0.8 + i * 1.6, 9), "L94_Stone")
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl(f"tower{sx}{sy}", 3.2, 16, (sx * 15, sy * 14, 0), "L94_Stone", 24)
            cyl(f"troof{sx}{sy}", 3.8, 7, (sx * 15, sy * 14, 16), "L94_RoofGrey", 24, r2=0.05)
    box("keep", (12, 10, 22), (0, 3, 0), "L94_Stone")
    for sx in (-1, 1):
        cyl(f"ktower{sx}", 2.2, 30, (sx * 6, -2, 0), "L94_Stone", 20)
        cyl(f"kroof{sx}", 2.7, 8, (sx * 6, -2, 30), "L94_RoofGrey", 20, r2=0.05)
    polygon_prism("keeproof", [(-6.5, 0), (6.5, 0), (0, 6)], 10.5, "L94_RoofGrey", loc=(0, 8.25, 22), rot=(math.pi / 2, 0, 0))
    cyl("spire", 1.4, 38, (0, 3, 0), "L94_Stone", 16)
    cyl("spireroof", 1.8, 10, (0, 3, 38), "L94_RoofGrey", 16, r2=0.02)
    for i in range(24):
        x, z = random.uniform(-5, 5), random.uniform(4, 20)
        box(f"kwin{i}", (0.8, 0.2, 1.6), (x, -2.05, z), "EmissiveWarm")
    box("gate", (4, 0.4, 5.5), (0, -15.1, 0), "PaintBlack")


# ================================================================== LEVEL 37 (pool)
@model
def build_Pillar_Tiled():
    """Round tiled pillar r=0.35, 4.6m (floor to ceiling)."""
    p = cyl("shaft", 0.35, 4.6, (0, 0, 0), "L37_TileSmall", 40)
    cyl_uv_m(p, 0.35)
    b = cyl("base", 0.42, 0.12, (0, 0, 0), "L37_TileLarge", 40, bevel=0.03)
    cyl_uv_m(b, 0.42)
    c = cyl("capital", 0.45, 0.15, (0, 0, 4.45), "L37_TileLarge", 40, bevel=0.03)
    cyl_uv_m(c, 0.45)
    return ["shaft", "base", "capital"]


@model
def build_Arch_Tiled():
    """Wall segment 4m wide, 4.6m high, 0.6 thick with a round arch opening (2.6 x 3.6). Centered, bottom at 0."""
    wall = box("wall", (4.0, 0.6, 4.6), (0, 0, 0), "L37_TileSmall")
    cut = box("cut_rect", (2.6, 1.0, 2.3), (0, 0, -0.01), "L37_TileSmall")
    boolean(wall, cut)
    cyl_cut = cyl("cut_arc", 1.3, 1.0, (0, 0.5, 2.29), "L37_TileSmall", 48, rot=(math.pi / 2, 0, 0))
    boolean(wall, cyl_cut)
    for poly in wall.data.polygons:
        poly.use_smooth = False


@model
def build_PoolLadder():
    """Chrome pool ladder, hangs over the pool edge at Y=0 (pool toward +Y), pivot at deck level."""
    for sx in (-1, 1):
        x = sx * 0.28
        torus(f"bend{sx}", 0.2, 0.024, (x, 0.0, 0.55), "Com_Chrome", rot=(0, math.pi / 2, 0), seg=32, mseg=10)
        cyl(f"down{sx}", 0.024, 1.8, (x, 0.2, -1.25), "Com_Chrome", 16)
        cyl(f"deck{sx}", 0.024, 0.55, (x, -0.2, 0.0), "Com_Chrome", 16)
        cyl(f"flange{sx}", 0.045, 0.02, (x, -0.2, 0.0), "Com_Chrome", 16)
    for k in range(3):
        box(f"step{k}", (0.54, 0.1, 0.03), (0, 0.22, -0.35 - k * 0.3), "Steel", bevel=0.01)
    # cut the unwanted halves of the tori (keep the top half)
    for sx in (-1, 1):
        c = box(f"tcut{sx}", (0.2, 0.6, 0.4), (sx * 0.28, 0.0, 0.15), "Com_Chrome")
        boolean(bpy.data.objects[f"bend{sx}"], c)


@model
def build_Skylight():
    """3m x 3m skylight frame with glazing bars; sits in a ceiling opening (pivot at ceiling plane)."""
    for i in range(5):
        o = -1.5 + i * 0.75
        box(f"bx{i}", (3.0, 0.06, 0.12), (0, o, -0.06), "PaintOffWhite")
        box(f"by{i}", (0.06, 3.0, 0.12), (o, 0, -0.06), "PaintOffWhite")
    box("glass", (3.0, 3.0, 0.01), (0, 0, 0.0), "Glass")
    for (x, y, w, d) in ((0, -1.6, 3.4, 0.2), (0, 1.6, 3.4, 0.2), (-1.6, 0, 0.2, 3.0), (1.6, 0, 0.2, 3.0)):
        box(f"curb{x}{y}", (w, d, 0.5), (x, y, 0), "L37_TileLarge")


@model
def build_DrainGrate():
    box("frame", (0.4, 0.4, 0.01), (0, 0, 0), "Steel", bevel=0.002)
    cut = box("cut", (0.34, 0.34, 0.05), (0, 0, -0.02), "Steel")
    boolean(bpy.data.objects["frame"], cut)
    for i in range(9):
        box(f"bar{i}", (0.34, 0.012, 0.008), (0, -0.16 + i * 0.04, 0.0), "Steel")
    box("dark", (0.34, 0.34, 0.005), (0, 0, -0.03), "PaintBlack")


if __name__ == "__main__":
    houses = [
        make_house("House_Yellow", "L94_SidingYellow", "L94_RoofRed", "PaintGreen", "PaintGreen", 1),
        make_house("House_Blue", "L94_SidingBlue", "L94_RoofGrey", "PaintRed", "PaintOffWhite", 2, w=6.5, d=8.5, pitch=45),
        make_house("House_Green", "L94_SidingGreen", "L94_RoofRed", "PaintOffWhite", None, 3, w=7.5, d=9.5, pitch=38),
        make_house("House_White", "L94_SidingWhite", "L94_RoofGrey", "PaintBlack", "PaintGreen", 4, w=7.0, d=10.0, pitch=42),
        make_house("House_Brick", "L94_Brick", "L94_RoofGrey", "PaintGreen", None, 5, w=8.0, d=9.0, pitch=35),
    ]
    only = [a[5:] for a in sys.argv if a.startswith("only=")]
    fns = houses + [build_House_Door, build_WaterTower, build_Fountain, build_SirenPole, build_PicketFence, build_Mailbox, build_VintageVan,
                    build_FloatingCastle, build_Pillar_Tiled, build_Arch_Tiled, build_PoolLadder, build_Skylight, build_DrainGrate]
    for fn in fns:
        if only and fn.__name__.replace("build_", "") not in only[0].split(","):
            continue
        fn()
