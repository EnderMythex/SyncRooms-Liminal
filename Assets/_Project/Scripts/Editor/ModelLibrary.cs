using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>
    /// Turns imported FBX files into ready-to-place prefabs:
    ///  - Poly Haven models: HDRP materials from their *.materials.json sidecar
    ///  - Custom Blender models: material slots named after MaterialLibrary entries
    /// Prefabs land in Assets/_Project/Prefabs/{PolyHaven|Custom}/...
    /// </summary>
    public static class ModelLibrary
    {
        public const string PHDir = "Assets/_Project/Art/Models/PolyHaven";
        public const string CustomDir = "Assets/_Project/Art/Models/Custom";
        public const string PrefabDir = "Assets/_Project/Prefabs";

        [Serializable] class MatEntry
        {
            public string alphaMode = "OPAQUE";
            public float alphaCutoff = 0.5f;
            public bool doubleSided;
            public float[] baseColorFactor;
            public float metallicFactor = 1, roughnessFactor = 1;
            public float transmission = -1;
            public float[] emissive;
            public string albedo, normal, mask;
        }

        static readonly HashSet<string> NoCollider = new()
        {
            "grass_medium_01", "grass_medium_02", "dandelion_01", "shrub_01", "shrub_02", "mounted_fluorescent_lights",
            "Chandelier_01", "Chandelier_02", "caged_hanging_light", "fire_alarm", "wall_clock", "fancy_picture_frame_01",
            "fancy_picture_frame_02", "ornate_mirror_01", "industrial_wall_sconce", "industrial_caged_sconce", "office_notepads",
            "stationery_supplies", "vintage_stapler", "clipboard", "binder_notebook", "tea_set_01", "chess_set", "wine_bottles_01",
            "mantel_clock_01", "brass_candleholders", "book_encyclopedia_set_01", "modular_electric_cables",
        };
        static readonly HashSet<string> Trees = new() { "island_tree_01", "jacaranda_tree", "tree_small_02" };
        static readonly HashSet<string> Foliage = new() { "grass_medium_01", "grass_medium_02", "dandelion_01", "shrub_01", "shrub_02", "island_tree_01", "jacaranda_tree", "tree_small_02", "potted_plant_02" };

        [MenuItem("SyncRooms/3. Build Model Prefabs")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            if (Directory.Exists(PHDir))
                foreach (var json in Directory.GetFiles(PHDir, "*.materials.json", SearchOption.AllDirectories))
                    BuildPolyHaven(json.Replace('\\', '/'));
            if (Directory.Exists(CustomDir))
                foreach (var fbx in Directory.GetFiles(CustomDir, "*.fbx", SearchOption.AllDirectories))
                    BuildCustom(fbx.Replace('\\', '/'));
            AssetDatabase.SaveAssets();
            Debug.Log("[SyncRooms] Model prefabs built.");
        }

        public static GameObject Get(string id)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/PolyHaven/{id}.prefab");
            if (!p) p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Custom/{id}.prefab");
            if (!p) Debug.LogWarning("[SyncRooms] Missing prefab " + id);
            return p;
        }

        // ------------------------------------------------------------------ Poly Haven
        static void BuildPolyHaven(string jsonPath)
        {
            string dir = Path.GetDirectoryName(jsonPath).Replace('\\', '/');
            string id = Path.GetFileName(dir);
            string fbx = $"{dir}/{id}.fbx";
            if (!File.Exists(fbx)) return;

            // Parse the "materials" object manually (JsonUtility cannot read dictionaries).
            var mats = ParseMaterials(File.ReadAllText(jsonPath));
            var built = new Dictionary<string, Material>();
            bool foliage = Foliage.Contains(id);
            foreach (var kv in mats)
            {
                var e = kv.Value;
                string safe = new string(kv.Key.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
                var m = MaterialLibrary.NewLit($"{dir}/{safe}.mat");
                var tint = e.baseColorFactor != null && e.baseColorFactor.Length == 4
                    ? new Color(e.baseColorFactor[0], e.baseColorFactor[1], e.baseColorFactor[2], e.baseColorFactor[3]) : Color.white;
                var alb = e.albedo != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{e.albedo}") : null;
                var nor = e.normal != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{e.normal}") : null;
                var msk = e.mask != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{e.mask}") : null;
                MaterialLibrary.ApplyMaps(m, alb, nor, msk, null, tint);
                if (!msk)
                {
                    m.SetFloat("_Metallic", e.metallicFactor);
                    m.SetFloat("_Smoothness", 1f - e.roughnessFactor);
                }

                bool glass = e.transmission > 0 || e.alphaMode == "BLEND";
                if (glass)
                {
                    HDMaterial.SetSurfaceType(m, true);
                    m.SetFloat("_BlendMode", 0);
                    m.SetFloat("_EnableFogOnTransparent", 1);
                    m.SetFloat("_ReceivesSSRTransparent", 1);
                    if (e.transmission > 0)
                    {
                        var c = m.GetColor("_BaseColor");
                        c.a = Mathf.Min(c.a, 0.18f);
                        m.SetColor("_BaseColor", c);
                        m.SetFloat("_Smoothness", 0.97f);
                        m.SetFloat("_SmoothnessRemapMin", 0.9f);
                    }
                }
                else if (e.alphaMode == "MASK" || foliage)
                {
                    HDMaterial.SetAlphaClipping(m, true);
                    HDMaterial.SetAlphaCutoff(m, e.alphaMode == "MASK" ? e.alphaCutoff : 0.5f);
                }
                if (e.doubleSided || foliage)
                {
                    m.SetFloat("_DoubleSidedEnable", 1);
                    m.SetFloat("_DoubleSidedNormalMode", foliage ? 1 : 2); // mirror for leaves, none otherwise
                    m.SetFloat("_CullMode", 0);
                    m.SetFloat("_CullModeForward", 0);
                }
                if (e.emissive != null && e.emissive.Length == 3 && (e.emissive[0] + e.emissive[1] + e.emissive[2]) > 0.01f)
                {
                    HDMaterial.SetUseEmissiveIntensity(m, true);
                    HDMaterial.SetEmissiveColor(m, new Color(e.emissive[0], e.emissive[1], e.emissive[2]));
                    HDMaterial.SetEmissiveIntensity(m, 8f, EmissiveIntensityUnit.EV100);
                }
                HDMaterial.ValidateMaterial(m);
                EditorUtility.SetDirty(m);
                built[kv.Key] = m;
            }
            Remap(fbx, name => built.TryGetValue(name, out var m) ? m : null);
            MakePrefab(fbx, $"{PrefabDir}/PolyHaven/{id}.prefab", id);
        }

        static Dictionary<string, MatEntry> ParseMaterials(string json)
        {
            // Sidecar format: { ..., "materials": { "<name>": { ... }, ... } }
            var result = new Dictionary<string, MatEntry>();
            int i = json.IndexOf("\"materials\"", StringComparison.Ordinal);
            if (i < 0) return result;
            i = json.IndexOf('{', i) + 1;
            while (true)
            {
                int ks = json.IndexOf('"', i);
                int close = json.IndexOf('}', i);
                if (ks < 0 || (close >= 0 && close < ks)) break;
                int ke = json.IndexOf('"', ks + 1);
                string key = json.Substring(ks + 1, ke - ks - 1);
                int os = json.IndexOf('{', ke);
                int depth = 0, oe = os;
                for (; oe < json.Length; oe++)
                {
                    if (json[oe] == '{') depth++;
                    else if (json[oe] == '}' && --depth == 0) break;
                }
                result[key] = JsonUtility.FromJson<MatEntry>(json.Substring(os, oe - os + 1));
                i = oe + 1;
            }
            return result;
        }

        // ------------------------------------------------------------------ Custom Blender
        static void BuildCustom(string fbx)
        {
            string name = Path.GetFileNameWithoutExtension(fbx);
            string dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
            // Custom per-model textured materials, if any: <dir>/<name>_<Mat>_Albedo.png etc.
            Remap(fbx, matName =>
            {
                string clean = StripSuffix(matName);
                var alb = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{clean}_Albedo.png");
                if (alb)
                {
                    var m = MaterialLibrary.NewLit($"{dir}/{clean}.mat");
                    MaterialLibrary.ApplyMaps(m, alb, AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{clean}_Normal.png"), null, null, Color.white);
                    m.SetFloat("_Smoothness", 0.45f);
                    HDMaterial.ValidateMaterial(m);
                    return m;
                }
                return MaterialLibrary.Get(clean);
            });
            if (fbx.Contains("/Monsters/")) return; // monster prefabs are assembled by RuntimeLibrary
            MakePrefab(fbx, $"{PrefabDir}/Custom/{name}.prefab", name);
        }

        static string StripSuffix(string n)
        {
            int dot = n.LastIndexOf('.');
            if (dot > 0 && n.Length - dot == 4 && int.TryParse(n.Substring(dot + 1), out _)) n = n.Substring(0, dot);
            return n;
        }

        static void Remap(string fbx, Func<string, Material> resolve)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            bool changed = false;
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>())
            {
                var target = resolve(sub.name) ?? resolve(StripSuffix(sub.name));
                if (target == null) { Debug.LogWarning($"[SyncRooms] {fbx}: no material for slot '{sub.name}'"); continue; }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sub.name), target);
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }

        static void MakePrefab(string fbx, string prefabPath, string id)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (!model) return;
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = id;
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
            }

            bool custom = prefabPath.Contains("/Custom/");
            if (Trees.Contains(id))
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = 0.35f;
                cap.height = 4f;
                cap.center = new Vector3(0, 2f, 0);
            }
            else if (!NoCollider.Contains(id) && !id.StartsWith("Arch_") || custom)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    // Emissive light diffusers and glass don't need collision; everything else gets an exact mesh collider.
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
