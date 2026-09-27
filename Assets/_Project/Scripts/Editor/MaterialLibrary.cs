using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>
    /// Builds HDRP Lit materials for the whole project:
    ///  - texture-based surfaces from Tools/materials.json (+ generated ones)
    ///  - simple procedural materials (glass, emissive panels, plastics...)
    /// Every architecture mesh uses UVs in meters, so tiling = 1 / tileMeters.
    /// </summary>
    public static class MaterialLibrary
    {
        public const string MatDir = "Assets/_Project/Art/Materials";
        public const string TexDir = MatDir + "/Textures";
        static readonly Dictionary<string, Material> Cache = new();

        // Per-material tuning: tile meters, pixel displacement depth (cm), clear coat, tint, smoothness range.
        class Tune
        {
            public float tile = 1f, pom = 0f, coat = 0f, normal = 1f, smin = 0f, smax = 1f;
            public Color tint = Color.white;
        }

        static readonly Dictionary<string, Tune> Tunes = new()
        {
            ["L4_Carpet"] = new Tune { tile = 1.2f, pom = 0.4f, tint = new Color(0.78f, 0.76f, 0.7f) },
            ["L4_Wall"] = new Tune { tile = 1.2f, tint = new Color(0.9f, 0.89f, 0.82f), normal = 0.6f },
            ["L4_Ceiling"] = new Tune { tile = 0.6f, normal = 0.8f },
            ["L4_Tile"] = new Tune { tile = 1.2f, pom = 0.3f, coat = 0.35f },
            ["L4_Door"] = new Tune { tile = 1.5f },
            ["L4_Metal"] = new Tune { tile = 1f },
            ["L4_Concrete"] = new Tune { tile = 2f },
            ["L5_Parquet"] = new Tune { tile = 1.4f, pom = 0.3f, coat = 0.6f, smin = 0.1f, smax = 0.9f },
            ["L5_Marble"] = new Tune { tile = 2.5f, coat = 1f, smin = 0.6f, smax = 1f },
            ["L5_WoodPanel"] = new Tune { tile = 1.2f, coat = 0.5f, tint = new Color(0.62f, 0.4f, 0.3f) },
            ["L5_Walnut"] = new Tune { tile = 1f, coat = 0.7f, tint = new Color(0.7f, 0.45f, 0.35f) },
            ["L5_Velvet"] = new Tune { tile = 0.8f },
            ["L5_Gold"] = new Tune { tile = 0.5f, smin = 0.35f, smax = 0.95f },
            ["L5_Plaster"] = new Tune { tile = 2f, tint = new Color(0.93f, 0.89f, 0.8f) },
            ["L5_Damask"] = new Tune { tile = 0.62f },
            ["L5_HotelCarpet"] = new Tune { tile = 1.1f, pom = 0.3f },
            ["L5_Brick"] = new Tune { tile = 1.6f, pom = 1.2f },
            ["L5_Rust"] = new Tune { tile = 1.5f },
            ["L5_OldConcrete"] = new Tune { tile = 2.5f, pom = 0.3f },
            ["L37_TileSmall"] = new Tune { tile = 0.5f, pom = 0.12f, coat = 1f, smin = 0.55f, smax = 1f, tint = new Color(0.97f, 0.98f, 0.98f) },
            ["L37_TileLarge"] = new Tune { tile = 1.2f, pom = 0.15f, coat = 1f, smin = 0.5f, smax = 1f, tint = new Color(0.97f, 0.98f, 0.98f) },
            ["L94_Grass"] = new Tune { tile = 2.5f, pom = 0.6f, tint = new Color(0.95f, 1f, 0.85f) },
            ["L94_Path"] = new Tune { tile = 2.2f, pom = 1.5f },
            ["L94_Asphalt"] = new Tune { tile = 4f, pom = 0.3f },
            ["L94_Sidewalk"] = new Tune { tile = 2f, pom = 0.5f },
            ["L94_Brick"] = new Tune { tile = 1.6f, pom = 1f },
            ["L94_SidingYellow"] = new Tune { tile = 1.5f },
            ["L94_SidingGreen"] = new Tune { tile = 1.5f },
            ["L94_SidingBlue"] = new Tune { tile = 1.5f },
            ["L94_SidingWhite"] = new Tune { tile = 1.5f },
            ["L94_RoofRed"] = new Tune { tile = 1.6f, pom = 1.5f },
            ["L94_RoofGrey"] = new Tune { tile = 1.6f, pom = 1.2f },
            ["L94_Stone"] = new Tune { tile = 1.2f, pom = 0.3f },
            ["L94_PaintedMetal"] = new Tune { tile = 2f },
            ["L94_Wood"] = new Tune { tile = 1.5f },
            ["Com_Chrome"] = new Tune { tile = 1f, smin = 0.7f, smax = 1f },
            ["Com_Paper"] = new Tune { tile = 0.5f, tint = new Color(0.95f, 0.93f, 0.86f) },
        };

        [MenuItem("SyncRooms/2. Build Material Library")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            foreach (var dir in Directory.GetDirectories(TexDir))
            {
                string name = Path.GetFileName(dir);
                if (name.EndsWith("Base")) continue; // raw inputs of generated textures
                BuildTextured(name);
            }
            BuildProcedural();
            AssetDatabase.SaveAssets();
            Debug.Log("[SyncRooms] Material library built.");
        }

        public static Material Get(string name)
        {
            if (Cache.TryGetValue(name, out var m) && m) return m;
            m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
            if (m) Cache[name] = m;
            else Debug.LogWarning("[SyncRooms] Missing material " + name);
            return m;
        }

        public static Material NewLit(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;
            var m = new Material(Shader.Find("HDRP/Lit"));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Texture2D Tex(string dir, string file)
        {
            foreach (var ext in new[] { ".jpg", ".png" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{file}{ext}");
                if (t) return t;
            }
            return null;
        }

        static void BuildTextured(string name)
        {
            string dir = $"{TexDir}/{name}";
            var tune = Tunes.TryGetValue(name, out var t) ? t : new Tune();
            var m = NewLit($"{MatDir}/{name}.mat");
            ApplyMaps(m, Tex(dir, "Albedo"), Tex(dir, "Normal"), Tex(dir, "Mask"), tune.pom > 0 ? Tex(dir, "Height") : null,
                tune.tint, tune.normal, tune.smin, tune.smax, tune.pom, tune.coat);
            m.SetTextureScale("_BaseColorMap", Vector2.one / tune.tile);
            var op = Tex(dir, "Opacity");
            if (op != null && name.StartsWith("L94_Roof")) { } // roofing opacity maps unused (solid roofs)
            HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
        }

        public static void ApplyMaps(Material m, Texture2D albedo, Texture2D normal, Texture2D mask, Texture2D height,
            Color tint, float normalScale = 1f, float smin = 0f, float smax = 1f, float pomCm = 0f, float coat = 0f)
        {
            m.SetColor("_BaseColor", tint);
            if (albedo) m.SetTexture("_BaseColorMap", albedo);
            if (normal)
            {
                m.SetTexture("_NormalMap", normal);
                m.SetFloat("_NormalScale", normalScale);
            }
            if (mask)
            {
                m.SetTexture("_MaskMap", mask);
                m.SetFloat("_MetallicRemapMin", 0f);
                m.SetFloat("_MetallicRemapMax", 1f);
                m.SetFloat("_SmoothnessRemapMin", smin);
                m.SetFloat("_SmoothnessRemapMax", smax);
                m.SetFloat("_AORemapMin", 0.15f);
                m.SetFloat("_AORemapMax", 1f);
            }
            if (height && pomCm > 0f)
            {
                m.SetTexture("_HeightMap", height);
                m.SetFloat("_DisplacementMode", 2f /* pixel displacement */);
                m.SetFloat("_HeightPoMAmplitude", pomCm);
                m.SetFloat("_HeightCenter", 0.5f);
                m.SetFloat("_PPDMinSamples", 5);
                m.SetFloat("_PPDMaxSamples", 15);
                m.SetFloat("_PPDLodThreshold", 5);
                m.SetFloat("_PPDPrimitiveLength", 1f);
                m.SetFloat("_PPDPrimitiveWidth", 1f);
                m.SetFloat("_DepthOffsetEnable", 0f);
            }
            if (coat > 0f)
            {
                m.SetFloat("_CoatMask", coat);
                m.EnableKeyword("_MATERIAL_FEATURE_CLEAR_COAT");
                m.SetFloat("_MaterialID", 1);
            }
            m.SetFloat("_ReceivesSSR", 1f);
            m.enableInstancing = true;
        }

        // --------------------------------------------------------------- procedural
        static void BuildProcedural()
        {
            Solid("Glass", new Color(0.9f, 0.95f, 0.95f, 0.12f), 0f, 0.98f, transparent: true);
            Solid("GlassFrosted", new Color(0.95f, 0.97f, 1f, 0.55f), 0f, 0.55f, transparent: true);
            Solid("Mirror", new Color(0.95f, 0.95f, 0.95f), 1f, 1f);
            Solid("BlackOut", new Color(0.015f, 0.015f, 0.015f), 0f, 0.35f);
            Solid("PlasticWhite", new Color(0.86f, 0.86f, 0.83f), 0f, 0.55f);
            Solid("PlasticBeige", new Color(0.8f, 0.76f, 0.66f), 0f, 0.45f);
            Solid("PlasticBlue", new Color(0.2f, 0.42f, 0.75f, 0.55f), 0f, 0.9f, transparent: true);
            Solid("PlasticBlack", new Color(0.04f, 0.04f, 0.04f), 0f, 0.45f);
            Solid("PlasticRed", new Color(0.6f, 0.05f, 0.04f), 0f, 0.5f);
            Solid("Rubber", new Color(0.03f, 0.03f, 0.03f), 0f, 0.2f);
            Solid("PaintOffWhite", new Color(0.83f, 0.82f, 0.77f), 0f, 0.35f);
            Solid("PaintCream", new Color(0.9f, 0.86f, 0.74f), 0f, 0.4f);
            Solid("PaintGreen", new Color(0.18f, 0.35f, 0.26f), 0f, 0.45f);
            Solid("PaintRed", new Color(0.55f, 0.08f, 0.06f), 0f, 0.45f);
            Solid("PaintBlack", new Color(0.03f, 0.03f, 0.03f), 0f, 0.5f);
            Solid("Brass", new Color(0.83f, 0.66f, 0.37f), 1f, 0.78f);
            Solid("Steel", new Color(0.62f, 0.62f, 0.62f), 1f, 0.6f);
            Solid("DarkMetal", new Color(0.2f, 0.2f, 0.2f), 1f, 0.45f);
            Solid("Liquid", new Color(0.93f, 0.9f, 0.82f, 0.45f), 0f, 0.95f, transparent: true);
            Solid("ElevatorSteel", new Color(0.7f, 0.68f, 0.64f), 1f, 0.72f);
            Solid("MonsterSkin", new Color(0.6f, 0.58f, 0.55f), 0f, 0.4f);
            Solid("MonsterEye", new Color(0.01f, 0.01f, 0.01f), 0f, 0.95f);
            Solid("MothWing", new Color(0.36f, 0.31f, 0.25f), 0f, 0.2f);
            Solid("MothBody", new Color(0.2f, 0.14f, 0.1f), 0f, 0.25f);
            Solid("ToonBlack", new Color(0.02f, 0.02f, 0.02f), 0f, 0.65f);
            Solid("ToonWhite", new Color(0.93f, 0.92f, 0.88f), 0f, 0.5f);
            Solid("Slime", new Color(0.3f, 0.85f, 0.35f, 0.6f), 0f, 0.95f, transparent: true);
            Solid("SlimeCore", new Color(0.08f, 0.35f, 0.1f), 0f, 0.8f);
            Solid("LeviathanSkin", new Color(0.03f, 0.04f, 0.05f), 0f, 0.7f);
            Solid("Puddle", new Color(0.04f, 0.045f, 0.05f, 0.35f), 0f, 1f, transparent: true);
            Solid("PaintYellow", new Color(0.88f, 0.68f, 0.1f), 0f, 0.4f);
            Solid("PaintLine", new Color(0.85f, 0.85f, 0.8f), 0f, 0.35f);
            Solid("Jacket", new Color(0.13f, 0.15f, 0.12f), 0f, 0.3f);
            Solid("Jeans", new Color(0.1f, 0.13f, 0.2f), 0f, 0.25f);
            Solid("Skin", new Color(0.72f, 0.52f, 0.42f), 0f, 0.45f);
            Solid("ShoeLeather", new Color(0.08f, 0.055f, 0.04f), 0f, 0.55f);
            Emissive("SmilerGlow", new Color(1f, 1f, 0.95f), 9f);
            Emissive("EmissivePanel", new Color(1f, 0.97f, 0.9f), 9f);       // fluorescent diffuser
            Emissive("EmissiveWarm", new Color(1f, 0.78f, 0.48f), 9f);       // incandescent bulbs
            Emissive("EmissiveExit", new Color(0.1f, 1f, 0.3f), 7f);
            Emissive("EmissiveSky", new Color(0.85f, 0.93f, 1f), 11f);       // skylights in Level 37
            Emissive("EmissiveRed", new Color(1f, 0.1f, 0.05f), 6f);
        }

        public static Material Solid(string name, Color c, float metal, float smooth, bool transparent = false)
        {
            var m = NewLit($"{MatDir}/{name}.mat");
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", smooth);
            if (transparent)
            {
                HDMaterial.SetSurfaceType(m, true);
                m.SetFloat("_BlendMode", 0); // alpha
                m.SetFloat("_EnableFogOnTransparent", 1);
                m.SetFloat("_TransparentBackfaceEnable", 0);
                m.SetFloat("_ReceivesSSRTransparent", 1);
                m.SetFloat("_TransparentWritingMotionVec", 0);
                if (name.StartsWith("Glass") || name == "Liquid" || name == "PlasticBlue" || name == "Slime")
                {
                    m.SetFloat("_RefractionModel", 3); // thin
                    m.SetFloat("_Ior", 1.45f);
                }
            }
            m.enableInstancing = true;
            HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            Cache[name] = m;
            return m;
        }

        public static Material Emissive(string name, Color c, float ev)
        {
            var m = NewLit($"{MatDir}/{name}.mat");
            m.SetColor("_BaseColor", c * 0.9f);
            m.SetFloat("_Smoothness", 0.4f);
            HDMaterial.SetUseEmissiveIntensity(m, true);
            HDMaterial.SetEmissiveColor(m, c);
            HDMaterial.SetEmissiveIntensity(m, ev, EmissiveIntensityUnit.EV100);
            m.SetFloat("_EmissiveExposureWeight", 0.9f);
            m.enableInstancing = true;
            HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            Cache[name] = m;
            return m;
        }
    }
}
