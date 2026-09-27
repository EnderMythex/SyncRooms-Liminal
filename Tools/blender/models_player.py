"""Player body + first-person arms (articulated), rowboat and lighthouse.
  blender -b --factory-startup -P models_player.py
"""
import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import *  # noqa
import models_monsters as mm

ROUT = os.path.join(OUT, "Rigs")


def export_rig(name):
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in objs:
        box_uv(o)
        o.select_set(True)
    os.makedirs(ROUT, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(ROUT, name + ".fbx"), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=False,
                             mesh_smooth_type="FACE", use_tspace=True, add_leaf_bones=False, bake_anim=False)
    print(f"[rig] {name}: {[o.name for o in objs]}")


def build_PlayerBody():
    """Faces -Y. No head (the camera is there). Hips pivot at 0.95m."""
    reset()
    part, parent = mm.part, mm.parent
    hips = part("Hips", [box("pelvis", (0.34, 0.2, 0.2), (0, 0, 0.86), "Jeans", bevel=0.04)], (0, 0, 0.95))
    torso = part("Torso", [
        box("chest", (0.38, 0.22, 0.48), (0, 0, 1.02), "Jacket", bevel=0.06),
        box("zip", (0.02, 0.01, 0.46), (0, -0.115, 1.03), "Steel"),
        box("collar", (0.3, 0.2, 0.06), (0, 0, 1.48), "Jacket", bevel=0.03),
        cyl("shL", 0.07, 0.42, (0.23, 0, 1.02), "Jacket", 12),
        cyl("shR", 0.07, 0.42, (-0.23, 0, 1.02), "Jacket", 12),
    ], (0, 0, 1.0))
    parent(torso, hips)
    for side, sx in (("L", 1), ("R", -1)):
        up = part("Leg" + side, [cyl("thigh", 0.085, 0.46, (sx * 0.1, 0, 0.47), "Jeans", 14)], (sx * 0.1, 0, 0.93))
        low = part("Leg" + side + "_Low", [
            cyl("shin", 0.07, 0.42, (sx * 0.1, 0, 0.07), "Jeans", 14),
            box("shoe", (0.12, 0.28, 0.1), (sx * 0.1, -0.07, 0.0), "ShoeLeather", bevel=0.04),
        ], (sx * 0.1, 0, 0.49))
        parent(up, hips)
        parent(low, up)
    export_rig("PlayerBody")


def build_FPArms():
    """Two forearms pointing forward (-Y) from their elbow pivots, with hands. Positioned by code under the camera."""
    reset()
    part = mm.part
    for side, sx in (("L", 1), ("R", -1)):
        part("Arm" + side, [
            cyl("sleeve", 0.052, 0.3, (sx * 0.0, 0, 0), "Jacket", 14, rot=(math.pi / 2, 0, 0)),
            torus("cuff", 0.05, 0.012, (0, -0.3, 0), "Jacket", rot=(math.pi / 2, 0, 0), seg=16, mseg=6),
            sphere("wrist", 0.036, (0, -0.32, 0), "Skin", seg=12),
            box("palm", (0.085, 0.1, 0.035), (0, -0.37, -0.015), "Skin", bevel=0.015),
            box("fingers", (0.08, 0.06, 0.03), (0, -0.44, 0.0), "Skin", bevel=0.012, rot=(math.radians(-50), 0, 0)),
            box("thumb", (0.025, 0.06, 0.025), (sx * -0.05, -0.37, 0.01), "Skin", bevel=0.01, rot=(0, 0, math.radians(sx * 30))),
        ], (0, 0, 0))
        bpy.data.objects["Arm" + side].location.x = sx * 0.001
    export_rig("FPArms")


@model
def build_Rowboat():
    """Wooden rowboat 3.6m, front toward -Y, pivot at the waterline center."""
    prof = [(0.0, -0.35), (0.35, -0.33), (0.62, -0.2), (0.74, 0.0), (0.78, 0.25), (0.8, 0.3)]
    hull = lathe("hull", prof, "L94_Wood", steps=24)
    hull.scale = (1.0, 2.3, 1.0)
    inner = lathe("inner", [(0.0, -0.28), (0.3, -0.27), (0.56, -0.15), (0.68, 0.05), (0.72, 0.3)], "L5_Walnut", steps=24)
    inner.scale = (1.0, 2.25, 1.0)
    for y in (-0.8, 0.1, 0.9):
        box(f"seat{y}", (1.3, 0.28, 0.04), (0, y, 0.05), "L94_Wood")
    box("keel", (0.06, 3.6, 0.1), (0, 0, -0.4), "L5_Walnut")
    for sx in (-1, 1):
        cyl(f"oarlock{sx}", 0.02, 0.1, (sx * 0.76, 0.1, 0.3), "Steel", 8)
        oar = cyl(f"oar{sx}", 0.025, 2.2, (sx * 0.76, 0.1, 0.35), "L94_Wood", 8, rot=(0, math.radians(sx * 70), math.radians(sx * 10)))
        box(f"blade{sx}", (0.03, 0.14, 0.5), (sx * 2.7, 0.25, 0.0), "L94_Wood", rot=(0, math.radians(sx * 70), 0))
    cyl("lanternpole", 0.02, 0.9, (0, -1.5, 0.05), "Steel", 8)
    cyl("lantern", 0.08, 0.2, (0, -1.5, 0.95), "EmissiveWarm", 12)
    cyl("lantop", 0.1, 0.05, (0, -1.5, 1.15), "Steel", 12, r2=0.02)


@model
def build_Lighthouse():
    """Rusted offshore lighthouse on a platform (pivot at platform deck, deck at z=0). Hatch at (2, 2)."""
    box("deck", (12, 12, 1.5), (0, 0, -1.5), "L5_Rust")
    for x in (-5, 5):
        for y in (-5, 5):
            cyl(f"pile{x}{y}", 0.45, 12, (x, y, -13), "DarkMetal", 16)
    lathe("tower", [(3.0, 0), (2.8, 6), (2.4, 14), (2.2, 18), (0, 18)], "PaintOffWhite", steps=32)
    for z in (4, 9, 14):
        torus(f"band{z}", 2.8 - z * 0.03, 0.08, (0, 0, z), "PaintRed", seg=40)
    cyl("gallery", 3.1, 0.2, (0, 0, 18), "DarkMetal", 32)
    cyl("lamp", 1.4, 2.2, (0, 0, 18.2), "Glass", 24)
    cyl("lampcore", 0.5, 1.2, (0, 0, 18.6), "EmissiveWarm", 16)
    lathe("cap", [(1.6, 20.4), (1.2, 21.4), (0.2, 22.2), (0, 22.3)], "PaintRed", steps=24)
    box("door", (1.1, 0.3, 2.2), (0, -2.9, 0), "PaintBlack")
    box("hatch", (1.2, 1.2, 0.06), (3.8, 3.8, 0.0), "Steel")
    box("dock", (3, 6, 0.3), (0, -8.8, -0.6), "L94_Wood")
    for sx in (-1, 1):
        for y in (-6.5, -9, -11.5):
            cyl(f"dpost{sx}{y}", 0.12, 3, (sx * 1.4, y, -2.5), "L94_Wood", 8)


if __name__ == "__main__":
    build_PlayerBody()
    build_FPArms()
    build_Rowboat()
    build_Lighthouse()
