using System.IO;
using System.Linq;
using SyncRooms.Gen;
using SyncRooms.Monsters;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>Builds monster prefabs and the runtime Registry (materials + prefabs by name) used by streamed levels.</summary>
    public static class RuntimeLibrary
    {
        const string MonsterDir = "Assets/_Project/Prefabs/Monsters";

        [MenuItem("SyncRooms/4b. Build Monsters + Runtime Registry")]
        public static void BuildAll()
        {
            BuildMonsters();
            BuildRegistry();
        }

        class Cfg
        {
            public string name; public MonsterAnimator.Style style;
            public float radius, height, centerY, wander, chase, sight, hearing, dmg, hover, scale = 1f;
            public bool flying, dark; public string idle, chaseSnd, attack; public float glow;
        }

        static readonly Cfg[] Monsters =
        {
            new() { name = "Hound", style = MonsterAnimator.Style.Hound, radius = 0.35f, height = 1.0f, centerY = 0.5f, wander = 1.3f, chase = 5.2f, sight = 25, hearing = 16, dmg = 30,
                idle = "Monsters/hound_growl", chaseSnd = "Monsters/hound_scream", attack = "Monsters/hound_scream" },
            new() { name = "Deathmoth", style = MonsterAnimator.Style.Moth, radius = 0.45f, height = 0.9f, centerY = 0f, wander = 1.5f, chase = 4f, sight = 22, hearing = 10, dmg = 20, flying = true, hover = 1.9f,
                idle = "Monsters/moth_flutter", chaseSnd = "Monsters/moth_flutter", attack = "Monsters/moth_flutter" },
            new() { name = "Smiler", style = MonsterAnimator.Style.Smiler, radius = 0.35f, height = 0.8f, centerY = 0f, wander = 0.8f, chase = 3.6f, sight = 20, hearing = 8, dmg = 40, flying = true, hover = 1.6f, dark = true, glow = 25f,
                idle = "Monsters/smiler_giggle", chaseSnd = "Monsters/smiler_giggle", attack = "Monsters/hound_scream" },
            new() { name = "Toon", style = MonsterAnimator.Style.Toon, radius = 0.35f, height = 2.4f, centerY = 1.2f, wander = 1.4f, chase = 4.6f, sight = 30, hearing = 18, dmg = 35,
                idle = "Monsters/toon_laugh", chaseSnd = "Monsters/toon_laugh", attack = "Monsters/hound_scream" },
            new() { name = "Slime", style = MonsterAnimator.Style.Slime, radius = 0.45f, height = 0.8f, centerY = 0.4f, wander = 0.8f, chase = 2.6f, sight = 16, hearing = 10, dmg = 15,
                idle = "Monsters/slime_squelch", chaseSnd = "Monsters/slime_squelch", attack = "Monsters/slime_squelch" },
            new() { name = "SlimeBig", style = MonsterAnimator.Style.Slime, radius = 0.9f, height = 1.6f, centerY = 0.8f, wander = 0.6f, chase = 2.0f, sight = 16, hearing = 10, dmg = 30, scale = 2f,
                idle = "Monsters/slime_squelch", chaseSnd = "Monsters/slime_squelch", attack = "Monsters/slime_squelch" },
        };

        static void BuildMonsters()
        {
            Directory.CreateDirectory(MonsterDir);
            foreach (var c in Monsters)
            {
                string model = c.name == "SlimeBig" ? "Slime" : c.name;
                var src = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Models/Custom/Monsters/{model}.fbx");
                if (!src) { Debug.LogWarning("[SyncRooms] missing monster model " + model); continue; }
                var root = new GameObject(c.name);
                var cc = root.AddComponent<CharacterController>();
                cc.radius = c.radius; cc.height = c.height; cc.center = new Vector3(0, c.centerY, 0);
                cc.stepOffset = Mathf.Min(0.4f, c.height * 0.4f);
                var m = root.AddComponent<Monster>();
                m.wanderSpeed = c.wander; m.chaseSpeed = c.chase; m.sightRange = c.sight; m.hearingRange = c.hearing; m.damage = c.dmg;
                m.flying = c.flying; m.hoverHeight = c.hover; m.onlyInDarkness = c.dark;
                m.idleSound = c.idle; m.chaseSound = c.chaseSnd; m.attackSound = c.attack;
                m.attackRange = c.radius + 1.1f;
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(src);
                vis.name = "Visual";
                vis.transform.SetParent(root.transform, false);
                vis.transform.localScale = Vector3.one * c.scale;
                foreach (var col in vis.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = c.style == MonsterAnimator.Style.Smiler ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                vis.AddComponent<MonsterAnimator>().style = c.style;
                if (c.glow > 0)
                {
                    var lg = new GameObject("Glow");
                    lg.transform.SetParent(root.transform, false);
                    lg.transform.localPosition = new Vector3(0, 0, 0.3f);
                    lg.AddHDLight(LightType.Point);
                    var l = lg.GetComponent<Light>();
                    l.lightUnit = LightUnit.Lumen; l.intensity = c.glow; l.range = 3f; l.shadows = LightShadows.None;
                }
                PrefabUtility.SaveAsPrefabAsset(root, $"{MonsterDir}/{c.name}.prefab");
                Object.DestroyImmediate(root);
            }
        }

        public static void BuildRegistry()
        {
            var reg = AssetDatabase.LoadAssetAtPath<Registry>("Assets/_Project/Resources/Registry.asset");
            if (!reg)
            {
                reg = ScriptableObject.CreateInstance<Registry>();
                AssetDatabase.CreateAsset(reg, "Assets/_Project/Resources/Registry.asset");
            }
            reg.materials = Directory.GetFiles(MaterialLibrary.MatDir, "*.mat", SearchOption.TopDirectoryOnly)
                .Select(p => p.Replace('\\', '/'))
                .Select(p => new Registry.MatEntry { name = Path.GetFileNameWithoutExtension(p), mat = AssetDatabase.LoadAssetAtPath<Material>(p) })
                .ToList();
            reg.prefabs = Directory.GetFiles("Assets/_Project/Prefabs", "*.prefab", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Select(p => new Registry.PrefabEntry { name = Path.GetFileNameWithoutExtension(p), prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p) })
                .ToList();
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SyncRooms] Registry: {reg.materials.Count} materials, {reg.prefabs.Count} prefabs");
        }
    }
}
