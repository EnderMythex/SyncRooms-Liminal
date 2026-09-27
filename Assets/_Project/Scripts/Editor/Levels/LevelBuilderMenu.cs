using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    public static class LevelBuilderMenu
    {
        static readonly string[] Order = { "MainMenu", "Level4", "Level5", "Level94", "Level7", "Level37", "Level2", "Level1", "Hub" };

        [MenuItem("SyncRooms/5. Build All Levels")]
        public static void BuildAllLevels()
        {
            Level4Builder.BuildMainMenu();
            StreamedLevels.BuildLevel4();
            Level5Builder.Build();
            Level94Builder.Build();
            HubAndOceanBuilders.BuildLevel7();
            StreamedLevels.BuildLevel37();
            StreamedLevels.BuildLevel2();
            StreamedLevels.BuildLevel1();
            HubAndOceanBuilders.BuildHub();
            RegisterScenes();
        }

        static void TryBuild(string name)
        {
            var t = System.Type.GetType($"SyncRooms.EditorTools.{name}Builder");
            var m = t?.GetMethod("Build", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m != null) m.Invoke(null, null);
        }

        public static string[] SceneList() =>
            Order.Select(n => $"{LevelKit.SceneDir}/{n}.unity").Where(File.Exists).ToArray();

        public static void RegisterScenes()
        {
            EditorBuildSettings.scenes = SceneList().Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            Debug.Log("[SyncRooms] Build settings scenes: " + string.Join(", ", SceneList()));
        }

        /// <summary>Logs imported mesh bounds of a few custom models to verify Blender->Unity axis conventions.</summary>
        public static void DebugAxes()
        {
            foreach (var n in new[] { "Office_DoorLeaf", "ExitSign", "Hotel_DoorLeaf", "Troffer", "OfficeDesk" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Models/Custom/{n}.fbx");
                var mf = go ? go.GetComponentInChildren<MeshFilter>() : null;
                if (mf == null) { Debug.Log($"[Axes] {n}: missing"); continue; }
                Debug.Log($"[Axes] {n}: mesh bounds min={mf.sharedMesh.bounds.min:F3} max={mf.sharedMesh.bounds.max:F3} localRot={mf.transform.localEulerAngles} localScale={mf.transform.localScale}");
            }
        }

        // ------------------------------------------------------------------ QA screenshots
        /// <summary>
        /// -executeMethod SyncRooms.EditorTools.LevelBuilderMenu.Screenshots  (env SR_SCENE, SR_SHOTS="x,y,z,yaw,pitch;...")
        /// Without SR_SHOTS: shots from the player camera plus random interior viewpoints.
        /// </summary>
        public static void Screenshots()
        {
            string scene = System.Environment.GetEnvironmentVariable("SR_SCENE") ?? "Level4";
            string shots = System.Environment.GetEnvironmentVariable("SR_SHOTS");
            string outDir = System.Environment.GetEnvironmentVariable("SR_OUT") ?? "Temp/Shots";
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene($"{LevelKit.SceneDir}/{scene}.unity", OpenSceneMode.Single);
            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            // realtime probes must render once in edit mode
            foreach (var p in Object.FindObjectsByType<HDProbe>(FindObjectsSortMode.None)) p.RequestRenderNextUpdate();
            var views = new List<(Vector3 pos, Vector3 euler)>();
            if (!string.IsNullOrEmpty(shots))
            {
                foreach (var s in shots.Split(';'))
                {
                    var f = s.Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    views.Add((new Vector3(f[0], f[1], f[2]), new Vector3(f.Length > 4 ? f[4] : 0, f[3], 0)));
                }
            }
            else
            {
                views.Add((cam.transform.position, cam.transform.eulerAngles));
                var rng = new System.Random(1);
                var probes = Object.FindObjectsByType<HDAdditionalReflectionData>(FindObjectsSortMode.None);
                foreach (var pr in probes.OrderBy(_ => rng.Next()).Take(5))
                    views.Add((pr.transform.position + Vector3.up * 0.25f, new Vector3(5, rng.Next(0, 360), 0)));
            }
            int i = 0;
            foreach (var v in views)
            {
                cam.transform.SetPositionAndRotation(v.pos, Quaternion.Euler(v.euler));
                var tex = EditorCapture.Render(cam, 1280, 720, 24);
                var st = UnityEngine.Rendering.VolumeManager.instance.stack;
                var ve = st.GetComponent<VisualEnvironment>();
                var gs = st.GetComponent<GradientSky>();
                var ex = st.GetComponent<Exposure>();
                var fog = st.GetComponent<Fog>();
                Debug.Log($"[SKY] type={ve.skyType.value} gradExp={gs.exposure.value} mode={gs.skyIntensityMode.value} mult={gs.multiplier.value} intensity={gs.GetIntensityFromSettings()} top={gs.top.value} expMode={ex.mode.value} lim={ex.limitMin.value}-{ex.limitMax.value} fog={fog.enabled.value} sun={Object.FindAnyObjectByType<Light>()?.intensity}");
                var px = tex.GetPixel(640, 700); var gpx = tex.GetPixel(640, 60);
                Debug.Log($"[SKY] skyPixel={px} groundPixel={gpx}");
                EditorCapture.SavePng(tex, $"{outDir}/{scene}_{i++:00}.png");
                Object.DestroyImmediate(tex);
            }
            Debug.Log($"[SyncRooms] {i} screenshots saved to {outDir}");
        }
    }
}
