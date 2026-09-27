"""Entities (articulated: one object per part, pivots at the joints, identity rotations).
  blender -b --factory-startup -P models_monsters.py
Exported to Assets/_Project/Art/Models/Custom/Monsters/<Name>.fbx with the part hierarchy kept.
"""
import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import *  # noqa

MOUT = os.path.join(OUT, "Monsters")


def part(name, objs, joint):
    """Join objs into one object named `name` whose origin sits at `joint` (world), rotation identity."""
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.context.view_layer.objects.active = objs[0]
    for o in objs:
        o.select_set(True)
    if len(objs) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name
    ob.data.name = name
    ob.data.transform(Matrix.Translation(-Vector(joint)))
    ob.location = joint
    for o in bpy.context.scene.objects:
        o.select_set(False)
    return ob


def parent(child, par):
    child.parent = par
    child.matrix_parent_inverse = par.matrix_world.inverted()


def export_rig(name):
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in objs:
        box_uv(o)
        o.select_set(True)
    os.makedirs(MOUT, exist_ok=True)
    path = os.path.join(MOUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=False,
                             mesh_smooth_type="FACE", use_tspace=True, add_leaf_bones=False, bake_anim=False)
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)
    print(f"[rig] {name}: parts={[o.name for o in objs]} tris={tris}")


def rig(fn):
    def w():
        reset()
        try:
            fn()
            export_rig(fn.__name__.replace("build_", ""))
        except Exception as e:
            import traceback
            traceback.print_exc()
            print("[FAIL]", fn.__name__, e)
    w.__name__ = fn.__name__
    return w


@rig
def build_Hound():
    """Pale, emaciated humanoid running on all fours. Faces -Y. ~1.5m long."""
    torso = part("Torso", [
        sphere("t", 0.5, (0, 0, 0.78), "MonsterSkin", seg=24, scale=(0.42, 1.25, 0.4)),
        sphere("ribs", 0.5, (0, -0.2, 0.74), "MonsterSkin", seg=16, scale=(0.46, 0.7, 0.42)),
        box("spine", (0.05, 1.0, 0.05), (0, 0, 0.96), "MonsterSkin", bevel=0.02),
    ], (0, 0, 0.78))
    head = part("Head", [
        sphere("skull", 0.17, (0, -0.78, 0.88), "MonsterSkin", seg=20, scale=(0.9, 1.2, 1.0)),
        box("jaw", (0.2, 0.22, 0.06), (0, -0.86, 0.74), "MonsterSkin", bevel=0.03),
        sphere("eyeL", 0.035, (0.07, -0.95, 0.92), "MonsterEye", seg=10),
        sphere("eyeR", 0.035, (-0.07, -0.95, 0.92), "MonsterEye", seg=10),
        box("teeth", (0.14, 0.02, 0.03), (0, -0.96, 0.78), "PlasticWhite"),
    ], (0, -0.58, 0.84))
    parent(head, torso)
    for nm, x, y in (("FL", 0.2, -0.45), ("FR", -0.2, -0.45), ("BL", 0.2, 0.45), ("BR", -0.2, 0.45)):
        up = part("Leg" + nm, [cyl("u", 0.055, 0.42, (x, y, 0.34), "MonsterSkin", 10)], (x, y, 0.76))
        low = part("Leg" + nm + "_Low", [
            cyl("l", 0.04, 0.36, (x, y - 0.05, 0.0), "MonsterSkin", 10),
            sphere("hand", 0.07, (x, y - 0.12, 0.03), "MonsterSkin", seg=10, scale=(0.9, 1.6, 0.4)),
        ], (x, y, 0.36))
        parent(up, torso)
        parent(low, up)


@rig
def build_Deathmoth():
    """Giant moth (~1.9m wingspan). Faces -Y. Origin at the body center."""
    body = part("Body", [
        sphere("abd", 0.5, (0, 0.18, 0), "MothBody", seg=18, scale=(0.24, 0.8, 0.24)),
        sphere("thorax", 0.14, (0, -0.2, 0), "MothBody", seg=16),
        sphere("head", 0.1, (0, -0.36, 0.02), "MothBody", seg=14),
        sphere("eyeL", 0.055, (0.07, -0.42, 0.04), "MonsterEye", seg=12),
        sphere("eyeR", 0.055, (-0.07, -0.42, 0.04), "MonsterEye", seg=12),
        cyl("antL", 0.008, 0.35, (0.04, -0.42, 0.05), "MothBody", 6, rot=(math.radians(-60), math.radians(20), 0)),
        cyl("antR", 0.008, 0.35, (-0.04, -0.42, 0.05), "MothBody", 6, rot=(math.radians(-60), math.radians(-20), 0)),
    ], (0, 0, 0))
    for side, sx in (("L", 1), ("R", -1)):
        w1 = part("Wing" + side, [
            sphere("w", 0.5, (sx * 0.52, -0.12, 0), "MothWing", seg=20, scale=(1.0, 0.55, 0.02)),
            sphere("spot", 0.09, (sx * 0.6, -0.12, 0.012), "MonsterEye", seg=14, scale=(1, 1, 0.1)),
            sphere("spot2", 0.13, (sx * 0.6, -0.12, 0.008), "MothBody", seg=14, scale=(1, 1, 0.1)),
        ], (sx * 0.08, -0.15, 0))
        w2 = part("Wing" + side + "2", [
            sphere("w2", 0.34, (sx * 0.36, 0.2, -0.01), "MothWing", seg=18, scale=(1.0, 0.6, 0.02)),
        ], (sx * 0.07, 0.08, 0))
        parent(w1, body)
        parent(w2, body)


@rig
def build_Smiler():
    """Only a glowing grin and eyes floating in the dark. Faces -Y."""
    objs = [
        sphere("eyeL", 0.07, (0.13, 0, 0.14), "SmilerGlow", seg=14, scale=(1.3, 0.3, 0.8)),
        sphere("eyeR", 0.07, (-0.13, 0, 0.14), "SmilerGlow", seg=14, scale=(1.3, 0.3, 0.8)),
    ]
    n = 18
    for i in range(n):
        a = math.radians(-70 + 140 * i / (n - 1))
        x, z = math.sin(a) * 0.3, -math.cos(a) * 0.18 - 0.02
        objs.append(box(f"t{i}", (0.03, 0.02, 0.07 if i % 2 else 0.05), (x, 0, z), "SmilerGlow", rot=(0, a * 0.6, 0)))
    objs.append(sphere("shade", 0.45, (0, 0.25, 0.02), "PaintBlack", seg=16, scale=(1.1, 0.6, 1.0)))
    part("Face", objs, (0, 0, 0))


@rig
def build_Toon():
    """'Animation': a 2.4m rubber-hose cartoon figure (pie eyes, wide grin, gloves). Faces -Y."""
    body = part("Body", [
        lathe("pear", [(0, 0.85), (0.22, 0.88), (0.3, 1.1), (0.26, 1.45), (0.16, 1.7), (0, 1.75)], "ToonBlack", steps=24),
        lathe("shorts", [(0.22, 0.86), (0.305, 1.05), (0.31, 1.12), (0.22, 1.1), (0.2, 0.88)], "PaintRed", steps=24),
        cyl("neck", 0.06, 0.15, (0, 0, 1.72), "ToonBlack", 10),
    ], (0, 0, 0.9))
    head = part("Head", [
        sphere("skull", 0.3, (0, 0, 2.1), "ToonBlack", seg=24),
        sphere("snout", 0.2, (0, -0.2, 2.0), "ToonWhite", seg=18, scale=(1.1, 0.8, 0.75)),
        sphere("nose", 0.06, (0, -0.37, 2.07), "ToonBlack", seg=12),
        sphere("eyeWL", 0.09, (0.1, -0.24, 2.22), "ToonWhite", seg=14, scale=(0.7, 0.4, 1.3)),
        sphere("eyeWR", 0.09, (-0.1, -0.24, 2.22), "ToonWhite", seg=14, scale=(0.7, 0.4, 1.3)),
        sphere("pupL", 0.04, (0.1, -0.29, 2.2), "ToonBlack", seg=10, scale=(0.8, 0.4, 1.5)),
        sphere("pupR", 0.04, (-0.1, -0.29, 2.2), "ToonBlack", seg=10, scale=(0.8, 0.4, 1.5)),
        box("grin", (0.34, 0.04, 0.05), (0, -0.33, 1.9), "ToonWhite", bevel=0.02),
        cyl("earL", 0.16, 0.03, (0.27, 0.05, 2.36), "ToonBlack", 20, rot=(math.pi / 2, 0, 0)),
        cyl("earR", 0.16, 0.03, (-0.27, 0.05, 2.36), "ToonBlack", 20, rot=(math.pi / 2, 0, 0)),
    ], (0, 0, 1.78))
    parent(head, body)
    for side, sx in (("L", 1), ("R", -1)):
        arm = part("Arm" + side, [
            cyl("a", 0.035, 0.85, (sx * 0.32, 0, 0.72), "ToonBlack", 10),
            sphere("glove", 0.11, (sx * 0.32, 0, 0.7), "ToonWhite", seg=14, scale=(1, 1, 1.2)),
            torus("cuff", 0.08, 0.025, (sx * 0.32, 0, 0.8), "ToonWhite", seg=16, mseg=6),
        ], (sx * 0.3, 0, 1.58))
        leg = part("Leg" + side, [
            cyl("l", 0.04, 0.8, (sx * 0.13, 0, 0.1), "ToonBlack", 10),
            sphere("shoe", 0.14, (sx * 0.13, -0.08, 0.08), "PaintYellow", seg=16, scale=(1.0, 1.6, 0.6)),
        ], (sx * 0.13, 0, 0.9))
        parent(arm, body)
        parent(leg, body)


@rig
def build_Slime():
    """Translucent green blob (1m) with a darker core and two beady eyes. Pivot at the ground."""
    part("Blob", [
        sphere("b", 0.5, (0, 0, 0.36), "Slime", seg=28, scale=(1, 1, 0.72)),
        sphere("core", 0.17, (0, 0.05, 0.3), "SlimeCore", seg=16),
        sphere("eyeL", 0.045, (0.13, -0.44, 0.48), "PaintBlack", seg=10),
        sphere("eyeR", 0.045, (-0.13, -0.44, 0.48), "PaintBlack", seg=10),
    ], (0, 0, 0))


@rig
def build_Leviathan():
    """Huge dark eel-like silhouette (~30m) made of a chain of segments. Faces -Y."""
    prev = None
    for i in range(10):
        r = 1.6 * (1 - i / 11) + 0.2
        y = i * 3.0
        seg = part(f"Seg{i}", [
            sphere("s", 1.0, (0, y + 1.5, 0), "LeviathanSkin", seg=20, scale=(r, 2.0, r * 0.8)),
        ] + ([cyl("fin", 0.05, 1.8 * r, (0, y + 1.5, r * 0.6), "LeviathanSkin", 8, r2=0.01)] if i % 2 == 0 else []),
            (0, y, 0))
        if prev:
            parent(seg, prev)
        prev = seg


if __name__ == "__main__":
    for fn in (build_Hound, build_Deathmoth, build_Smiler, build_Toon, build_Slime, build_Leviathan):
        fn()
