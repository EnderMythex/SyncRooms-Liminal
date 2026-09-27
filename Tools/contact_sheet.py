"""Build a labeled-order contact sheet from images. Run with Blender:
blender -b --factory-startup -P contact_sheet.py -- out.png cols cell img1 img2 ...
"""
import sys

import bpy
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:]
out, cols, cell, files = argv[0], int(argv[1]), int(argv[2]), argv[3:]
rows = (len(files) + cols - 1) // cols
sheet = np.zeros((rows * cell, cols * cell, 4), dtype=np.float32)
sheet[..., 3] = 1
for i, f in enumerate(files):
    img = bpy.data.images.load(f)
    img.scale(cell, cell)
    px = np.array(img.pixels[:], dtype=np.float32).reshape(cell, cell, 4)
    r, c = rows - 1 - i // cols, i % cols  # Blender pixel origin is bottom-left
    sheet[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = px
    print(i, f)
res = bpy.data.images.new("sheet", cols * cell, rows * cell, alpha=True)
res.pixels = sheet.ravel()
res.filepath_raw = out
res.file_format = "PNG"
res.save()
