"""Download PBR materials (ambientCG / Poly Haven, CC0) and pack HDRP maps.

Run inside Blender (for image IO + numpy):
  blender -b --factory-startup -P fetch_textures.py -- [res=2K] [only=Name1,Name2]

Output per material in Assets/_Project/Art/Materials/Textures/<Name>/:
  Albedo.jpg   base color
  Normal.jpg   OpenGL normal map
  Height.jpg   displacement (if available)
  Mask.png     HDRP mask map: R metallic, G AO, B detail mask (1), A smoothness
"""
import io
import json
import os
import shutil
import sys
import urllib.request
import zipfile

import bpy
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Materials", "Textures")
CACHE = os.path.join(ROOT, "Tools", "cache", "textures")
UA = {"User-Agent": "SyncRooms-Liminal-AssetFetcher/1.0"}

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opts = dict(a.split("=", 1) for a in args if "=" in a)
RES = opts.get("res", "2K")
ONLY = set(opts["only"].split(",")) if "only" in opts else None


def http(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=180) as r:
        return r.read()


def cached(url, name):
    os.makedirs(CACHE, exist_ok=True)
    p = os.path.join(CACHE, name)
    if not os.path.exists(p):
        data = http(url)
        with open(p, "wb") as f:
            f.write(data)
    return p


def fetch_acg(asset_id):
    """Returns dict of map-name -> local file path."""
    z = cached(f"https://ambientcg.com/get?file={asset_id}_{RES}-JPG.zip", f"{asset_id}_{RES}.zip")
    d = os.path.join(CACHE, f"{asset_id}_{RES}")
    if not os.path.isdir(d):
        zipfile.ZipFile(z).extractall(d)
    maps = {}
    for f in os.listdir(d):
        low = f.lower()
        full = os.path.join(d, f)
        if low.endswith("_color.jpg"): maps["albedo"] = full
        elif low.endswith("_normalgl.jpg"): maps["normal"] = full
        elif low.endswith("_roughness.jpg"): maps["rough"] = full
        elif low.endswith("_ambientocclusion.jpg"): maps["ao"] = full
        elif low.endswith("_displacement.jpg"): maps["height"] = full
        elif low.endswith("_metalness.jpg"): maps["metal"] = full
        elif low.endswith("_opacity.jpg"): maps["opacity"] = full
    return maps


def fetch_ph(asset_id):
    files = json.loads(http(f"https://api.polyhaven.com/files/{asset_id}"))
    res = RES.lower()
    want = {"albedo": ["Diffuse", "diff"], "normal": ["nor_gl"], "rough": ["Rough", "rough"],
            "ao": ["AO", "ao"], "height": ["Displacement", "disp"], "metal": ["Metal", "metal"]}
    maps = {}
    for key, names in want.items():
        for n in names:
            if n in files and res in files[n]:
                entry = files[n][res]
                fmt = "jpg" if "jpg" in entry else ("png" if "png" in entry else None)
                if fmt is None:
                    continue
                url = entry[fmt]["url"]
                maps[key] = cached(url, f"{asset_id}_{res}_{key}.{fmt}")
                break
    return maps


def load_gray(path, size):
    img = bpy.data.images.load(path)
    if tuple(img.size) != size:
        img.scale(*size)
    a = np.empty(size[0] * size[1] * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    bpy.data.images.remove(img)
    return a.reshape(size[1], size[0], 4)[..., 0]


def save_rgba(arr, path):
    h, w, _ = arr.shape
    img = bpy.data.images.new("tmp", w, h, alpha=True)
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(arr.astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def process(name, spec):
    dst = os.path.join(OUT, name)
    if os.path.exists(os.path.join(dst, "Mask.png")):
        print(f"[skip] {name}")
        return
    os.makedirs(dst, exist_ok=True)
    maps = fetch_acg(spec["id"]) if spec["src"] == "acg" else fetch_ph(spec["id"])
    if "albedo" not in maps:
        raise RuntimeError(f"{name}: no albedo in {maps}")
    shutil.copy(maps["albedo"], os.path.join(dst, "Albedo.jpg"))
    if "normal" in maps:
        shutil.copy(maps["normal"], os.path.join(dst, "Normal.jpg"))
    if "height" in maps:
        shutil.copy(maps["height"], os.path.join(dst, "Height.jpg"))
    if "opacity" in maps:
        shutil.copy(maps["opacity"], os.path.join(dst, "Opacity.jpg"))

    ref = bpy.data.images.load(maps["albedo"])
    size = tuple(ref.size)
    bpy.data.images.remove(ref)
    h, w = size[1], size[0]
    mask = np.zeros((h, w, 4), dtype=np.float32)
    mask[..., 0] = load_gray(maps["metal"], size) if "metal" in maps else 0.0
    mask[..., 1] = load_gray(maps["ao"], size) if "ao" in maps else 1.0
    mask[..., 2] = 1.0
    mask[..., 3] = 1.0 - load_gray(maps["rough"], size) if "rough" in maps else 0.5
    save_rgba(mask, os.path.join(dst, "Mask.png"))
    with open(os.path.join(dst, "source.json"), "w") as f:
        json.dump({"source": spec["src"], "id": spec["id"], "license": "CC0",
                   "maps": sorted(maps.keys())}, f, indent=1)
    print(f"[ok] {name} <- {spec['src']}:{spec['id']} maps={sorted(maps)}")


def main():
    with open(os.path.join(ROOT, "Tools", "materials.json")) as f:
        lib = json.load(f)
    failed = []
    for name, spec in lib.items():
        if name.startswith("_") or (ONLY and name not in ONLY):
            continue
        try:
            process(name, spec)
        except Exception as e:  # keep going, report at the end
            failed.append(name)
            print(f"[FAIL] {name}: {e}")
    print("FAILED:", failed)


main()
