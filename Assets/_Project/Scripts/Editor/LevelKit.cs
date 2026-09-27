using System.Collections.Generic;
using System.IO;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>
    /// Accumulates architecture geometry (UVs in meters) per material and per spatial chunk,
    /// then emits static MeshRenderers + MeshColliders.
    /// </summary>
    public class GeoBuilder
    {
        class Buffer
        {
            public readonly List<Vector3> v = new();
            public readonly List<Vector3> n = new();
            public readonly List<Vector2> uv = new();
            public readonly List<int> t = new();
        }

        readonly Dictionary<(string mat, int cx, int cz), Buffer> _buffers = new();
        readonly Dictionary<string, SurfaceType> _surfaces = new();
        readonly float _chunk;
        public bool collide = true;

        public GeoBuilder(float chunkSize = 16f) { _chunk = chunkSize; }

        public void SetSurface(string mat, SurfaceType s) => _surfaces[mat] = s;

        Buffer Get(string mat, Vector3 at)
        {
            var key = (mat, Mathf.FloorToInt(at.x / _chunk), Mathf.FloorToInt(at.z / _chunk));
            if (!_buffers.TryGetValue(key, out var b)) _buffers[key] = b = new Buffer();
            return b;
        }

        /// <summary>Quad from 4 corners (counter-clockwise seen from the front). UVs from world position projected on the dominant axis.</summary>
        public void Quad(string mat, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float uvRot = 0f)
        {
            var normal = Vector3.Cross(b - a, d - a).normalized;
            var center = (a + b + c + d) * 0.25f;
            var buf = Get(mat, center);
            int i0 = buf.v.Count;
            foreach (var p in new[] { a, b, c, d })
            {
                buf.v.Add(p);
                buf.n.Add(normal);
                buf.uv.Add(WorldUV(p, normal, uvRot));
            }
            // Unity front faces are clockwise: (a,b,c) winds so that its front side matches cross(b-a, d-a).
            buf.t.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
        }

        static Vector2 WorldUV(Vector3 p, Vector3 n, float rot)
        {
            Vector2 uv;
            var an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (an.y >= an.x && an.y >= an.z) uv = new Vector2(p.x, n.y > 0 ? p.z : -p.z);
            else if (an.x >= an.z) uv = new Vector2(n.x > 0 ? -p.z : p.z, p.y);
            else uv = new Vector2(n.z > 0 ? p.x : -p.x, p.y);
            if (rot != 0f)
            {
                float c = Mathf.Cos(rot), s = Mathf.Sin(rot);
                uv = new Vector2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
            }
            return uv;
        }

        /// <summary>Horizontal rectangle facing up (floor) or down (ceiling).</summary>
        public void Floor(string mat, Rect r, float y, bool up = true)
        {
            var a = new Vector3(r.xMin, y, r.yMin);
            var b = new Vector3(r.xMax, y, r.yMin);
            var c = new Vector3(r.xMax, y, r.yMax);
            var d = new Vector3(r.xMin, y, r.yMax);
            if (up) Quad(mat, a, d, c, b);
            else Quad(mat, a, b, c, d);
        }

        /// <summary>Vertical wall face between p0 and p1 (XZ), facing the left side of p0->p1.</summary>
        public void WallFace(string mat, Vector2 p0, Vector2 p1, float y0, float y1)
        {
            var a = new Vector3(p0.x, y0, p0.y);
            var b = new Vector3(p1.x, y0, p1.y);
            var c = new Vector3(p1.x, y1, p1.y);
            var d = new Vector3(p0.x, y1, p0.y);
            Quad(mat, a, b, c, d);
        }

        /// <summary>Axis aligned solid box, all 6 faces (or a subset).</summary>
        public void Box(string mat, Vector3 min, Vector3 max, bool top = true, bool bottom = true, string sideMat = null)
        {
            string s = sideMat ?? mat;
            Vector3 p000 = min, p111 = max;
            var p100 = new Vector3(max.x, min.y, min.z); var p010 = new Vector3(min.x, max.y, min.z);
            var p001 = new Vector3(min.x, min.y, max.z); var p110 = new Vector3(max.x, max.y, min.z);
            var p101 = new Vector3(max.x, min.y, max.z); var p011 = new Vector3(min.x, max.y, max.z);
            Quad(s, p000, p010, p110, p100); // -Z
            Quad(s, p001, p101, p111, p011); // +Z
            Quad(s, p000, p001, p011, p010); // -X
            Quad(s, p101, p100, p110, p111); // +X
            if (top) Quad(mat, p010, p011, p111, p110);
            if (bottom) Quad(mat, p000, p100, p101, p001);
        }

        /// <summary>
        /// Straight wall of thickness t along a segment (axis aligned), with rectangular openings (doors/windows).
        /// openings: (distance along the wall from `from`, width, sill height, top height)
        /// </summary>
        public void Wall(string matA, string matB, Vector2 from, Vector2 to, float t, float y0, float y1,
            IList<(float at, float w, float sill, float top)> openings = null, string capMat = null, string baseboard = null, float baseH = 0.1f)
        {
            var dir = (to - from);
            float len = dir.magnitude;
            dir /= len;
            var nrm = new Vector2(-dir.y, dir.x); // left side
            var list = new List<(float at, float w, float sill, float top)>(openings ?? new List<(float, float, float, float)>());
            list.Sort((x, y) => x.at.CompareTo(y.at));
            float cursor = 0f;
            void Segment(float s0, float s1, float z0, float z1)
            {
                if (s1 - s0 < 0.001f || z1 - z0 < 0.001f) return;
                var a = from + dir * s0; var b = from + dir * s1;
                var aL = a + nrm * t * 0.5f; var bL = b + nrm * t * 0.5f;
                var aR = a - nrm * t * 0.5f; var bR = b - nrm * t * 0.5f;
                WallFace(matA, aL, bL, z0, z1);      // left face (normal +nrm)
                WallFace(matB, bR, aR, z0, z1);      // right face (normal -nrm)
                if (baseboard != null && z0 <= y0 + 0.001f)
                {
                    const float bd = 0.015f;
                    var aL2 = aL + nrm * bd; var bL2 = bL + nrm * bd; var aR2 = aR - nrm * bd; var bR2 = bR - nrm * bd;
                    WallFace(baseboard, aL2, bL2, y0, y0 + baseH);
                    Quad(baseboard, V(aL, y0 + baseH), V(aL2, y0 + baseH), V(bL2, y0 + baseH), V(bL, y0 + baseH));
                    WallFace(baseboard, bR2, aR2, y0, y0 + baseH);
                    Quad(baseboard, V(bR, y0 + baseH), V(bR2, y0 + baseH), V(aR2, y0 + baseH), V(aR, y0 + baseH));
                }
            }
            void Reveal(float s, float z0, float z1, bool startSide)
            {
                var p = from + dir * s;
                var l = p + nrm * t * 0.5f; var r = p - nrm * t * 0.5f;
                if (startSide) WallFace(capMat ?? matA, r, l, z0, z1);
                else WallFace(capMat ?? matA, l, r, z0, z1);
            }
            foreach (var o in list)
            {
                float s0 = o.at - o.w * 0.5f, s1 = o.at + o.w * 0.5f;
                Segment(cursor, s0, y0, y1);
                Segment(s0, s1, y0, o.sill);         // below sill
                Segment(s0, s1, o.top, y1);          // above lintel
                Reveal(s0, o.sill, o.top, false);
                Reveal(s1, o.sill, o.top, true);
                // sill & head reveals
                var a = from + dir * s0; var b = from + dir * s1;
                if (o.sill > y0 + 0.001f)
                    Quad(capMat ?? matA, V(a + nrm * t * .5f, o.sill), V(b + nrm * t * .5f, o.sill), V(b - nrm * t * .5f, o.sill), V(a - nrm * t * .5f, o.sill)); // faces up
                Quad(capMat ?? matA, V(a - nrm * t * .5f, o.top), V(b - nrm * t * .5f, o.top), V(b + nrm * t * .5f, o.top), V(a + nrm * t * .5f, o.top));        // faces down
                cursor = s1;
            }
            Segment(cursor, len, y0, y1);
        }

        static Vector3 V(Vector2 p, float y) => new(p.x, y, p.y);

        public struct Opening
        {
            public float at, w, sill, top;
            public Opening(float at, float w, float top, float sill = 0f) { this.at = at; this.w = w; this.top = top; this.sill = sill; }
        }

        /// <summary>
        /// Inward-facing walls of a rectangular space (0=S,1=N,2=W,3=E). Openings: `at` measured from xMin (S/N) or zMin (W/E).
        /// Reveals extend t/2 outward so two neighbouring spaces separated by a gap of t close the wall thickness seamlessly.
        /// </summary>
        public void InnerWalls(string mat, Rect r, float y0, float y1, float t, List<Opening>[] ops, string revealMat = null)
        {
            for (int side = 0; side < 4; side++)
            {
                Vector2 p0, p1; bool flip;
                switch (side)
                {
                    case 0: p0 = new Vector2(r.xMin, r.yMin); p1 = new Vector2(r.xMax, r.yMin); flip = false; break;
                    case 1: p0 = new Vector2(r.xMax, r.yMax); p1 = new Vector2(r.xMin, r.yMax); flip = true; break;
                    case 2: p0 = new Vector2(r.xMin, r.yMax); p1 = new Vector2(r.xMin, r.yMin); flip = true; break;
                    default: p0 = new Vector2(r.xMax, r.yMin); p1 = new Vector2(r.xMax, r.yMax); flip = false; break;
                }
                float len = (p1 - p0).magnitude;
                var dir = (p1 - p0) / len;
                var nrm = new Vector2(-dir.y, dir.x);
                var hole = new List<Opening>();
                if (ops != null && ops[side] != null)
                    foreach (var o in ops[side]) hole.Add(new Opening(flip ? len - o.at : o.at, o.w, Mathf.Min(o.top, y1), o.sill));
                hole.Sort((a, b) => a.at.CompareTo(b.at));
                float cursor = 0;
                void Seg(float s0, float s1, float z0, float z1)
                {
                    if (s1 - s0 < 0.001f || z1 - z0 < 0.001f) return;
                    WallFace(mat, p0 + dir * s0, p0 + dir * s1, z0, z1);
                }
                string rm = revealMat ?? mat;
                float h = t * 0.5f;
                foreach (var o in hole)
                {
                    float s0 = o.at - o.w * 0.5f, s1 = o.at + o.w * 0.5f;
                    Seg(cursor, s0, y0, y1);
                    Seg(s0, s1, y0, o.sill);
                    Seg(s0, s1, o.top, y1);
                    var a = p0 + dir * s0; var b = p0 + dir * s1;
                    WallFace(rm, a, a - nrm * h, o.sill, o.top);    // faces +dir
                    WallFace(rm, b - nrm * h, b, o.sill, o.top);    // faces -dir
                    if (o.sill > y0 + 0.001f) Quad(rm, V(a, o.sill), V(b, o.sill), V(b - nrm * h, o.sill), V(a - nrm * h, o.sill));
                    if (o.top < y1 - 0.001f) Quad(rm, V(a, o.top), V(a - nrm * h, o.top), V(b - nrm * h, o.top), V(b, o.top));
                    cursor = s1;
                }
                Seg(cursor, len, y0, y1);
            }
        }

        /// <summary>Floor + ceiling + inward walls of a space.</summary>
        public void Space(Rect r, float y0, float h, float t, string floorMat, string wallMat, string ceilMat, List<Opening>[] ops, string revealMat = null)
        {
            Floor(floorMat, r, y0);
            if (ceilMat != null) Floor(ceilMat, r, y0 + h, false);
            InnerWalls(wallMat, r, y0, y0 + h, t, ops, revealMat);
        }

        public void Emit(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.isStatic = true;
            foreach (var kv in _buffers)
            {
                var b = kv.Value;
                if (b.v.Count == 0) continue;
                var mesh = new Mesh { name = $"{name}_{kv.Key.mat}_{kv.Key.cx}_{kv.Key.cz}" };
                mesh.indexFormat = b.v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(b.v);
                mesh.SetNormals(b.n);
                mesh.SetUVs(0, b.uv);
                mesh.SetTriangles(b.t, 0);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                var go = new GameObject(mesh.name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = MaterialLibrary.Get(kv.Key.mat);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; // single-sided shells must still block light
                var mat = mr.sharedMaterial;
                bool transparent = mat && mat.GetFloat("_SurfaceType") > 0.5f;
                if (collide && !transparent)
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                }
                if (_surfaces.TryGetValue(kv.Key.mat, out var st)) go.AddComponent<SurfaceTag>().type = st;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);
                LevelKit.SaveMesh(mesh);
            }
            _buffers.Clear();
        }
    }

    /// <summary>Scene scaffolding shared by all level builders.</summary>
    public static class LevelKit
    {
        public const string SceneDir = "Assets/_Project/Scenes";
        public const string GenDir = "Assets/_Project/Generated";
        static string _currentLevel;

        public static UnityEngine.SceneManagement.Scene NewScene(string name)
        {
            _currentLevel = name;
            var dir = $"{GenDir}/{name}";
            if (Directory.Exists(dir)) AssetDatabase.DeleteAsset(dir);
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static void SaveMesh(Mesh m)
        {
            var path = $"{GenDir}/{_currentLevel}/{m.name}.asset";
            AssetDatabase.CreateAsset(m, AssetDatabase.GenerateUniqueAssetPath(path));
        }

        public static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, $"{SceneDir}/{name}.unity");
            AssetDatabase.SaveAssets();
        }

        public static T SaveAsset<T>(T obj, string name) where T : Object
        {
            var path = $"{GenDir}/{_currentLevel}/{name}.asset";
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        public static GameObject Group(string name, Transform parent = null)
        {
            var g = new GameObject(name);
            if (parent) g.transform.SetParent(parent, false);
            return g;
        }

        public static GameObject Place(string prefab, Vector3 pos, float yaw, Transform parent, float scale = 1f, bool isStatic = true)
        {
            var p = ModelLibrary.Get(prefab);
            if (!p) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            if (isStatic)
                foreach (var t in go.GetComponentsInChildren<Transform>())
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        public static GameObject PlaceItem(string itemId, Vector3 pos, float yaw, Transform parent)
        {
            var def = ItemLibrary.Get(itemId);
            if (!def || !def.worldPrefab) { Debug.LogWarning("missing item " + itemId); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(def.worldPrefab, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            return go;
        }

        // ------------------------------------------------------------------ lights
        public static Light PointLight(Transform parent, Vector3 pos, float lumen, Color color, float range, bool shadows, float temperature = -1)
        {
            var go = new GameObject("Point Light");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var hd = go.AddHDLight(LightType.Point);
            var l = go.GetComponent<Light>();
            l.lightUnit = LightUnit.Lumen;
            l.intensity = lumen;
            SetColor(l, color, temperature);
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (shadows) hd.SetShadowResolution(512);
            hd.shadowFadeDistance = 25f;
            hd.fadeDistance = 60f;
            hd.volumetricDimmer = 0.6f;
            return l;
        }

        public static Light SpotLight(Transform parent, Vector3 pos, Quaternion rot, float lumen, Color color, float range, float angle, bool shadows, float temperature = -1)
        {
            var go = new GameObject("Spot Light");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            var hd = go.AddHDLight(LightType.Spot);
            var l = go.GetComponent<Light>();
            l.lightUnit = LightUnit.Lumen;
            l.intensity = lumen;
            SetColor(l, color, temperature);
            l.range = range;
            l.spotAngle = angle;
            l.innerSpotAngle = angle * 0.55f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (shadows) hd.SetShadowResolution(1024);
            return l;
        }

        public static Light RectLight(Transform parent, Vector3 pos, Quaternion rot, Vector2 size, float lumen, Color color, float range, bool shadows, float temperature = -1)
        {
            var go = new GameObject("Area Light");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            var hd = go.AddHDLight(LightType.Rectangle);
            var l = go.GetComponent<Light>();
            l.lightUnit = LightUnit.Lumen;
            l.intensity = lumen;
            l.areaSize = size;
            SetColor(l, color, temperature);
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (shadows) hd.SetShadowResolution(512);
            hd.fadeDistance = 45f;
            hd.shadowFadeDistance = 20f;
            hd.volumetricDimmer = 0.4f;
            return l;
        }

        public static Light Sun(Transform parent, Vector3 euler, float lux, float temperature)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.eulerAngles = euler;
            var hd = go.AddHDLight(LightType.Directional);
            var l = go.GetComponent<Light>();
            l.lightUnit = LightUnit.Lux;
            l.intensity = lux;
            l.useColorTemperature = true;
            l.colorTemperature = temperature;
            l.color = Color.white;
            l.shadows = LightShadows.Soft;
            hd.SetShadowResolution(2048);
            hd.angularDiameter = 0.53f;
            return l;
        }

        static void SetColor(Light l, Color c, float temperature)
        {
            if (temperature > 0)
            {
                l.useColorTemperature = true;
                l.colorTemperature = temperature;
                l.color = c;
            }
            else l.color = c;
        }

        // ------------------------------------------------------------------ probes
        public static HDAdditionalReflectionData ReflectionProbe(Transform parent, Vector3 center, Vector3 size, bool realtime = true, int priority = 0)
        {
            var go = new GameObject("Reflection Probe");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var rp = go.AddComponent<ReflectionProbe>();
            var hd = go.AddComponent<HDAdditionalReflectionData>();
            hd.influenceVolume.shape = InfluenceShape.Box;
            hd.influenceVolume.boxSize = size;
            hd.influenceVolume.boxBlendDistancePositive = Vector3.one * 1f;
            hd.influenceVolume.boxBlendDistanceNegative = Vector3.one * 1f;
            hd.proxyVolume = null;
            hd.mode = realtime ? ProbeSettings.Mode.Realtime : ProbeSettings.Mode.Baked;
            hd.realtimeMode = ProbeSettings.RealtimeMode.OnEnable;
            return hd;
        }

        public static PlanarReflectionProbe PlanarProbe(Transform parent, Vector3 center, Vector2 size)
        {
            var go = new GameObject("Planar Reflection");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var p = go.AddComponent<PlanarReflectionProbe>();
            p.influenceVolume.shape = InfluenceShape.Box;
            p.influenceVolume.boxSize = new Vector3(size.x, 1.5f, size.y);
            p.influenceVolume.boxBlendDistancePositive = new Vector3(1, 0.5f, 1);
            p.influenceVolume.boxBlendDistanceNegative = new Vector3(1, 0.5f, 1);
            p.mode = ProbeSettings.Mode.Realtime;
            p.realtimeMode = ProbeSettings.RealtimeMode.EveryFrame;
            return p;
        }

        // ------------------------------------------------------------------ volume profile
        public class Look
        {
            public float expMin = 6, expMax = 11, expComp = 0;
            public bool fixedExposure;
            public float fixedEV = 9;
            public Color fogAlbedo = Color.white;
            public float fogMFP = 60f, fogBaseHeight = 0f, fogMaxHeight = 30f;
            public bool volumetricFog = true;
            public float bloom = 0.25f, vignette = 0.2f, grain = 0.15f, saturation = 0, contrast = 8;
            public float temperature = 0, tint = 0;
            public bool ssgi = true;
            public int skyType = (int)SkyType.PhysicallyBased;
            public Cubemap hdri;
            public float hdriExposure = 12;
            public bool clouds;
            public float ambientDimmer = 1f;
            public Color gradientTop = new(0.1f, 0.1f, 0.1f), gradientMid = new(0.08f, 0.08f, 0.08f), gradientBottom = new(0.05f, 0.05f, 0.05f);
            public float shadowDistance = 60f;
            public float chroma = 0.05f;
            public float skyExposure = 0f;
            public bool fog = true;
        }

        public static Volume GlobalVolume(Transform parent, string name, Look look)
        {
            var go = new GameObject("Global Volume");
            go.transform.SetParent(parent, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            SaveAsset(p, name + "_Profile");

            var vis = p.Add<VisualEnvironment>(true);
            vis.skyType.Override(look.skyType);
            vis.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            vis.cloudType.Override(0);
            if (look.skyType == (int)SkyType.PhysicallyBased)
            {
                var pbs = p.Add<PhysicallyBasedSky>(true);
                pbs.type.Override(PhysicallyBasedSkyModel.EarthAdvanced);
                if (look.skyExposure != 0) pbs.exposure.Override(look.skyExposure);
            }
            else if (look.skyType == (int)SkyType.HDRI && look.hdri)
            {
                var hs = p.Add<HDRISky>(true);
                hs.hdriSky.Override(look.hdri);
                hs.exposure.Override(look.hdriExposure);
                hs.skyIntensityMode.Override(SkyIntensityMode.Exposure);
            }
            else
            {
                vis.skyType.Override((int)SkyType.Gradient);
                var g = p.Add<GradientSky>(true);
                g.top.Override(look.gradientTop);
                g.middle.Override(look.gradientMid);
                g.bottom.Override(look.gradientBottom);
                g.multiplier.Override(1f);
                if (look.skyExposure != 0) g.exposure.Override(look.skyExposure);
            }
            if (look.clouds)
            {
                var vc = p.Add<VolumetricClouds>(true);
                vc.enable.Override(true);
                vc.cloudPreset = VolumetricClouds.CloudPresets.Sparse;
            }

            var exp = p.Add<Exposure>(true);
            if (look.fixedExposure)
            {
                exp.mode.Override(ExposureMode.Fixed);
                exp.fixedExposure.Override(look.fixedEV);
            }
            else
            {
                exp.mode.Override(ExposureMode.AutomaticHistogram);
                exp.limitMin.Override(look.expMin);
                exp.limitMax.Override(look.expMax);
                exp.compensation.Override(look.expComp);
                exp.adaptationMode.Override(AdaptationMode.Progressive);
                exp.adaptationSpeedDarkToLight.Override(2.5f);
                exp.adaptationSpeedLightToDark.Override(1.2f);
            }

            var fog = p.Add<Fog>(true);
            fog.enabled.Override(look.fog);
            fog.meanFreePath.Override(look.fogMFP);
            fog.baseHeight.Override(look.fogBaseHeight);
            fog.maximumHeight.Override(look.fogMaxHeight);
            fog.albedo.Override(look.fogAlbedo);
            fog.enableVolumetricFog.Override(look.volumetricFog);
            fog.denoisingMode.Override(FogDenoisingMode.Reprojection);
            fog.quality.Override(1);

            var tm = p.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);
            var bloom = p.Add<Bloom>(true);
            bloom.intensity.Override(look.bloom);
            bloom.scatter.Override(0.65f);
            bloom.quality.Override(2);
            var vig = p.Add<Vignette>(true);
            vig.intensity.Override(look.vignette);
            vig.smoothness.Override(0.45f);
            var fg = p.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Thin1);
            fg.intensity.Override(look.grain);
            var ca = p.Add<ChromaticAberration>(true);
            ca.intensity.Override(look.chroma);
            var col = p.Add<ColorAdjustments>(true);
            col.saturation.Override(look.saturation);
            col.contrast.Override(look.contrast);
            var wb = p.Add<WhiteBalance>(true);
            wb.temperature.Override(look.temperature);
            wb.tint.Override(look.tint);

            var ssr = p.Add<ScreenSpaceReflection>(true);
            ssr.enabled.Override(true);
            ssr.enabledTransparent.Override(true);
            ssr.quality.Override(1);
            ssr.minSmoothness = 0.35f;
            ssr.smoothnessFadeStart = 0.4f;
            ssr.reflectSky.Override(true);
            ssr.usedAlgorithm.Override(ScreenSpaceReflectionAlgorithm.PBRAccumulation);
            var ao = p.Add<ScreenSpaceAmbientOcclusion>(true);
            ao.intensity.Override(1.2f);
            ao.radius.Override(1.5f);
            ao.quality.Override(1);
            if (look.ssgi)
            {
                var gi = p.Add<GlobalIllumination>(true);
                gi.enable.Override(true);
                gi.quality.Override(1);
                gi.fullResolutionSS.Override(false);
            }
            var cs = p.Add<ContactShadows>(true);
            cs.enable.Override(true);
            cs.length.Override(0.25f);
            var sh = p.Add<HDShadowSettings>(true);
            sh.maxShadowDistance.Override(look.shadowDistance);
            var ilc = p.Add<IndirectLightingController>(true);
            ilc.indirectDiffuseLightingMultiplier.Override(look.ambientDimmer);
            var ms = p.Add<MicroShadowing>(true);
            ms.enable.Override(true);

            // components created in code must be stored inside the profile asset, otherwise they are lost on reload
            foreach (var c in p.components)
                if (!AssetDatabase.Contains(c)) { c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(c, p); }
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            vol.sharedProfile = p;
            return vol;
        }

        // ------------------------------------------------------------------ player
        public static GameObject Player(Transform parent, Vector3 pos, float yaw)
        {
            var root = new GameObject("Player");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            root.layer = 2; // ignore raycast (interaction casts start inside the capsule)
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.03f;

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0, 1.68f, 0);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = 70f;
            var hd = camGo.AddComponent<HDAdditionalCameraData>();
            EditorCapture.ConfigureCamera(hd);
            camGo.AddComponent<AudioListener>();

            var flGo = new GameObject("Flashlight");
            flGo.transform.SetParent(camGo.transform, false);
            flGo.transform.localPosition = new Vector3(0.18f, -0.22f, 0.15f);
            flGo.transform.localRotation = Quaternion.Euler(1.5f, -2f, 0);
            var fhd = flGo.AddHDLight(LightType.Spot);
            var fl = flGo.GetComponent<Light>();
            fl.lightUnit = LightUnit.Lumen;
            fl.intensity = 900f;
            fl.useColorTemperature = true;
            fl.colorTemperature = 4300f;
            fl.range = 30f;
            fl.spotAngle = 58f;
            fl.innerSpotAngle = 18f;
            fl.shadows = LightShadows.Soft;
            fhd.SetShadowResolution(1024);
            fhd.volumetricDimmer = 1.5f;
            fl.enabled = false;
            var flash = flGo.AddComponent<Flashlight>();
            flash.spot = fl;

            var hb = camGo.AddComponent<AudioSource>();
            hb.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Resources/Audio/SFX/sfx_heartbeat.mp3");
            hb.loop = true; hb.playOnAwake = false; hb.volume = 0f; hb.spatialBlend = 0f;
            var uw = camGo.AddComponent<AudioSource>();
            uw.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Resources/Audio/SFX/sfx_underwater.mp3");
            uw.loop = true; uw.playOnAwake = false; uw.volume = 0.8f; uw.spatialBlend = 0f;

            var sanityGo = new GameObject("Sanity Volume");
            sanityGo.transform.SetParent(root.transform, false);
            var sv = sanityGo.AddComponent<Volume>();
            sv.isGlobal = true;
            sv.priority = 50;
            sv.weight = 0;

            var fpc = root.AddComponent<FirstPersonController>();
            fpc.cameraPivot = pivot;
            fpc.playerCamera = cam;
            var inter = root.AddComponent<PlayerInteractor>();
            inter.cam = cam;
            inter.mask = ~(1 << 2);
            root.AddComponent<PlayerAvatar>();
            var vit = root.AddComponent<PlayerVitals>();
            vit.sanityVolume = sv;
            vit.heartbeat = hb;
            vit.underwater = uw;
            return root;
        }

        public static LevelSettings Settings(Transform parent, Transform spawn, float drain, float darkDrain, string[] loops, float volume)
        {
            var go = new GameObject("Level");
            go.transform.SetParent(parent, false);
            var ls = go.AddComponent<LevelSettings>();
            ls.spawnPoint = spawn;
            ls.sanityDrainPerMinute = drain;
            ls.darknessDrainPerMinute = darkDrain;
            ls.ambientLoops = loops;
            ls.ambientVolume = volume;
            go.AddComponent<LevelAmbience>();
            return ls;
        }

        public static AudioSource Audio3D(Transform parent, Vector3 pos, string resPath, float volume, float maxDist, bool loop = true)
        {
            var go = new GameObject("Audio " + Path.GetFileName(resPath));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Resources/Audio/{resPath}.mp3");
            src.loop = loop;
            src.playOnAwake = loop;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = maxDist;
            src.dopplerLevel = 0;
            return src;
        }
    }
}
