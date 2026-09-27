using System.Collections.Generic;
using SyncRooms.Monsters;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using O = SyncRooms.EditorTools.GeoBuilder.Opening;

namespace SyncRooms.EditorTools
{
    /// <summary>The Hub (level select corridor) and Level 7 "Thalassophobia" (bunker on an endless foggy ocean).</summary>
    public static class HubAndOceanBuilders
    {
        // ================================================================== HUB
        [MenuItem("SyncRooms/Levels/Build The Hub")]
        public static void BuildHub()
        {
            var scene = LevelKit.NewScene("Hub");
            var root = LevelKit.Group("Hub").transform;
            var props = LevelKit.Group("Props", root).transform;
            var lights = LevelKit.Group("Lights", root).transform;
            var geo = new GeoBuilder(20f);
            geo.SetSurface("L5_Parquet", SurfaceType.Wood);
            const float W = 4f, L = 62f, H = 3.4f;
            var r = new Rect(-W / 2, 0, W, L);
            var ops = new[] { new List<O>(), new List<O>(), new List<O>(), new List<O>() };
            string[] doors = { "Level4", "Level5", "Level94", "Level7", "Level37", "Level2", "Level1" };
            for (int i = 0; i < doors.Length; i++)
            {
                float z = 8f + i * 7f;
                int side = i % 2 == 0 ? 2 : 3;
                ops[side].Add(new O(z, 1.0f, 2.2f));
            }
            ops[1].Add(new O(W / 2, 1.0f, 2.2f));   // SORTIE at the far end
            geo.Space(r, 0, H, 0.24f, "L5_Parquet", "L5_Plaster", "L5_Plaster", ops, "L5_WoodPanel");
            geo.Floor("L5_HotelCarpet", Rect.MinMaxRect(-1.1f, 0.3f, 1.1f, L - 0.3f), 0.006f);
            geo.Emit(root, "Architecture");
            for (int i = 0; i < doors.Length; i++)
            {
                float z = 8f + i * 7f;
                bool west = i % 2 == 0;
                float x = west ? -W / 2 : W / 2;
                float yaw = west ? 90 : -90;
                HubDoor(props, new Vector3(x, 0, z), yaw, doors[i]);
            }
            HubDoor(props, new Vector3(0, 0, L), 180, "MainMenu");
            // lighting: warm pendant bulbs
            for (float z = 4f; z < L; z += 6f)
            {
                LevelKit.Place("caged_hanging_light", new Vector3(0, H - 0.75f, z), 0, props, 0.8f);
                var l = LevelKit.PointLight(lights, new Vector3(0, H - 0.9f, z), 900f, Color.white, 9f, z < 12f, 2700f);
            }
            for (float z = 5f; z < L - 3; z += 14f)
            {
                LevelKit.Place("Wainscot", new Vector3(-W / 2, 0, z), 90, props);
                LevelKit.Place("ClassicConsole_01", new Vector3(W / 2 - 0.3f, 0, z + 2.5f), -90, props, 0.8f);
            }
            LevelKit.Place("vintage_grandfather_clock_01", new Vector3(-W / 2 + 0.25f, 0, L - 1.2f), 90, props);
            LevelKit.ReflectionProbe(lights, new Vector3(0, H / 2, L / 2), new Vector3(W, H, L));
            LevelKit.GlobalVolume(root, "Hub", new LevelKit.Look
            {
                expMin = 6.8f, expMax = 9.5f, expComp = -0.4f, fogMFP = 30f, fogMaxHeight = 4f, fogAlbedo = new Color(1f, 0.92f, 0.8f),
                bloom = 0.35f, vignette = 0.3f, grain = 0.2f, saturation = -5f, contrast = 8f, temperature = 8f,
                skyType = 3, gradientTop = new Color(0.01f, 0.01f, 0.01f), gradientMid = new Color(0.01f, 0.01f, 0.01f), gradientBottom = new Color(0.01f, 0.01f, 0.01f),
                shadowDistance = 30f
            });
            var sp = new GameObject("Spawn").transform;
            sp.SetParent(root, false);
            sp.position = new Vector3(0, 0.05f, 2f);
            LevelKit.Player(root, sp.position, 0);
            var ls = LevelKit.Settings(root, sp, 0f, 0f, new[] { "Ambience/amb_building_tone" }, 0.25f);
            ls.arrival = ArrivalKind.Walk;
            LevelKit.SaveScene(scene, "Hub");
        }

        static void HubDoor(Transform props, Vector3 wallPoint, float yaw, string target)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var frame = LevelKit.Place("Hotel_DoorFrame", wallPoint + fwd * 0.12f, yaw, props);
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get("Hotel_DoorLeaf"), frame.transform);
            leaf.transform.localPosition = new Vector3(0.475f, 0, 0);
            foreach (var t in leaf.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var exit = leaf.AddComponent<LevelExit>();
            exit.targetScene = target;
            var info = SyncRooms.Core.LevelCatalog.Find(target);
            exit.prompt = target == "MainMenu" ? "[E] Retourner au menu" : $"[E] {info?.number} — {info?.title}";
            exit.exitKind = ExitKind.Walk;
            exit.anchor = frame.transform;
            LevelKit.Place("Sign_" + target, wallPoint + fwd * 0.16f + Vector3.up * 2.62f, yaw, props);
        }

        // ================================================================== LEVEL 7
        [MenuItem("SyncRooms/Levels/Build Level 7 (Thalassophobia)")]
        public static void BuildLevel7()
        {
            var scene = LevelKit.NewScene("Level7");
            var root = LevelKit.Group("Level7").transform;
            var props = LevelKit.Group("Props", root).transform;
            var lights = LevelKit.Group("Lights", root).transform;
            var geo = new GeoBuilder(20f);
            geo.SetSurface("L5_Rust", SurfaceType.Metal);
            geo.SetSurface("L94_PaintedMetal", SurfaceType.Metal);

            // hull / deck (deck top at y = 1.2)
            const float D = 1.2f;
            geo.Box("L5_Rust", new Vector3(-10, -2.5f, -7), new Vector3(10, D, 7));
            // cabin: 12 x 8 x 3.2, door on the south side, portholes
            var cabin = Rect.MinMaxRect(-6, -4, 6, 4);
            var ops = new[] { new List<O>(), new List<O>(), new List<O>(), new List<O>() };
            ops[0].Add(new O(6f, 1.0f, D + 2.1f, D));
            geo.InnerWalls("L94_PaintedMetal", cabin, D, D + 3.2f, 0.2f, ops, "L5_Rust");
            geo.Floor("L94_PaintedMetal", cabin, D + 0.005f);
            geo.Floor("L5_Rust", cabin, D + 3.2f, false);
            // cabin outer shell (thin boxes around the inner walls) + roof
            geo.Box("L5_Rust", new Vector3(-6.2f, D, -4.2f), new Vector3(-0.5f, D + 3.4f, -4.1f), true, false);
            geo.Box("L5_Rust", new Vector3(0.5f, D, -4.2f), new Vector3(6.2f, D + 3.4f, -4.1f), true, false);
            geo.Box("L5_Rust", new Vector3(-0.5f, D + 2.1f, -4.2f), new Vector3(0.5f, D + 3.4f, -4.1f), false, true);
            geo.Box("L5_Rust", new Vector3(-6.2f, D, 4.1f), new Vector3(6.2f, D + 3.4f, 4.2f), true, false);
            geo.Box("L5_Rust", new Vector3(-6.2f, D, -4.1f), new Vector3(-6.1f, D + 3.4f, 4.1f), true, false);
            geo.Box("L5_Rust", new Vector3(6.1f, D, -4.1f), new Vector3(6.2f, D + 3.4f, 4.1f), true, false);
            geo.Box("L5_Rust", new Vector3(-6.2f, D + 3.2f, -4.2f), new Vector3(6.2f, D + 3.4f, 4.2f));
            // railings around the deck
            foreach (var (a, b) in new[] { (new Vector3(-9.9f, D, -6.95f), new Vector3(9.9f, D, -6.95f)), (new Vector3(-9.9f, D, 6.95f), new Vector3(9.9f, D, 6.95f)),
                                           (new Vector3(-9.95f, D, -6.9f), new Vector3(-9.95f, D, 6.9f)), (new Vector3(9.95f, D, -6.9f), new Vector3(9.95f, D, -1.1f)), (new Vector3(9.95f, D, 1.1f), new Vector3(9.95f, D, 6.9f)) })
            {
                var min = Vector3.Min(a, b) - new Vector3(0.03f, 0, 0.03f);
                var max = Vector3.Max(a, b) + new Vector3(0.03f, 0, 0.03f);
                geo.Box("Steel", min + Vector3.up * 1.0f, max + Vector3.up * 1.06f);
                geo.Box("Steel", min + Vector3.up * 0.5f, max + Vector3.up * 0.54f);
                for (float t = 0; t <= 1.001f; t += 0.1f)
                {
                    var p = Vector3.Lerp(a, b, t);
                    geo.Box("Steel", p + new Vector3(-0.03f, 0, -0.03f), p + new Vector3(0.03f, 1.06f, 0.03f));
                }
            }
            // hatch area: a dark square in the cabin floor
            geo.Floor("PaintBlack", Rect.MinMaxRect(3.4f, 1.6f, 4.6f, 2.8f), D + 0.012f);
            geo.Emit(root, "Bunker");

            // interior props
            LevelKit.Place("metal_office_desk", new Vector3(-3.5f, D, 2.9f), 180, props, 0.8f);
            LevelKit.Place("SchoolChair_01", new Vector3(-3.5f, D, 2.0f), 10, props);
            LevelKit.Place("portable_cassette_player", new Vector3(-3.2f, D + 0.64f, 3.0f), 160, props, 0.8f);
            LevelKit.Place("steel_frame_shelves_03", new Vector3(-5.6f, D, -1f), 90, props, 0.8f);
            LevelKit.Place("steel_frame_shelves_03", new Vector3(5.6f, D, -1.5f), -90, props, 0.8f);
            for (int i = 0; i < 3; i++) LevelKit.Place("cardboard_box_01", new Vector3(-5.2f + i * 0.5f, D, -3.3f), i * 40, props);
            LevelKit.Place("Barrel_01", new Vector3(4.8f, D, 3.2f), 0, props);
            LevelKit.Place("metal_jerrycan", new Vector3(5.3f, D, 3.3f), 30, props);
            LevelKit.Place("caged_hanging_light", new Vector3(0, D + 2.45f, 0), 0, props, 0.8f);
            var cl = LevelKit.PointLight(lights, new Vector3(0, D + 2.3f, 0), 900f, Color.white, 9f, true, 3200f);
            var fl = cl.gameObject.AddComponent<FlickerLight>();
            fl.lights = new[] { cl }; fl.emissiveRenderers = new Renderer[0]; fl.flicker = true; fl.flickerChance = 0.2f;
            LevelKit.PointLight(lights, new Vector3(-3.5f, D + 1.3f, 2.8f), 150f, new Color(1f, 0.6f, 0.3f), 3f, false);
            // ladder hatch: exit to Level 37
            var hatch = new GameObject("Exit → Level 37 (hatch)");
            hatch.transform.SetParent(root, false);
            hatch.transform.position = new Vector3(4f, D + 0.1f, 2.2f);
            hatch.AddComponent<BoxCollider>().size = new Vector3(1.2f, 0.2f, 1.2f);
            var le = hatch.AddComponent<LevelExit>();
            le.targetScene = "Level37";
            le.prompt = "[E] Ouvrir l'écoutille";
            le.requiredItemId = "never";
            le.missingItemMessage = "L'écoutille est soudée par la rouille. Le phare, au loin... il doit y en avoir une autre.";
            var note = LevelKit.Place("Item_Note", new Vector3(-3.7f, D + 0.64f, 2.7f), 20, props);
            if (note)
            {
                var rn = note.AddComponent<ReadableNote>();
                rn.title = "Journal de bord";
                rn.text = "Jour ?\n\nAucune côte. Aucun soleil. Juste le brouillard, et l'eau qui ne gèle jamais malgré le froid.\n\nLa nuit, quelque chose d'énorme passe sous la coque. Le bunker entier grince.\n\nNE PAS SAUTER À L'EAU.\n\nL'écoutille d'ici est soudée. Mais on voit le faisceau d'un PHARE à l'est, par temps clair. La barque est amarrée au ponton. Rame vers la lumière, et ne regarde pas sous la surface.";
                note.AddComponent<BoxCollider>().size = new Vector3(0.15f, 0.02f, 0.21f);
            }
            LevelKit.PlaceItem("almond_water", new Vector3(5.5f, D + 0.2f, 2.8f), 0, props);
            LevelKit.PlaceItem("ration", new Vector3(-5.4f, D + 0.9f, -1f), 0, props);
            // searchlight on the roof sweeping the fog
            var sl = LevelKit.SpotLight(lights, new Vector3(0, D + 3.9f, 0), Quaternion.Euler(12, 0, 0), 40000f, Color.white, 250f, 14f, true, 5000f);
            sl.GetComponent<HDAdditionalLightData>().volumetricDimmer = 3f;
            sl.gameObject.AddComponent<Rotator>().degreesPerSecond = 9f;
            LevelKit.Place("portable_searchlight", new Vector3(0, D + 3.4f, 0), 0, props);

            // endless ocean
            var oc = new GameObject("Ocean");
            oc.transform.SetParent(root, false);
            var ws = oc.AddComponent<WaterSurface>();
            ws.surfaceType = WaterSurfaceType.OceanSeaLake;
            ws.geometryType = WaterGeometryType.Infinite;
            ws.largeWindSpeed = 22f;
            ws.refractionColor = new Color(0.08f, 0.14f, 0.16f);
            ws.scatteringColor = new Color(0.03f, 0.08f, 0.1f);
            ws.absorptionDistance = 4f;
            ws.caustics = false;
            var kz = new GameObject("KillZone (sea)");
            kz.transform.SetParent(root, false);
            kz.transform.position = new Vector3(0, -3f, 0);
            var kb = kz.AddComponent<BoxCollider>();
            kb.isTrigger = true; kb.size = new Vector3(4000, 4, 4000);
            kz.AddComponent<KillZone>();
            var floor = new GameObject("Sea floor (safety)").AddComponent<BoxCollider>();
            floor.transform.SetParent(root, false);
            floor.transform.position = new Vector3(0, -6f, 0);
            floor.size = new Vector3(4000, 1, 4000);

            // the leviathan
            var lev = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Custom/Monsters/Leviathan.fbx");
            if (lev)
            {
                var levRoot = new GameObject("Leviathan");
                levRoot.transform.SetParent(root, false);
                var v = (GameObject)PrefabUtility.InstantiatePrefab(lev, levRoot.transform);
                foreach (var c in v.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                v.AddComponent<MonsterAnimator>().style = MonsterAnimator.Style.Leviathan;
                var sw = levRoot.AddComponent<Swimmer>();
                sw.center = new Vector3(165f, 0, 70f);
                sw.radius = 70f; sw.depth = 10f; sw.speed = 6f;
            }
            var horn = LevelKit.Audio3D(props, new Vector3(-180, 5, 240), "Ambience/amb_foghorn", 1f, 600f, false);
            var ta = horn.gameObject.AddComponent<TimedAudio>();
            ta.clips = new[] { AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Resources/Audio/Ambience/amb_foghorn.mp3") };
            ta.minGap = 40; ta.maxGap = 90; ta.startPlaying = false;

            // dock + rowboat
            var dockGeo = new GeoBuilder(40f);
            dockGeo.SetSurface("L94_Wood", SurfaceType.Wood);
            dockGeo.Box("L94_Wood", new Vector3(10f, 0.8f, -1f), new Vector3(15f, 1.0f, 1f));
            for (float x = 11f; x <= 15f; x += 2f)
                foreach (var z in new[] { -0.9f, 0.9f })
                    dockGeo.Box("L94_Wood", new Vector3(x - 0.1f, -2f, z - 0.1f), new Vector3(x + 0.1f, 0.8f, z + 0.1f), true, false);
            dockGeo.Emit(root, "Dock");
            var dock1 = new GameObject("Dock (bunker)").AddComponent<BoatDock>();
            dock1.transform.SetParent(root, false);
            dock1.transform.position = new Vector3(15.5f, 0, 0);
            dock1.exitPoint = new GameObject("Exit").transform;
            dock1.exitPoint.SetParent(dock1.transform, false);
            dock1.exitPoint.position = new Vector3(12.5f, 1.05f, 0);
            dock1.exitPoint.rotation = Quaternion.Euler(0, -90, 0);
            var boat = LevelKit.Place("Rowboat", new Vector3(17.2f, 0, 0), 90, props, 1f, false);
            if (boat)
            {
                var seat = new GameObject("Seat").transform;
                seat.SetParent(boat.transform, false);
                seat.localPosition = new Vector3(0, 0.1f, 0.2f);
                var b = boat.AddComponent<Boat>();
                b.seat = seat;
                LevelKit.PointLight(boat.transform, boat.transform.TransformPoint(new Vector3(0, 1.0f, 1.5f)), 350f, Color.white, 8f, false, 2200f);
            }

            // the lighthouse far away in the fog: the real way out
            var lhPos = new Vector3(330f, 1.5f, 140f);
            float lhYaw = Mathf.Atan2(-lhPos.x, -lhPos.z) * Mathf.Rad2Deg;
            var lh = LevelKit.Place("Lighthouse", lhPos, lhYaw, props);
            if (lh)
            {
                var t = lh.transform;
                var dock2 = new GameObject("Dock (lighthouse)").AddComponent<BoatDock>();
                dock2.transform.SetParent(root, false);
                dock2.transform.position = t.TransformPoint(new Vector3(0, -1.5f, 12f));
                dock2.exitPoint = new GameObject("Exit").transform;
                dock2.exitPoint.SetParent(dock2.transform, false);
                dock2.exitPoint.position = t.TransformPoint(new Vector3(0, -0.25f, 8f));
                dock2.exitPoint.rotation = Quaternion.Euler(0, lhYaw + 180f, 0);
                var steps = new GeoBuilder(40f);
                steps.Box("L94_Wood", t.TransformPoint(new Vector3(-0.8f, -0.6f, 6.4f)) - new Vector3(0, 0, 0), t.TransformPoint(new Vector3(0.8f, -0.3f, 6.4f)) + new Vector3(0, 0, 0));
                var hatch2 = new GameObject("Exit → Level 37 (lighthouse hatch)");
                hatch2.transform.SetParent(root, false);
                hatch2.transform.position = t.TransformPoint(new Vector3(-3.8f, 0.1f, -3.8f));
                hatch2.AddComponent<BoxCollider>().size = new Vector3(1.3f, 0.3f, 1.3f);
                var le2 = hatch2.AddComponent<LevelExit>();
                le2.targetScene = "Level37";
                le2.prompt = "[E] Descendre par l'écoutille";
                le2.exitKind = ExitKind.Ladder;
                le2.transitionSfx = null;
                var beam = LevelKit.SpotLight(lights, t.TransformPoint(new Vector3(0, 19f, 0)), Quaternion.Euler(4, 0, 0), 120000f, Color.white, 600f, 9f, false, 4200f);
                beam.GetComponent<HDAdditionalLightData>().volumetricDimmer = 6f;
                beam.gameObject.AddComponent<Rotator>().degreesPerSecond = 24f;
                LevelKit.PointLight(lights, t.TransformPoint(new Vector3(0, 19f, 0)), 20000f, Color.white, 60f, false, 3500f);
                LevelKit.PointLight(lights, t.TransformPoint(new Vector3(0, 2.2f, 0)), 1200f, Color.white, 12f, true, 3000f);
            }

            var safe = new GameObject("SafeZone (cabin)");
            safe.transform.SetParent(root, false);
            safe.transform.position = new Vector3(0, D + 1.6f, 0);
            var sb = safe.AddComponent<BoxCollider>(); sb.isTrigger = true; sb.size = new Vector3(11.6f, 3f, 7.6f);
            safe.AddComponent<SafeZone>();

            LevelKit.Sun(lights, new Vector3(30, 60, 0), 15000f, 7500f);
            LevelKit.GlobalVolume(root, "Level7", new LevelKit.Look
            {
                expMin = 8.5f, expMax = 12f, expComp = -0.5f, fogMFP = 35f, fogBaseHeight = 0f, fogMaxHeight = 60f, fogAlbedo = new Color(0.75f, 0.8f, 0.83f),
                bloom = 0.25f, vignette = 0.3f, grain = 0.22f, saturation = -35f, contrast = 8f, temperature = -12f,
                skyType = 3, gradientTop = new Color(0.4f, 0.43f, 0.46f), gradientMid = new Color(0.5f, 0.53f, 0.55f), gradientBottom = new Color(0.3f, 0.33f, 0.35f),
                skyExposure = 12f, shadowDistance = 60f
            });
            LevelKit.ReflectionProbe(lights, new Vector3(0, D + 1.6f, 0), new Vector3(12, 3.2f, 8));

            var sp = new GameObject("Spawn").transform;
            sp.SetParent(root, false);
            sp.position = new Vector3(-1.5f, D + 0.05f, -1f);
            sp.rotation = Quaternion.Euler(0, 180, 0);
            LevelKit.Player(root, sp.position, 180);
            var ls = LevelKit.Settings(root, sp, 1.5f, 1f, new[] { "Ambience/amb_ocean", "Ambience/amb_wind" }, 0.4f);
            ls.arrival = ArrivalKind.WakeUp;
            LevelKit.SaveScene(scene, "Level7");
        }
    }
}
