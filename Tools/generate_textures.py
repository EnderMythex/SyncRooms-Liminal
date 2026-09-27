"""Procedural ornamental textures for Level 5 (Terror Hotel), built on top of scanned CC0 base materials.
  blender -b --factory-startup -P generate_textures.py
Creates Assets/_Project/Art/Materials/Textures/{L5_Damask, L5_HotelCarpet}/ (Albedo, Normal, Mask, Height)
"""
import os

import bpy
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEX = os.path.join(ROOT, "Assets", "_Project", "Art", "Materials", "Textures")
N = 2048


def load(path, size=N):
    img = bpy.data.images.load(path)
    img.colorspace_settings.name = "Non-Color"  # raw values, we handle color ourselves
    if tuple(img.size) != (size, size):
        img.scale(size, size)
    a = np.empty(size * size * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    bpy.data.images.remove(img)
    return a.reshape(size, size, 4)


def save(arr, path, fmt="PNG", srgb=False):
    h, w = arr.shape[:2]
    if arr.shape[2] == 3:
        arr = np.concatenate([arr, np.ones((h, w, 1), np.float32)], axis=2)
    img = bpy.data.images.new("out", w, h, alpha=True, float_buffer=False)
    img.colorspace_settings.name = "Non-Color"
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = fmt
    if fmt == "JPEG":
        bpy.context.scene.render.image_settings.quality = 92
    img.save()
    bpy.data.images.remove(img)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def grid():
    v, u = np.mgrid[0:N, 0:N].astype(np.float32) / N
    return u, v


def half_drop_local(u, v, cols, rows):
    """Local coords (in cell units, centered) to nearest motif on a half-drop lattice. Tileable."""
    x = u * cols
    y = v * rows
    # lattice A: integer centers + 0.5 ; lattice B: offset by (0.5, 0.5)
    ax = x - np.floor(x) - 0.5
    ay = y - np.floor(y) - 0.5
    bx = (x + 0.5) - np.floor(x + 0.5) - 0.5
    by = (y + 0.5) - np.floor(y + 0.5) - 0.5
    return ax, ay, bx, by


def ellipse(x, y, cx, cy, rx, ry, soft=0.0022):
    d = np.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
    return 1 - smooth(1 - soft / min(rx, ry), 1 + soft / min(rx, ry), d)


def damask_motif(x, y):
    """Mirrored ornamental motif, x,y in [-0.5,0.5] (y up). Returns 0..1 coverage."""
    ax = np.abs(x)
    m = np.zeros_like(x)
    # central flame / teardrop
    body = ellipse(ax, y, 0, -0.02, 0.085, 0.2)
    hole = ellipse(ax, y, 0, -0.04, 0.045, 0.12)
    m = np.maximum(m, body - hole * 0.85)
    # inner seed
    m = np.maximum(m, ellipse(ax, y, 0, -0.06, 0.02, 0.05))
    # crown lobes on top
    m = np.maximum(m, ellipse(ax, y, 0, 0.27, 0.035, 0.09))
    m = np.maximum(m, ellipse(ax, y, 0.07, 0.22, 0.03, 0.07) * (ax > 0.03))
    # side leaves (rotated ellipses)
    for (cx, cy, rx, ry, ang) in [(0.16, 0.05, 0.035, 0.13, -0.6), (0.2, -0.17, 0.03, 0.11, 0.7), (0.12, -0.3, 0.025, 0.08, 1.2)]:
        ca, sa = np.cos(ang), np.sin(ang)
        px, py = ax - cx, y - cy
        rx_, ry_ = px * ca - py * sa, px * sa + py * ca
        m = np.maximum(m, ellipse(rx_, ry_, 0, 0, rx, ry))
    # scrolls: spiral arcs
    for (cx, cy, s) in [(0.3, 0.2, 1.0), (0.28, -0.32, -1.0)]:
        px, py = ax - cx, (y - cy) * s
        r = np.sqrt(px ** 2 + py ** 2)
        th = np.arctan2(py, px) % (2 * np.pi)
        target = 0.012 + 0.012 * th
        band = 1 - smooth(0.006, 0.012, np.abs(r - target))
        m = np.maximum(m, band * (r < 0.1))
    # stem connecting to the lattice diagonals
    m = np.maximum(m, (1 - smooth(0.006, 0.011, np.abs(ax - np.abs(y + 0.42) * 0.3))) * (y < -0.22) * (y > -0.5))
    return np.clip(m, 0, 1)


def small_motif(x, y):
    ax, ay = np.abs(x), np.abs(y)
    d = ax + ay
    m = 1 - smooth(0.05, 0.058, d)
    ring = (1 - smooth(0.004, 0.009, np.abs(d - 0.09)))
    dots = ellipse(ax, ay, 0.12, 0.0, 0.012, 0.012) + ellipse(ax, ay, 0.0, 0.12, 0.012, 0.012)
    return np.clip(np.maximum(np.maximum(m, ring), dots), 0, 1)


def tile_noise(u, v, octaves, seed):
    rng = np.random.default_rng(seed)
    n = np.zeros_like(u)
    for o in range(octaves):
        f = 2 ** (o + 1)
        for _ in range(3):
            fx, fy = rng.integers(1, f + 1), rng.integers(1, f + 1)
            ph = rng.random() * 6.283
            n += np.sin(2 * np.pi * (fx * u + fy * v) + ph) / (o + 1)
    return (n - n.min()) / (n.max() - n.min())


def height_to_normal(h, strength):
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * strength
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * strength
    n = np.stack([-dx, -dy, np.ones_like(h)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n


def blend_normals(base, detail):
    # Reoriented normal mapping approximation (whiteout)
    n = np.stack([base[..., 0] + detail[..., 0], base[..., 1] + detail[..., 1], base[..., 2] * detail[..., 2]], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def srgb_to_lin(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def make_damask():
    base_dir = os.path.join(TEX, "L5_WallpaperBase")
    out = os.path.join(TEX, "L5_Damask")
    os.makedirs(out, exist_ok=True)
    u, v = grid()
    ax, ay, bx, by = half_drop_local(u, v, 2, 2)
    pat = damask_motif(ax, ay)
    pat = np.maximum(pat, small_motif(bx, by) * 0.9)

    paper = load(os.path.join(base_dir, "Albedo.jpg"))[..., :3]
    lum = paper.mean(-1, keepdims=True)
    grain = lum / lum.mean()
    stain = tile_noise(u, v, 3, 7)[..., None]

    gy, gx = np.gradient(pat)
    outline = np.clip(np.sqrt(gx ** 2 + gy ** 2) * 900, 0, 1)
    red = srgb_to_lin(np.array([0.42, 0.07, 0.075]))
    velvet = srgb_to_lin(np.array([0.24, 0.028, 0.035]))
    gold = srgb_to_lin(np.array([0.78, 0.6, 0.3]))
    p = pat[..., None]
    o = outline[..., None]
    alb_lin = red * (1 - p) + velvet * p
    alb_lin = alb_lin * (1 - o) + gold * o
    alb_lin *= (0.82 + 0.18 * grain) * (0.94 + 0.06 * stain)
    alb = lin_to_srgb(alb_lin)
    save(alb, os.path.join(out, "Albedo.jpg"), "JPEG")

    bn = load(os.path.join(base_dir, "Normal.jpg"))[..., :3] * 2 - 1
    emb = height_to_normal(pat * 0.6 + outline * 0.4, 5.0)
    n = blend_normals(bn, emb)
    save(n * 0.5 + 0.5, os.path.join(out, "Normal.jpg"), "JPEG")

    rough_paper = 1 - load(os.path.join(base_dir, "Mask.png"), N)[..., 3]
    smooth_ = (1 - rough_paper) * 0.45 * (1 - pat) + pat * 0.12
    smooth_ = smooth_ * (1 - outline) + outline * 0.62
    mask = np.stack([outline * 0.85, 1 - 0.1 * (1 - stain[..., 0]), np.ones_like(pat), smooth_], -1)
    save(mask[::2, ::2], os.path.join(out, "Mask.png"))
    save(np.repeat((0.5 + pat * 0.3)[::2, ::2, None], 3, -1), os.path.join(out, "Height.jpg"), "JPEG")
    print("[gen] L5_Damask")


def make_carpet():
    base_dir = os.path.join(TEX, "L5_CarpetBase")
    out = os.path.join(TEX, "L5_HotelCarpet")
    os.makedirs(out, exist_ok=True)
    u, v = grid()
    cells = 3
    x = u * cells
    y = v * cells
    fx = x - np.floor(x) - 0.5
    fy = y - np.floor(y) - 0.5
    # diamond lattice lines
    d = np.abs(fx) + np.abs(fy)
    lines = 1 - smooth(0.012, 0.02, np.abs(d - 0.5))
    lines2 = 1 - smooth(0.006, 0.011, np.abs(d - 0.44))
    # central rosette
    r = np.sqrt(fx ** 2 + fy ** 2)
    th = np.arctan2(fy, fx)
    petals = 1 - smooth(0.0, 0.015, r - (0.14 + 0.05 * np.cos(8 * th)))
    core = 1 - smooth(0.0, 0.01, r - 0.05)
    ring = 1 - smooth(0.006, 0.012, np.abs(r - 0.22))
    # corner fleurons (at diamond vertices)
    cx = np.abs(np.abs(fx) - 0.5)
    cy = np.abs(fy)
    fleur = (1 - smooth(0.0, 0.01, np.sqrt(cx ** 2 * 4 + cy ** 2) - 0.06))
    fleur2 = (1 - smooth(0.0, 0.01, np.sqrt(cy ** 2 * 4 + np.abs(np.abs(fy) - 0.5) ** 2 + fx ** 2 * 0) - 0.0))

    red = srgb_to_lin(np.array([0.42, 0.06, 0.06]))
    red_dark = srgb_to_lin(np.array([0.26, 0.035, 0.04]))
    gold = srgb_to_lin(np.array([0.66, 0.48, 0.2]))
    navy = srgb_to_lin(np.array([0.08, 0.09, 0.16]))
    inside = d < 0.44
    alb_lin = np.where(inside[..., None], red, red_dark)
    alb_lin = alb_lin * (1 - petals[..., None]) + navy * petals[..., None]
    alb_lin = alb_lin * (1 - core[..., None]) + gold * core[..., None]
    for m in (lines, lines2, ring, fleur):
        alb_lin = alb_lin * (1 - m[..., None]) + gold * m[..., None]

    fiber = load(os.path.join(base_dir, "Albedo.jpg"))[..., :3]
    lum = fiber.mean(-1, keepdims=True)
    alb_lin *= 0.7 + 0.3 * (lum / lum.mean())
    wear = tile_noise(u, v, 2, 3)[..., None]
    alb_lin *= 0.88 + 0.12 * wear
    save(lin_to_srgb(alb_lin), os.path.join(out, "Albedo.jpg"), "JPEG")
    bn = load(os.path.join(base_dir, "Normal.jpg"))[..., :3]
    save(bn, os.path.join(out, "Normal.jpg"), "JPEG")
    m = load(os.path.join(base_dir, "Mask.png"), N)
    m[..., 3] *= 0.6
    save(m[::2, ::2], os.path.join(out, "Mask.png"))
    h = load(os.path.join(base_dir, "Height.jpg"), N)
    save(h[::2, ::2, :3], os.path.join(out, "Height.jpg"), "JPEG")
    print("[gen] L5_HotelCarpet")


make_damask()
make_carpet()
