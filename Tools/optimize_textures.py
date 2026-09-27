"""Shrink texture sizes on disk (repo-friendly) without visible loss.
  blender -b --factory-startup -P optimize_textures.py
Albedo/Normal: re-encode JPG q90 at native res. Height: 1K JPG. Mask: 1K PNG.
Marks each folder with .optimized so it runs once.
"""
import os

import bpy

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
import sys
ONLY_MATS = "mats" in sys.argv
DIRS = [os.path.join(ROOT, "Assets", "_Project", "Art", "Materials", "Textures"),
        os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "PolyHaven")]
if ONLY_MATS:
    DIRS = DIRS[:1]


def resave(path, fmt, size=None, quality=90):
    img = bpy.data.images.load(path)
    if size and img.size[0] > size:
        img.scale(size, int(size * img.size[1] / img.size[0]))
    scene = bpy.context.scene
    s = scene.render.image_settings
    s.file_format = fmt
    s.quality = quality
    s.color_mode = "RGBA" if fmt == "PNG" else "RGB"
    s.color_depth = "8"
    s.compression = 100
    img.save_render(path, scene=scene)
    bpy.data.images.remove(img)


def main():
    scene = bpy.context.scene
    scene.view_settings.view_transform = "Standard"
    scene.display_settings.display_device = "sRGB"
    for base in DIRS:
        for d, _, files in os.walk(base):
            marker = os.path.join(d, ".optimized")
            if os.path.exists(marker) or not files:
                continue
            for f in files:
                p = os.path.join(d, f)
                low = f.lower()
                if low.endswith(".jpg") and "height" in low:
                    resave(p, "JPEG", 1024)
                elif low.endswith(".jpg"):
                    resave(p, "JPEG", None, 90)
                elif low.endswith("mask.png"):
                    resave(p, "PNG", 1024)
            open(marker, "w").close()
            print("[opt]", os.path.relpath(d, ROOT))


main()
