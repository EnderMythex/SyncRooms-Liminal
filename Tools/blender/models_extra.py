"""Level 2 pipe modules, Hub level signs, misc.  blender -b --factory-startup -P models_extra.py"""
import math
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sr_lib import *  # noqa

PLEX = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "IBMPlexMono-SemiBold.ttf")


@model
def build_PipeRun():
    """6m bundle of pipes along a wall. Wall at Y=+0 (back), corridor toward -Y. Pivot bottom center."""
    L = 6.0
    specs = [(0.35, 0.09, "L5_Rust"), (1.85, 0.16, "DarkMetal"), (2.3, 0.07, "PaintGreen"), (2.55, 0.11, "L5_Rust")]
    for i, (z, r, m) in enumerate(specs):
        p = cyl(f"p{i}", r, L, (-L / 2, -0.25 - r, z), m, 20, rot=(0, math.pi / 2, 0))
        for k in (0, 1):
            torus(f"fl{i}{k}", r + 0.012, 0.018, (-L / 2 + 0.05 + k * (L - 0.1), -0.25 - r, z), "Steel", rot=(0, math.pi / 2, 0), seg=20, mseg=6)
    for k in range(5):
        x = -L / 2 + 0.6 + k * 1.2
        box(f"br{k}", (0.05, 0.5, 2.7), (x, -0.2, 0.05), "Steel")
    torus("wheel", 0.13, 0.015, (0.8, -0.57, 1.85), "PaintRed", rot=(math.pi / 2, 0, 0), seg=20)
    box("spoke", (0.26, 0.015, 0.015), (0.8, -0.57, 1.84), "PaintRed")
    cyl("stem", 0.02, 0.2, (0.8, -0.42, 1.85), "Steel", 8, rot=(math.pi / 2, 0, 0))
    cyl("gauge", 0.08, 0.04, (-1.4, -0.5, 1.5), "Brass", 24, rot=(math.pi / 2, 0, 0))
    cyl("gface", 0.068, 0.004, (-1.4, -0.52, 1.5), "PaintCream", 24, rot=(math.pi / 2, 0, 0))
    cyl("gstem", 0.015, 0.2, (-1.4, -0.44, 1.35), "Steel", 8)


@model
def build_PipeCeiling():
    """6m of pipes hanging under the ceiling (pivot at ceiling height, pipes hang below)."""
    L = 6.0
    for i, (x, r, m) in enumerate(((-0.6, 0.12, "DarkMetal"), (-0.2, 0.08, "L5_Rust"), (0.35, 0.15, "L5_Rust"), (0.75, 0.06, "PaintGreen"))):
        cyl(f"c{i}", r, L, (x, -L / 2, -0.25 - r), m, 20, rot=(-math.pi / 2, 0, 0))
    for k in range(4):
        box(f"hang{k}", (1.7, 0.05, 0.05), (0.07, -L / 2 + 0.75 + k * 1.5, -0.5), "Steel")
        box(f"rodL{k}", (0.02, 0.02, 0.5), (-0.75, -L / 2 + 0.75 + k * 1.5, -0.5), "Steel")
        box(f"rodR{k}", (0.02, 0.02, 0.5), (0.9, -L / 2 + 0.75 + k * 1.5, -0.5), "Steel")


def sign(scene, label):
    def build():
        box("plate", (1.0, 0.03, 0.34), (0, 0, 0), "Brass", bevel=0.01)
        text("t", label, 0.17, "PaintBlack", loc=(0, -0.018, 0.17), extrude=0.004, font=PLEX)
    build.__name__ = "build_Sign_" + scene
    return model(build)


if __name__ == "__main__":
    fns = [build_PipeRun, build_PipeCeiling]
    for scene, label in (("Level1", "LEVEL 1"), ("Level2", "LEVEL 2"), ("Level4", "LEVEL 4"), ("Level5", "LEVEL 5"),
                         ("Level7", "LEVEL 7"), ("Level37", "LEVEL 37"), ("Level94", "LEVEL 94"), ("MainMenu", "SORTIE")):
        fns.append(sign(scene, label))
    for fn in fns:
        fn()
