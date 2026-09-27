"""Shared Blender helpers for SyncRooms custom models.

Conventions (match Unity after FBX export with -Z forward / Y up):
  * meters, Z up in Blender, model FRONT faces Blender -Y (-> Unity +Z)
  * pivot at bottom center unless stated otherwise (doors: hinge)
  * material slot names == Unity MaterialLibrary names (e.g. "L4_Metal", "Glass", "EmissivePanel")
  * UVs are in meters (box projection), Unity materials tile by 1/tileMeters
"""
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Custom")


# ------------------------------------------------------------------ scene
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    return m


def _new_obj(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def set_material(ob, name):
    ob.data.materials.clear()
    ob.data.materials.append(mat(name))
    return ob


def shade(ob, smooth_angle=35):
    me = ob.data
    for p in me.polygons:
        p.use_smooth = True
    try:
        me.set_sharp_from_angle(angle=math.radians(smooth_angle))
    except AttributeError:
        pass
    return ob


# ------------------------------------------------------------------ primitives
def box(name, size, loc=(0, 0, 0), material="PaintOffWhite", bevel=0.0, segments=2, rot=(0, 0, 0)):
    """Box with its BOTTOM at loc.z, centered in x/y."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, size[2] / 2)), verts=bm.verts)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=segments, affect="EDGES", profile=0.5)
    ob = _new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = rot
    set_material(ob, material)
    return shade(ob)


def cyl(name, r, h, loc=(0, 0, 0), material="Steel", verts=32, bevel=0.0, rot=(0, 0, 0), r2=None, cap=True):
    """Cylinder (or cone when r2 is given) standing on loc.z."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=verts, radius1=r, radius2=r if r2 is None else r2, depth=h)
    bmesh.ops.translate(bm, vec=Vector((0, 0, h / 2)), verts=bm.verts)
    if bevel > 0:
        rim = [e for e in bm.edges if len(e.link_faces) == 2 and abs(e.link_faces[0].normal.dot(e.link_faces[1].normal)) < 0.5]
        bmesh.ops.bevel(bm, geom=rim, offset=bevel, segments=2, affect="EDGES", profile=0.5)
    ob = _new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = rot
    set_material(ob, material)
    return shade(ob, 50)


def sphere(name, r, loc=(0, 0, 0), material="Brass", seg=24, scale=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=seg // 2, radius=r)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    ob = _new_obj(name, bm)
    ob.location = loc
    set_material(ob, material)
    return shade(ob, 80)


def torus(name, R, r, loc=(0, 0, 0), material="Brass", rot=(0, 0, 0), seg=32, mseg=10):
    bm = bmesh.new()
    prof = [(R + r * math.cos(a), 0, r * math.sin(a)) for a in [i / mseg * 2 * math.pi for i in range(mseg)]]
    vs = [bm.verts.new(p) for p in prof]
    edges = [bm.edges.new((vs[i], vs[(i + 1) % mseg])) for i in range(mseg)]
    bmesh.ops.spin(bm, geom=vs + edges, cent=(0, 0, 0), axis=(0, 0, 1), angle=2 * math.pi, steps=seg, use_merge=True)
    ob = _new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = rot
    set_material(ob, material)
    return shade(ob, 80)


def lathe(name, profile, material, loc=(0, 0, 0), steps=48, angle=2 * math.pi, rot=(0, 0, 0)):
    """Revolve a (radius, z) profile around Z. Profile goes bottom->top."""
    bm = bmesh.new()
    vs = [bm.verts.new((r, 0, z)) for r, z in profile]
    es = [bm.edges.new((vs[i], vs[i + 1])) for i in range(len(vs) - 1)]
    bmesh.ops.spin(bm, geom=vs + es, cent=(0, 0, 0), axis=(0, 0, 1), angle=angle, steps=steps, use_merge=abs(angle - 2 * math.pi) < 1e-4)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = _new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = rot
    set_material(ob, material)
    return shade(ob, 60)


def extrude_profile(name, profile, length, material, loc=(0, 0, 0), rot=(0, 0, 0), closed=False):
    """Extrude a 2D (y, z) profile along +X by `length` (moldings, trims). Profile in the YZ plane."""
    bm = bmesh.new()
    vs = [bm.verts.new((0, y, z)) for y, z in profile]
    es = [bm.edges.new((vs[i], vs[i + 1])) for i in range(len(vs) - 1)]
    if closed:
        es.append(bm.edges.new((vs[-1], vs[0])))
    res = bmesh.ops.extrude_edge_only(bm, edges=es)
    moved = [e for e in res["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=Vector((length, 0, 0)), verts=moved)
    if closed:
        bmesh.ops.contextual_create(bm, geom=vs)
        bmesh.ops.contextual_create(bm, geom=moved)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = _new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = rot
    set_material(ob, material)
    return shade(ob, 40)


def polygon_prism(name, pts2d, height, material, loc=(0, 0, 0), rot=(0, 0, 0)):
    """Extrude a closed XY polygon upward (walls with gables, cutouts...)."""
    bm = bmesh.new()
    vs = [bm.verts.new((x, y, 0)) for x, y in pts2d]
    f = bm.faces.new(vs)
    res = bmesh.ops.extrude_face_region(bm, geom=[f])
    top = [v for v in res["geom"] if isinstance(v, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=Vector((0, 0, height)), verts=top)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = _new_obj(name, bm)
    ob.location = loc
    ob.rotation_euler = rot
    set_material(ob, material)
    return shade(ob, 30)


def text(name, s, size, material, loc=(0, 0, 0), extrude=0.003, rot=(math.pi / 2, 0, 0), align="CENTER", font=None):
    """3D text converted to mesh. Default rotation stands it up facing -Y (front)."""
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = s
    cu.size = size
    cu.extrude = extrude
    cu.resolution_u = 3
    cu.align_x = align
    cu.align_y = "CENTER"
    if font and os.path.exists(font):
        cu.font = bpy.data.fonts.load(font)
    ob = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = rot
    bpy.context.view_layer.objects.active = ob
    for o in bpy.context.scene.objects:
        o.select_set(o == ob)
    bpy.ops.object.convert(target="MESH")
    ob = bpy.context.view_layer.objects.active
    set_material(ob, material)
    return ob


def boolean(target, cutter, op="DIFFERENCE"):
    mod = target.modifiers.new("bool", "BOOLEAN")
    mod.object = cutter
    mod.operation = op
    mod.solver = "EXACT"
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter)
    return target


def array_copy(ob, count, offset, name=None):
    """Duplicate `ob` count-1 times along offset vector; returns list including original."""
    out = [ob]
    for i in range(1, count):
        c = ob.copy()
        c.data = ob.data.copy()
        c.location = ob.location + Vector(offset) * i
        bpy.context.scene.collection.objects.link(c)
        out.append(c)
    return out


def dup(ob, loc=None, rot=None, scale=None):
    c = ob.copy()
    c.data = ob.data.copy()
    bpy.context.scene.collection.objects.link(c)
    if loc is not None:
        c.location = loc
    if rot is not None:
        c.rotation_euler = rot
    if scale is not None:
        c.scale = scale
    return c


def mirror_x(ob):
    return dup(ob, loc=(-ob.location.x, ob.location.y, ob.location.z),
               rot=(ob.rotation_euler.x, ob.rotation_euler.y, -ob.rotation_euler.z), scale=(-ob.scale.x, ob.scale.y, ob.scale.z))


# ------------------------------------------------------------------ UV + export
def box_uv(ob, scale=1.0):
    """World-space box projection, UV unit = 1 meter."""
    me = ob.data
    mw = ob.matrix_world
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    rot = mw.to_3x3()
    for f in bm.faces:
        n = (rot @ f.normal).normalized()
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            p = mw @ l.vert.co
            if ax == 0:
                u, v = (p.y if n.x < 0 else -p.y), p.z
            elif ax == 1:
                u, v = (-p.x if n.y > 0 else p.x), p.z
            else:
                u, v = p.x, (p.y if n.z > 0 else -p.y)
            l[uv].uv = (u * scale, v * scale)
    bm.to_mesh(me)
    bm.free()


def finalize(name, objects=None, uv=True, keep_uv=()):
    """Apply transforms, box-UV everything (except objects in keep_uv), join and export FBX."""
    objs = objects or [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if uv:
        for o in objs:
            if o.name not in keep_uv:
                box_uv(o)
    # apply transforms (handles negative scales from mirroring)
    for o in objs:
        bpy.context.view_layer.objects.active = o
        for x in bpy.context.scene.objects:
            x.select_set(x == o)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)  # Blender fixes winding for negative scales
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name
    ob.data.name = name
    # merge material slots with identical names
    bpy.ops.object.material_slot_remove_unused()
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".fbx")
    for x in bpy.context.scene.objects:
        x.select_set(x == ob)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP",
                             add_leaf_bones=False, bake_anim=False)
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    d = ob.dimensions
    print(f"[model] {name}: tris={tris} size=({d.x:.2f}, {d.y:.2f}, {d.z:.2f}) mats={[m.name for m in ob.data.materials]}")
    bpy.data.objects.remove(ob)
    for o in list(bpy.context.scene.objects):
        bpy.data.objects.remove(o)
    return path


def model(fn):
    """Decorator: run builder in a fresh scene and export under its function name (minus 'build_')."""
    def wrapper():
        reset()
        name = fn.__name__.replace("build_", "")
        try:
            result = fn()
            finalize(name, keep_uv=result if isinstance(result, (list, tuple, set)) else ())
        except Exception as e:
            import traceback
            traceback.print_exc()
            print(f"[FAIL] {name}: {e}")
    wrapper.__name__ = fn.__name__
    return wrapper


def cyl_uv(ob, z0, z1):
    """Cylindrical UVs (u = angle, v = height) - for labels wrapped around bottles."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        us = []
        for l in f.loops:
            a = (math.atan2(l.vert.co.y, l.vert.co.x) / (2 * math.pi)) % 1.0
            us.append(a)
        # fix seam wrap inside a face
        if max(us) - min(us) > 0.5:
            us = [u + 1 if u < 0.5 else u for u in us]
        for l, u in zip(f.loops, us):
            l[uv].uv = (u, (l.vert.co.z - z0) / (z1 - z0))
    bm.to_mesh(me)
    bm.free()


def render_graphic(path, width, height, elements, bg=(1, 1, 1)):
    """Render a flat 2D graphic (labels, signs) with Workbench.
    elements: list of dicts {type: 'text'|'rect'|'ellipse', ...} in a width x height (meters) canvas, origin bottom-left."""
    scene = bpy.data.scenes.new("gfx")
    bpy.context.window.scene = scene if bpy.context.window else scene
    cam_data = bpy.data.cameras.new("gcam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = max(width, height)
    cam = bpy.data.objects.new("gcam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (width / 2, height / 2, 5)
    scene.camera = cam
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT"
    scene.display.shading.color_type = "MATERIAL"
    scene.display_settings.display_device = "sRGB"
    scene.view_settings.view_transform = "Standard"
    scene.render.resolution_x = int(1024 * (width / max(width, height)))
    scene.render.resolution_y = int(1024 * (height / max(width, height)))
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("gw")
    scene.world = world
    scene.display.shading.background_type = "VIEWPORT"
    scene.display.shading.background_color = bg
    scene.render.image_settings.file_format = "PNG"

    def colmat(rgb):
        m = bpy.data.materials.new("c%.3f%.3f%.3f" % tuple(rgb))
        m.diffuse_color = (*rgb, 1)
        return m

    # background card
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    bmesh.ops.scale(bm, vec=(width, height, 1), verts=bm.verts)
    me = bpy.data.meshes.new("bgm")
    bm.to_mesh(me)
    bm.free()
    bgo = bpy.data.objects.new("bg", me)
    bgo.location = (width / 2, height / 2, 0)
    bgo.data.materials.append(colmat(bg))
    scene.collection.objects.link(bgo)

    z = 0.01
    for e in elements:
        z += 0.001
        if e["type"] == "text":
            cu = bpy.data.curves.new("t", "FONT")
            cu.body = e["text"]
            cu.size = e["size"]
            cu.align_x = e.get("align", "CENTER")
            cu.align_y = "CENTER"
            if e.get("font") and os.path.exists(e["font"]):
                cu.font = bpy.data.fonts.load(e["font"])
            ob = bpy.data.objects.new("t", cu)
            ob.data.materials.append(colmat(e["color"]))
        else:
            bm = bmesh.new()
            if e["type"] == "rect":
                bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
                bmesh.ops.scale(bm, vec=(e["w"], e["h"], 1), verts=bm.verts)
            else:
                bmesh.ops.create_circle(bm, cap_ends=True, segments=48, radius=0.5)
                bmesh.ops.scale(bm, vec=(e["w"], e["h"], 1), verts=bm.verts)
            me = bpy.data.meshes.new("r")
            bm.to_mesh(me)
            bm.free()
            ob = bpy.data.objects.new("r", me)
            ob.data.materials.append(colmat(e["color"]))
        ob.location = (e["x"], e["y"], z)
        ob.rotation_euler = (0, 0, e.get("rot", 0))
        scene.collection.objects.link(ob)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True, scene=scene.name)
    print("[gfx]", path)


def local_planar_uv(ob, scale=1.0):
    """UV = local (x, y) in meters for Z-facing faces, (x|y, z) for side faces. Call BEFORE rotating the object."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            p = l.vert.co
            if ax == 2:
                l[uv].uv = (p.x * scale, p.y * scale)
            elif ax == 1:
                l[uv].uv = (p.x * scale, p.z * scale)
            else:
                l[uv].uv = (p.y * scale, p.z * scale)
    bm.to_mesh(me)
    bm.free()


def cyl_uv_m(ob, radius):
    """Cylindrical UVs in meters (u = arc length, v = height) for tiled columns."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        if abs(f.normal.z) > 0.7:
            for l in f.loops:
                l[uv].uv = (l.vert.co.x, l.vert.co.y)
            continue
        us = [(math.atan2(l.vert.co.y, l.vert.co.x) / (2 * math.pi)) % 1.0 for l in f.loops]
        if max(us) - min(us) > 0.5:
            us = [u + 1 if u < 0.5 else u for u in us]
        for l, u in zip(f.loops, us):
            l[uv].uv = (u * 2 * math.pi * radius, l.vert.co.z)
    bm.to_mesh(me)
    bm.free()
