"""Download Poly Haven models (glTF), clean them up in Blender, export FBX for Unity.

  blender -b --factory-startup -P fetch_models.py -- [only=id1,id2] [maxtris=120000]

Output: Assets/_Project/Art/Models/PolyHaven/<Group>/<id>/
  <id>.fbx                 mesh (meters, Y-up conversion baked by Unity importer)
  <id>.materials.json      material name -> texture files + alpha mode (read by Unity MaterialBuilder)
  <mat>_Albedo.jpg / _Normal.jpg / _Mask.png
"""
import json
import os
import shutil
import sys
import urllib.request

import bpy
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "PolyHaven")
CACHE = os.path.join(ROOT, "Tools", "cache", "models")
UA = {"User-Agent": "SyncRooms-Liminal-AssetFetcher/1.0"}

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opts = dict(a.split("=", 1) for a in args if "=" in a)
ONLY = set(opts["only"].split(",")) if "only" in opts else None
MAX_TRIS = int(opts.get("maxtris", "120000"))


def http(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=300) as r:
        return r.read()


def download(url, path):
    if not os.path.exists(path):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        data = http(url)
        with open(path, "wb") as f:
            f.write(data)


def img_array(path):
    img = bpy.data.images.load(path)
    w, h = img.size
    a = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    bpy.data.images.remove(img)
    return a.reshape(h, w, 4)


def save_png(arr, path):
    h, w, _ = arr.shape
    img = bpy.data.images.new("tmp", w, h, alpha=True)
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(arr.astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def tex_path(gltf, base, info):
    if not info:
        return None
    src = gltf["textures"][info["index"]]["source"]
    return os.path.join(base, gltf["images"][src]["uri"])


def build_textures(gltf_path, dst, asset_id):
    """Parse glTF materials, copy albedo/normal and pack HDRP mask maps."""
    base = os.path.dirname(gltf_path)
    with open(gltf_path) as f:
        gltf = json.load(f)
    out = {}
    for m in gltf.get("materials", []):
        name = m["name"]
        safe = "".join(c if c.isalnum() or c in "_-" else "_" for c in name)
        pbr = m.get("pbrMetallicRoughness", {})
        entry = {"alphaMode": m.get("alphaMode", "OPAQUE"), "alphaCutoff": m.get("alphaCutoff", 0.5),
                 "doubleSided": m.get("doubleSided", False),
                 "baseColorFactor": pbr.get("baseColorFactor", [1, 1, 1, 1]),
                 "metallicFactor": pbr.get("metallicFactor", 1.0),
                 "roughnessFactor": pbr.get("roughnessFactor", 1.0)}
        ext = m.get("extensions", {})
        if "KHR_materials_transmission" in ext:
            entry["transmission"] = ext["KHR_materials_transmission"].get("transmissionFactor", 1.0)
        if "KHR_materials_emissive_strength" in ext or m.get("emissiveFactor"):
            entry["emissive"] = m.get("emissiveFactor", [0, 0, 0])
        alb = tex_path(gltf, base, pbr.get("baseColorTexture"))
        if alb:
            ext_ = os.path.splitext(alb)[1].lower()
            fn = f"{safe}_Albedo{ext_}"
            shutil.copy(alb, os.path.join(dst, fn))
            entry["albedo"] = fn
        nor = tex_path(gltf, base, m.get("normalTexture"))
        if nor:
            fn = f"{safe}_Normal{os.path.splitext(nor)[1].lower()}"
            shutil.copy(nor, os.path.join(dst, fn))
            entry["normal"] = fn
        mr = tex_path(gltf, base, pbr.get("metallicRoughnessTexture"))
        occ = tex_path(gltf, base, m.get("occlusionTexture"))
        if mr or occ:
            ref = img_array(mr or occ)
            h, w, _ = ref.shape
            mask = np.ones((h, w, 4), dtype=np.float32)
            if mr:
                mra = ref if mr else img_array(mr)
                mask[..., 0] = mra[..., 2] * entry["metallicFactor"]
                mask[..., 3] = 1.0 - mra[..., 1] * entry["roughnessFactor"]
            else:
                mask[..., 0] = entry["metallicFactor"]
                mask[..., 3] = 1.0 - entry["roughnessFactor"]
            if occ:
                o = img_array(occ) if occ != mr else ref
                if o.shape[:2] == (h, w):
                    mask[..., 1] = o[..., 0]
            fn = f"{safe}_Mask.png"
            save_png(mask, os.path.join(dst, fn))
            entry["mask"] = fn
        out[name] = entry
    return out


def tri_count():
    n = 0
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            n += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return n


def process(group, asset_id, res):
    dst = os.path.join(OUT, group, asset_id)
    fbx = os.path.join(dst, f"{asset_id}.fbx")
    if os.path.exists(fbx):
        print(f"[skip] {asset_id}")
        return
    files = json.loads(http(f"https://api.polyhaven.com/files/{asset_id}"))
    avail = files["gltf"]
    if res not in avail:
        res = sorted(avail.keys())[0]
    g = avail[res]["gltf"]
    cdir = os.path.join(CACHE, asset_id, res)
    gpath = os.path.join(cdir, os.path.basename(g["url"]))
    download(g["url"], gpath)
    for rel, inc in g.get("include", {}).items():
        download(inc["url"], os.path.join(cdir, rel))

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=gpath)
    # Flatten hierarchy, apply transforms, join into a single mesh (keeps material slots).
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in bpy.context.scene.objects:
        o.select_set(o.type == "MESH")
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = asset_id
    for o in list(bpy.context.scene.objects):
        if o != obj:
            bpy.data.objects.remove(o)
    tris = tri_count()
    if tris > MAX_TRIS:
        mod = obj.modifiers.new("dec", "DECIMATE")
        mod.ratio = MAX_TRIS / tris
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
        print(f"   decimated {tris} -> {tri_count()}")
    # Pivot at bottom center so props sit on the floor.
    import mathutils
    bb = [obj.matrix_world @ mathutils.Vector(c) for c in obj.bound_box]
    minz = min(v.z for v in bb)
    cx = (min(v.x for v in bb) + max(v.x for v in bb)) / 2
    cy = (min(v.y for v in bb) + max(v.y for v in bb)) / 2
    obj.data.transform(mathutils.Matrix.Translation((-cx, -cy, -minz)))
    obj.location = (0, 0, 0)

    os.makedirs(dst, exist_ok=True)
    mats = build_textures(gpath, dst, asset_id)
    dims = obj.dimensions
    meta = {"id": asset_id, "group": group, "source": "Poly Haven", "license": "CC0",
            "url": f"https://polyhaven.com/a/{asset_id}", "tris": tri_count(),
            "size": [dims.x, dims.z, dims.y], "materials": mats}
    with open(os.path.join(dst, f"{asset_id}.materials.json"), "w") as f:
        json.dump(meta, f, indent=1)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP",
                             embed_textures=False, add_leaf_bones=False, bake_anim=False)
    print(f"[ok] {group}/{asset_id} tris={meta['tris']} size={[round(v, 2) for v in meta['size']]}")


def main():
    with open(os.path.join(ROOT, "Tools", "models.json")) as f:
        lib = json.load(f)
    failed = []
    for group, items in lib.items():
        if group.startswith("_"):
            continue
        for asset_id, res in items.items():
            if ONLY and asset_id not in ONLY:
                continue
            try:
                process(group, asset_id, res)
            except Exception as e:
                import traceback
                traceback.print_exc()
                failed.append(asset_id)
                print(f"[FAIL] {asset_id}: {e}")
    print("FAILED:", failed)


main()
