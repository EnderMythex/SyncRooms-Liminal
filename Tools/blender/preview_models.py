"""Render a QA contact sheet of custom FBX models (Workbench, 3/4 view from the front).
  blender -b --factory-startup -P preview_models.py -- out.png [filter]
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import OUT  # noqa

args = sys.argv[sys.argv.index("--") + 1:]
out_png = args[0]
flt = args[1].split(",") if len(args) > 1 else None
tmp = os.path.join(os.path.dirname(out_png), "_prev")
os.makedirs(tmp, exist_ok=True)

COLORS = {
    "Glass": (0.6, 0.8, 0.9), "GlassFrosted": (0.8, 0.85, 0.9), "Brass": (0.8, 0.62, 0.3), "Steel": (0.6, 0.6, 0.62),
    "EmissivePanel": (1, 1, 0.9), "EmissiveWarm": (1, 0.8, 0.5), "EmissiveExit": (0.1, 0.9, 0.3), "BlackOut": (0.05, 0.05, 0.05),
}

files = sorted(f for f in os.listdir(OUT) if f.endswith(".fbx"))
if flt:
    files = [f for f in files if any(k in f for k in flt)]
imgs = []
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, f))
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in objs:
        for m in o.data.materials:
            if m:
                c = COLORS.get(m.name)
                if c is None:
                    h = (hash(m.name) % 1000) / 1000
                    c = (0.4 + 0.5 * h, 0.4 + 0.5 * ((h * 7) % 1), 0.4 + 0.5 * ((h * 13) % 1))
                m.diffuse_color = (*c, 1)
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    ctr = (mn + mx) / 2
    size = (mx - mn).length
    cam_data = bpy.data.cameras.new("c")
    cam = bpy.data.objects.new("c", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    # After FBX import, Blender front (-Y) stays -Y. View from front-left-above.
    d = Vector((-0.8, -1.6, 0.9)).normalized() * size * 1.35
    cam.location = ctr + d
    cam.rotation_euler = (ctr - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 40
    sc = bpy.context.scene
    sc.camera = cam
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "MATERIAL"
    sc.display.shading.show_cavity = True
    sc.display.shading.show_shadows = True
    sc.render.resolution_x = sc.render.resolution_y = 320
    sc.render.filepath = os.path.join(tmp, f + ".png")
    bpy.ops.render.render(write_still=True)
    imgs.append(sc.render.filepath)
    print("[prev]", f)

# contact sheet
import numpy as np
cols = 5
cell = 320
rows = (len(imgs) + cols - 1) // cols
sheet = np.ones((rows * cell, cols * cell, 4), np.float32) * 0.15
for i, p in enumerate(imgs):
    im = bpy.data.images.load(p)
    a = np.empty(cell * cell * 4, np.float32)
    im.pixels.foreach_get(a)
    r, c = rows - 1 - i // cols, i % cols
    sheet[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = a.reshape(cell, cell, 4)
res = bpy.data.images.new("sheet", cols * cell, rows * cell, alpha=True)
res.pixels.foreach_set(sheet.ravel())
res.filepath_raw = out_png
res.file_format = "PNG"
res.save()
print("[sheet]", out_png, [os.path.basename(p) for p in imgs])
