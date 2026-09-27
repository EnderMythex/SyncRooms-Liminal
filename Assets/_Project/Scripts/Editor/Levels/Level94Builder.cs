using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>
    /// Level 94 — "Motion". A 1930s town on infinite grassy hills: plaza with an Almond Water fountain (exit to Level 37),
    /// radial streets with colourful cottages, siren poles playing cheerful music, a water tower, disembodied rooms standing
    /// alone in the fields, and a castle floating on a transparent mountain in the sky.
    /// </summary>
    public static class Level94Builder
    {
        static Transform _root, _props, _lights;
        static System.Random _rng;
        static float R() => (float)_rng.NextDouble();
        static int RI(int a, int b) => _rng.Next(a, b);
        const float Half = 320f;

        static readonly string[] Houses = { "House_Yellow", "House_Blue", "House_Green", "House_White", "House_Brick" };
        static readonly Dictionary<string, Vector2> HouseSize = new()
        {
            ["House_Yellow"] = new(7f, 9f), ["House_Blue"] = new(6.5f, 8.5f), ["House_Green"] = new(7.5f, 9.5f),
            ["House_White"] = new(7f, 10f), ["House_Brick"] = new(8f, 9f),
        };
        static readonly List<Light> Lamps = new();
        static readonly List<GameObject> Music = new();

        [MenuItem("SyncRooms/Levels/Build Level 94 (Motion)")]
        public static void Build()
        {
            var scene = LevelKit.NewScene("Level94");
            _rng = new System.Random(94);
            _root = LevelKit.Group("Level94").transform;
            _props = LevelKit.Group("Props", _root).transform;
            _lights = LevelKit.Group("Lights", _root).transform;
            Lamps.Clear(); Music.Clear();

            Terrain();
            Village();
            Countryside();
            Castle();

            var sun = LevelKit.Sun(_lights, new Vector3(38, -35, 0), 90000f, 5600f);
            var vol = LevelKit.GlobalVolume(_root, "Level94", new LevelKit.Look
            {
                expMin = 11f, expMax = 15.5f, expComp = 0.4f, fogMFP = 700f, fogBaseHeight = 0f, fogMaxHeight = 80f, fogAlbedo = new Color(0.9f, 0.93f, 1f),
                bloom = 0.25f, vignette = 0.22f, grain = 0.3f, saturation = 12f, contrast = 12f, temperature = 6f,
                skyType = (int)SkyType.Gradient, clouds = false, fog = false, shadowDistance = 80f, chroma = 0.04f, ssgi = false, skyExposure = 16.5f,
                gradientTop = new Color(0.22f, 0.42f, 0.85f), gradientMid = new Color(0.62f, 0.76f, 0.95f), gradientBottom = new Color(0.55f, 0.55f, 0.5f)
            });

            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(_root, false);
            spawn.position = new Vector3(_spawnPos.x, Height(_spawnPos.x, _spawnPos.y) + 0.1f, _spawnPos.y);
            spawn.rotation = Quaternion.Euler(0, _spawnYaw, 0);
            LevelKit.Player(_root, spawn.position, _spawnYaw);
            var ls = LevelKit.Settings(_root, spawn, 0.4f, 0f, new[] { "Ambience/amb_meadow", "Ambience/amb_wind" }, 0.35f);
            ls.arrival = SyncRooms.Player.ArrivalKind.FallIn;
            var dn = new GameObject("Day/Night").AddComponent<DayNightCycle>();
            dn.transform.SetParent(_root, false);
            dn.sun = sun; dn.volume = vol;
            dn.lamps = Lamps.ToArray();
            dn.musicSources = Music.ToArray();
            var night = StreamedLevels.Spawner(_root, 4, 5f, 12f, "Toon", "Toon", "Smiler");
            night.onlyAtNight = true;
            night.minDistance = 25f; night.maxDistance = 45f; night.despawnDistance = 90f;
            Items();
            Bounds();
            LevelKit.SaveScene(scene, "Level94");
        }

        // ------------------------------------------------------------------ terrain
        static float Height(float x, float z)
        {
            float d = new Vector2(x, z).magnitude;
            float town = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(16, 90, d));
            float hills = (Mathf.PerlinNoise(x * 0.008f + 11.3f, z * 0.008f + 3.1f) - 0.35f) * 26f
                        + (Mathf.PerlinNoise(x * 0.025f + 5f, z * 0.025f + 9f) - 0.5f) * 5f;
            float micro = (Mathf.PerlinNoise(x * 0.09f, z * 0.09f) - 0.5f) * 0.35f;
            return hills * town + micro * (0.3f + town);
        }

        static void Terrain()
        {
            var geoRoot = LevelKit.Group("Terrain", _root).transform;
            const float step = 2f;
            const int tile = 40; // quads per tile side
            float tileSize = tile * step;
            int tiles = Mathf.CeilToInt(Half * 2 / tileSize);
            for (int ti = 0; ti < tiles; ti++)
                for (int tj = 0; tj < tiles; tj++)
                {
                    float ox = -Half + ti * tileSize, oz = -Half + tj * tileSize;
                    var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
                    for (int j = 0; j <= tile; j++)
                        for (int i = 0; i <= tile; i++)
                        {
                            float x = ox + i * step, z = oz + j * step;
                            v.Add(new Vector3(x, Height(x, z), z));
                            uv.Add(new Vector2(x, z));
                        }
                    for (int j = 0; j < tile; j++)
                        for (int i = 0; i < tile; i++)
                        {
                            int a = j * (tile + 1) + i, b = a + 1, c = a + tile + 1, d = c + 1;
                            tris.AddRange(new[] { a, c, d, a, d, b });
                        }
                    var m = new Mesh { name = $"Terrain_{ti}_{tj}", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0);
                    m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
                    LevelKit.SaveMesh(m);
                    var go = new GameObject(m.name);
                    go.transform.SetParent(geoRoot, false);
                    go.AddComponent<MeshFilter>().sharedMesh = m;
                    go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get("L94_Grass");
                    go.AddComponent<MeshCollider>().sharedMesh = m;
                    go.AddComponent<SurfaceTag>().type = SurfaceType.Grass;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                }
        }

        // ------------------------------------------------------------------ town
        static GameObject Flat(string name, string mat, List<Vector2> outline, float y, SurfaceType st)
        {
            // fan-triangulated convex polygon (UV in meters)
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            foreach (var p in outline) { v.Add(new Vector3(p.x, y, p.y)); uv.Add(p); }
            for (int i = 1; i < outline.Count - 1; i++) t.AddRange(new[] { 0, i + 1, i });
            return MeshObject(name, mat, v, uv, t, st);
        }

        static GameObject MeshObject(string name, string mat, List<Vector3> v, List<Vector2> uv, List<int> t, SurfaceType st, bool collide = true)
        {
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            LevelKit.SaveMesh(m);
            var go = new GameObject(name);
            go.transform.SetParent(_props, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(mat);
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = m;
            go.AddComponent<SurfaceTag>().type = st;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
            return go;
        }

        static List<Vector2> Circle(float r, int n)
        {
            var l = new List<Vector2>();
            for (int i = 0; i < n; i++) { float a = i / (float)n * Mathf.PI * 2; l.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r)); }
            return l;
        }

        static void Ring(string name, string mat, float r0, float r1, float y, int n, SurfaceType st)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var p0 = d * r0; var p1 = d * r1;
                v.Add(new Vector3(p0.x, y, p0.y)); uv.Add(p0);
                v.Add(new Vector3(p1.x, y, p1.y)); uv.Add(p1);
                if (i < n) { int k = i * 2; t.AddRange(new[] { k, k + 3, k + 1, k, k + 2, k + 3 }); }
            }
            MeshObject(name, mat, v, uv, t, st);
        }

        /// <summary>Strip following the terrain between two points (roads, sidewalks), slightly raised.</summary>
        static void Strip(string name, string mat, Vector2 a, Vector2 b, float width, float lift, SurfaceType st)
        {
            var dir = (b - a).normalized; var n = new Vector2(-dir.y, dir.x);
            int segs = Mathf.CeilToInt((b - a).magnitude / 2f);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= segs; i++)
            {
                var c = Vector2.Lerp(a, b, i / (float)segs);
                var l = c + n * width * 0.5f; var r = c - n * width * 0.5f;
                v.Add(new Vector3(l.x, Height(l.x, l.y) + lift, l.y)); uv.Add(l);
                v.Add(new Vector3(r.x, Height(r.x, r.y) + lift, r.y)); uv.Add(r);
                if (i < segs) { int k = i * 2; t.AddRange(new[] { k, k + 2, k + 3, k, k + 3, k + 1 }); }
            }
            MeshObject(name, mat, v, uv, t, st);
        }

        static void Town()
        {
            // plaza + ring road
            Flat("Plaza", "L94_Sidewalk", Circle(16f, 64), 0.08f, SurfaceType.Concrete);
            Ring("RingRoad", "L94_Asphalt", 16f, 23f, 0.05f, 96, SurfaceType.Concrete);
            Ring("OuterSidewalk", "L94_Sidewalk", 23f, 25.5f, 0.1f, 96, SurfaceType.Concrete);
            var fountain = LevelKit.Place("Fountain", new Vector3(0, 0.08f, 0), 0, _props);
            FountainWater();
            LevelKit.Audio3D(_props, new Vector3(0, 1.5f, 0), "Ambience/amb_fountain", 0.8f, 30f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 30f;
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                LevelKit.Place("painted_wooden_bench", d * 10.5f + Vector3.up * 0.08f, a + 180, _props);
                LevelKit.Place("street_lamp_01", d * 14.8f + Vector3.up * 0.08f, a, _props);
            }
            // siren poles around the plaza
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                var p = Quaternion.Euler(0, a, 0) * Vector3.forward * 24.5f;
                LevelKit.Place("SirenPole", p + Vector3.up * Height(p.x, p.z), a, _props);
                var src = LevelKit.Audio3D(_props, p + Vector3.up * 9.5f, "Music/music_calliope_1", 0.9f, 90f, false);
                src.spatialBlend = 0.8f;
                var ta = src.gameObject.AddComponent<TimedAudio>();
                ta.clips = new[] { Clip("Music/music_calliope_1"), Clip("Music/music_calliope_2") };
                ta.minGap = 8f + i * 3f; ta.maxGap = 25f + i * 5f;
                Music.Add(src.gameObject);
            }
            // radial streets with houses
            for (int s = 0; s < 4; s++)
            {
                float a = s * 90f;
                var dir3 = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var dir = new Vector2(dir3.x, dir3.z);
                var side = new Vector2(dir.y, -dir.x);
                var start = dir * 23f; var end = dir * 125f;
                Strip($"Street{s}", "L94_Asphalt", start, end, 7f, 0.05f, SurfaceType.Concrete);
                Strip($"SidewalkL{s}", "L94_Sidewalk", start + side * 4.75f, end + side * 4.75f, 2.5f, 0.12f, SurfaceType.Concrete);
                Strip($"SidewalkR{s}", "L94_Sidewalk", start - side * 4.75f, end - side * 4.75f, 2.5f, 0.12f, SurfaceType.Concrete);
                int lot = 0;
                for (float t = 38f; t < 120f; t += 17f, lot++)
                    for (int k = -1; k <= 1; k += 2)
                    {
                        var c = dir * t + side * k * 16f;
                        float yaw = Mathf.Atan2(-side.x * k, -side.y * k) * Mathf.Rad2Deg; // front door faces the street
                        string house = Houses[RI(0, Houses.Length)];
                        var h = LevelKit.Place(house, new Vector3(c.x, Height(c.x, c.y) - 0.15f, c.y), yaw, _props);
                        if (h) DressHouse(h, house);
                        // front yard: fence, mailbox, path, props
                        var toStreet = -side * k;
                        var fenceC = c + toStreet * 9.2f;
                        for (int f = -2; f <= 2; f++)
                        {
                            if (f == 0) continue;
                            var fp = fenceC + dir * f * 2.05f;
                            LevelKit.Place("PicketFence", new Vector3(fp.x, Height(fp.x, fp.y), fp.y), a - 90, _props);
                        }
                        var mb = fenceC + dir * 1.3f + toStreet * 0.4f;
                        LevelKit.Place("Mailbox", new Vector3(mb.x, Height(mb.x, mb.y), mb.y), yaw, _props);
                        Strip($"Path{s}_{lot}_{k}", "L94_Path", c + toStreet * 5.5f + dir * -1.5f, fenceC + dir * -1.5f + toStreet * 0.3f, 1.3f, 0.04f, SurfaceType.Grass);
                        if (R() < 0.35f) { var g = c + toStreet * 7f + dir * 3f; LevelKit.Place("garden_gnome", new Vector3(g.x, Height(g.x, g.y), g.y), yaw + RI(-40, 40), _props); }
                        if (R() < 0.5f) { var sh = c + toStreet * 5.5f + dir * 4.5f; LevelKit.Place("shrub_02", new Vector3(sh.x, Height(sh.x, sh.y), sh.y), RI(0, 360), _props, 0.6f); }
                        if (R() < 0.3f) { var tr = c - toStreet * 3f + dir * 7f; LevelKit.Place("tree_small_02", new Vector3(tr.x, Height(tr.x, tr.y), tr.y), RI(0, 360), _props, 1.4f); }
                        if (R() < 0.3f) { var pb = c + toStreet * 5.7f - dir * 3.5f; LevelKit.Place("planter_box_01", new Vector3(pb.x, Height(pb.x, pb.y), pb.y), yaw, _props); }
                    }
                // street lamps
                for (float t = 32f; t < 122f; t += 20f)
                    for (int k = -1; k <= 1; k += 2)
                    {
                        var p = dir * t + side * k * 6.2f;
                        LevelKit.Place("street_lamp_01", new Vector3(p.x, Height(p.x, p.y) + 0.12f, p.y), a + (k > 0 ? -90 : 90), _props);
                        var sl = LevelKit.PointLight(_lights, new Vector3(p.x, Height(p.x, p.y) + 3.6f, p.y), 3500f, Color.white, 16f, false, 2800f);
                        sl.enabled = false;
                        Lamps.Add(sl);
                    }
                // parked vehicles
                var vp = dir * (50f + s * 13f) + side * 2.2f;
                LevelKit.Place(s == 1 ? "VintageVan" : "covered_car", new Vector3(vp.x, Height(vp.x, vp.y) + 0.05f, vp.y), a + (s == 1 ? 0 : 180), _props);
                var tc = dir * 42f + side * 5.6f;
                LevelKit.Place("metal_trash_can", new Vector3(tc.x, Height(tc.x, tc.y) + 0.12f, tc.y), a + 90, _props, 0.7f);
                var hy = dir * 60f - side * 5.4f;
                LevelKit.Place("fire_hydrant", new Vector3(hy.x, Height(hy.x, hy.y) + 0.12f, hy.y), a, _props);
            }
            LevelKit.Place("VintageVan", new Vector3(-12f, 0.05f, -19.5f), 70, _props);
            LevelKit.Place("WaterTower", new Vector3(62, Height(62, 58) - 0.2f, 58), 20, _props);
            LevelKit.Place("wooden_picnic_table", new Vector3(34, Height(34, 30), 30), 35, _props);
            LevelKit.Place("island_tree_01", new Vector3(38, Height(38, 36), 36), 0, _props, 1.6f);
            LevelKit.Place("island_tree_01", new Vector3(-36, Height(-36, 31), 31), 120, _props, 1.4f);
            LevelKit.Place("jacaranda_tree", new Vector3(-34, Height(-34, -36), -36), 40, _props, 0.55f);
            LevelKit.ReflectionProbe(_lights, new Vector3(0, 6, 0), new Vector3(60, 14, 60));
        }

        static AudioClip Clip(string res) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Resources/Audio/{res}.mp3");

        static void FountainWater()
        {
            // Milky Almond Water: HDRP water surface (pool) on a custom disc mesh.
            float y = 0.08f + 0.6f;
            var go = new GameObject("Fountain Water");
            go.transform.SetParent(_props, false);
            go.transform.position = new Vector3(0, y, 0);
            var disc = new List<Vector3> { Vector3.zero };
            var uv = new List<Vector2> { Vector2.zero };
            var tris = new List<int>();
            int n = 64;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2;
                disc.Add(new Vector3(Mathf.Cos(a) * 2.98f, 0, Mathf.Sin(a) * 2.98f));
                uv.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                tris.AddRange(new[] { 0, 1 + (i + 1) % n, 1 + i });
            }
            var m = new Mesh { name = "FountainDisc" };
            m.SetVertices(disc); m.SetUVs(0, uv); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            LevelKit.SaveMesh(m);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.enabled = false; // geometry only: the water system draws it
            var ws = go.AddComponent<WaterSurface>();
            ws.surfaceType = WaterSurfaceType.Pool;
            ws.geometryType = WaterGeometryType.Custom;
            ws.meshRenderers = new List<MeshRenderer> { mr };
            ws.refractionColor = new Color(0.92f, 0.86f, 0.72f);
            ws.scatteringColor = new Color(0.75f, 0.68f, 0.52f);
            ws.absorptionDistance = 0.35f;
            ws.maxRefractionDistance = 0.3f;
            ws.ripplesWindSpeed = 6f;
            ws.caustics = false;
            ws.startSmoothness = 0.93f;
            // wading + exit
            var zone = new GameObject("Fountain WaterZone");
            zone.transform.SetParent(_props, false);
            zone.transform.position = new Vector3(0, y - 0.4f, 0);
            var bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(4.2f, 0.8f, 4.2f);
            zone.AddComponent<WaterZone>().surfaceHeight = y;
            var exit = new GameObject("Exit → Level 37 (dive)");
            exit.transform.SetParent(_root, false);
            exit.transform.position = new Vector3(0, y, 0);
            var ec = exit.AddComponent<BoxCollider>();
            ec.isTrigger = true;
            ec.size = new Vector3(5.2f, 0.12f, 5.2f);
            var le = exit.AddComponent<LevelExit>();
            le.targetScene = "Level37";
            le.prompt = "[E] Plonger dans l'eau d'amande";
            le.transitionSfx = "SFX/sfx_splash_dive";
            le.exitKind = SyncRooms.Player.ExitKind.Dive;
        }

        /// <summary>Openable front door, furniture, a warm lamp and a safe zone (entities can't get you inside).</summary>
        static void DressHouse(GameObject h, string model)
        {
            var sz = HouseSize[model];
            float w = sz.x, d = sz.y, floorY = 0.59f;
            // hinge at Blender (door_x - 0.46, -d/2 + 0.09) -> Unity local (-x, z, -y)
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get("House_Door"), h.transform);
            leaf.transform.localPosition = new Vector3(w * 0.22f + 0.46f, 0.55f, d / 2 - 0.09f);
            leaf.transform.localRotation = Quaternion.identity;
            foreach (var t in leaf.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            leaf.AddComponent<Door>().openAngle = 95f;
            void Put(string prefab, Vector3 local, float yaw, float scale = 1f) =>
                LevelKit.Place(prefab, h.transform.TransformPoint(local), h.transform.eulerAngles.y + yaw, h.transform, scale);
            Put("sofa_02", new Vector3(0.6f, floorY, -d / 2 + 0.75f), 0);
            Put("CoffeeTable_01", new Vector3(0.6f, floorY, -d / 2 + 2.1f), 0, 0.6f);
            Put("ArmChair_01", new Vector3(-w / 2 + 1.1f, floorY, -0.5f), 90);
            Put("ClassicNightstand_01", new Vector3(w / 2 - 0.5f, floorY, -d / 2 + 0.6f), 180);
            Put("vintage_oil_lamp", new Vector3(w / 2 - 0.5f, floorY + 0.7f, -d / 2 + 0.6f), 0, 0.5f);
            Put("Bookshelf_Hotel", new Vector3(-w / 2 + 0.4f, floorY, -d / 2 + 1.6f), 90);
            LevelKit.PointLight(_lights, h.transform.TransformPoint(new Vector3(w / 2 - 0.5f, floorY + 1.15f, -d / 2 + 0.6f)), 700f, Color.white, 9f, false, 2400f);
            var zone = new GameObject("SafeZone (house)");
            zone.transform.SetParent(h.transform, false);
            zone.transform.localPosition = new Vector3(0, floorY + 1.3f, 0);
            var bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(w - 0.5f, 2.4f, d - 0.5f);
            zone.AddComponent<SyncRooms.Monsters.SafeZone>();
        }

        /// <summary>The castle floats closer, reachable by a floating glass stairway; its gate leads to Level 7.</summary>
        static void Castle()
        {
            var castle = LevelKit.Place("FloatingCastle", new Vector3(-150, 40, 205), 25, _props, 1f, true);
            if (!castle) return;
            var gate = castle.transform.TransformPoint(new Vector3(0, 0, 15.1f));
            var front = castle.transform.TransformPoint(new Vector3(0, 0, 18.5f));
            var start = new Vector3(-95, 0, 120);
            start.y = Height(start.x, start.z);
            var landing = new Vector3(front.x, 40f, front.z);
            int steps = Mathf.CeilToInt((landing.y - start.y) / 0.3f);
            var stairs = LevelKit.Group("Glass Stairway", _props).transform;
            var mat = MaterialLibrary.Get("GlassFrosted");
            var look = Quaternion.LookRotation(new Vector3(landing.x - start.x, 0, landing.z - start.z));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                var st = GameObject.CreatePrimitive(PrimitiveType.Cube);
                st.name = "Step";
                st.transform.SetParent(stairs, false);
                st.transform.position = Vector3.Lerp(start, landing, t) + Vector3.down * 0.1f;
                st.transform.rotation = look;
                st.transform.localScale = new Vector3(2.2f, 0.2f, 0.9f);
                st.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            var land = GameObject.CreatePrimitive(PrimitiveType.Cube);
            land.name = "Landing";
            land.transform.SetParent(stairs, false);
            land.transform.position = new Vector3(front.x, 39.9f, front.z);
            land.transform.rotation = castle.transform.rotation;
            land.transform.localScale = new Vector3(6f, 0.2f, 5f);
            land.GetComponent<MeshRenderer>().sharedMaterial = mat;
            LevelKit.PointLight(_lights, new Vector3(front.x, 42f, front.z), 2500f, Color.white, 14f, false, 3200f);
            var ex = new GameObject("Exit -> Level 7 (castle gate)");
            ex.transform.SetParent(_root, false);
            ex.transform.position = gate + Vector3.up * 2.5f;
            ex.transform.rotation = castle.transform.rotation;
            ex.AddComponent<BoxCollider>().size = new Vector3(4f, 5f, 1.2f);
            var le = ex.AddComponent<LevelExit>();
            le.targetScene = "Level7";
            le.prompt = "[E] Entrer dans le château";
            le.exitKind = SyncRooms.Player.ExitKind.Walk;
            le.transitionSfx = "SFX/sfx_door_open";
        }

        static Vector2 _spawnPos;
        static float _spawnYaw;

        /// <summary>
        /// The Motion village as in the wiki art: cottages scattered far apart on rolling hills, linked by winding
        /// dirt paths; picket fences, mailboxes, a water tower on the highest hill, a few tilted or floating houses.
        /// </summary>
        static void Village()
        {
            // village green with the Almond Water fountain
            Flat("Green", "L94_Sidewalk", Circle(11f, 64), 0.08f, SurfaceType.Concrete);
            LevelKit.Place("Fountain", new Vector3(0, 0.08f, 0), 0, _props);
            FountainWater();
            LevelKit.Audio3D(_props, new Vector3(0, 1.5f, 0), "Ambience/amb_fountain", 0.8f, 30f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 30f;
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                LevelKit.Place("painted_wooden_bench", d * 8f + Vector3.up * 0.08f, a + 180, _props);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                var p = Quaternion.Euler(0, a, 0) * Vector3.forward * 14f;
                LevelKit.Place("SirenPole", p + Vector3.up * Height(p.x, p.z), a, _props);
                var src = LevelKit.Audio3D(_props, p + Vector3.up * 9.5f, "Music/music_calliope_1", 0.9f, 110f, false);
                src.spatialBlend = 0.75f;
                var ta = src.gameObject.AddComponent<TimedAudio>();
                ta.clips = new[] { Clip("Music/music_calliope_1"), Clip("Music/music_calliope_2") };
                ta.minGap = 8f + i * 3f; ta.maxGap = 25f + i * 5f;
                Music.Add(src.gameObject);
            }
            // winding dirt paths radiating from the green
            int pathCount = 6;
            for (int k = 0; k < pathCount; k++)
            {
                float ang = k * 360f / pathCount + (float)_rng.NextDouble() * 25f;
                var pts = new List<Vector2>();
                var pos = new Vector2(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad)) * 10f;
                pts.Add(pos);
                float len = 150f + (float)_rng.NextDouble() * 60f;
                for (float t = 0; t < len; t += 7f)
                {
                    ang += ((float)_rng.NextDouble() - 0.5f) * 26f;
                    pos += new Vector2(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad)) * 7f;
                    pts.Add(pos);
                }
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var a = pts[i]; var b = pts[i + 1];
                    var dir = (b - a).normalized;
                    Strip($"Path{k}_{i}", "L94_Path", a - dir * 0.6f, b + dir * 0.6f, 2.3f, 0.05f, SurfaceType.Grass);
                    if (i % 4 == 2)
                    {
                        var side = new Vector2(-dir.y, dir.x) * 1.8f;
                        var lp = a + side;
                        LevelKit.Place("street_lamp_01", new Vector3(lp.x, Height(lp.x, lp.y), lp.y), 0, _props, 0.85f);
                        var sl = LevelKit.PointLight(_lights, new Vector3(lp.x, Height(lp.x, lp.y) + 3.1f, lp.y), 3000f, Color.white, 15f, false, 2800f);
                        sl.enabled = false;
                        Lamps.Add(sl);
                    }
                }
                if (k == 2) { _spawnPos = pts[^1]; var back = pts[^2] - pts[^1]; _spawnYaw = Mathf.Atan2(back.x, back.y) * Mathf.Rad2Deg; }
                // houses spaced out along the path
                int side0 = _rng.Next(2) * 2 - 1;
                for (int i = 4, n = 0; i < pts.Count - 1; i += 5 + _rng.Next(3), n++)
                {
                    var a = pts[i]; var dir = (pts[i + 1] - a).normalized;
                    int sd = (n % 2 == 0) ? side0 : -side0;
                    var nrm = new Vector2(-dir.y, dir.x) * sd;
                    var c = a + nrm * (14f + (float)_rng.NextDouble() * 6f);
                    PlaceCottage(c, a, dir);
                }
                // odd things lying around the path
                if (_rng.NextDouble() < 0.6) { var q = pts[pts.Count / 2] + new Vector2(3.5f, 2f); LevelKit.Place(_rng.NextDouble() < 0.5 ? "covered_car" : "VintageVan", new Vector3(q.x, Height(q.x, q.y) + 0.05f, q.y), _rng.Next(0, 360), _props); }
                if (_rng.NextDouble() < 0.4) { var q = pts[pts.Count / 3] + new Vector2(-4f, 1f); LevelKit.Place("wooden_picnic_table", new Vector3(q.x, Height(q.x, q.y), q.y), _rng.Next(0, 360), _props); }
            }
            // water tower on the highest nearby hilltop
            Vector2 best = new(60, 60); float bestH = -99;
            for (int i = 0; i < 120; i++)
            {
                float a = i * 3f * Mathf.Deg2Rad, r = 70f + (i % 5) * 12f;
                var q = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                if (Height(q.x, q.y) > bestH) { bestH = Height(q.x, q.y); best = q; }
            }
            LevelKit.Place("WaterTower", new Vector3(best.x, bestH - 0.3f, best.y), 20, _props);
            LevelKit.ReflectionProbe(_lights, new Vector3(0, 6, 0), new Vector3(40, 14, 40));
        }

        static void PlaceCottage(Vector2 c, Vector2 pathPoint, Vector2 pathDir)
        {
            string house = Houses[_rng.Next(Houses.Length)];
            var toPath = (pathPoint - c).normalized;
            float yaw = Mathf.Atan2(toPath.x, toPath.y) * Mathf.Rad2Deg;
            float roll = 0, pitch = 0, lift = 0;
            double odd = _rng.NextDouble();
            bool floating = false, tilted = false;
            if (tilted) { pitch = ((float)_rng.NextDouble() - 0.5f) * 14f; roll = ((float)_rng.NextDouble() - 0.5f) * 18f; }
            if (floating) lift = 2.5f + (float)_rng.NextDouble() * 3f;
            float y = Height(c.x, c.y) - 0.15f + lift;
            var h = LevelKit.Place(house, new Vector3(c.x, y, c.y), yaw, _props);
            if (!h) return;
            h.transform.rotation = Quaternion.Euler(pitch, yaw, roll);
            var sz = HouseSize[house];
            if (!floating)
            {
                // stone plinth hides the gap on slopes
                var pl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pl.name = "Plinth";
                pl.transform.SetParent(h.transform, false);
                pl.transform.localPosition = new Vector3(0, -1.2f, 0);
                pl.transform.localScale = new Vector3(sz.x + 0.1f, 2.6f, sz.y + 0.1f);
                pl.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get("L94_Stone");
                if (!tilted) DressHouse(h, house);
            }
            // front yard: spur path to the main path, fence, mailbox, garden
            var door = h.transform.TransformPoint(new Vector3(sz.x * 0.22f, 0, sz.y / 2 + 2.4f));
            var d2 = new Vector2(door.x, door.z);
            if (!floating) Strip($"Spur{c.x:0}", "L94_Path", d2, pathPoint, 1.2f, 0.04f, SurfaceType.Grass);
            var fenceC = c + toPath * (sz.y / 2 + 5f);
            var along = new Vector2(toPath.y, -toPath.x);
            float fyaw = Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg - 90f;
            for (int f = -3; f <= 3; f++)
            {
                if (Mathf.Abs(f) <= 0) continue;
                if (_rng.NextDouble() < 0.2) continue;
                var fp = fenceC + along * f * 2.05f + along * 1.0f;
                LevelKit.Place("PicketFence", new Vector3(fp.x, Height(fp.x, fp.y), fp.y), fyaw, _props);
            }
            var mb = pathPoint + (c - pathPoint).normalized * 2f + along * 1.2f;
            LevelKit.Place("Mailbox", new Vector3(mb.x, Height(mb.x, mb.y), mb.y), yaw, _props);
            if (_rng.NextDouble() < 0.4) { var g = c + toPath * (sz.y / 2 + 3f) + along * 3f; LevelKit.Place("garden_gnome", new Vector3(g.x, Height(g.x, g.y), g.y), yaw + _rng.Next(-40, 40), _props); }
            if (_rng.NextDouble() < 0.6) { var sh = c - along * (sz.x / 2 + 2.5f); LevelKit.Place("shrub_02", new Vector3(sh.x, Height(sh.x, sh.y), sh.y), _rng.Next(0, 360), _props, 0.6f); }
            if (_rng.NextDouble() < 0.5) { var tr = c - toPath * (sz.y / 2 + 4f) + along * 4f; LevelKit.Place("tree_small_02", new Vector3(tr.x, Height(tr.x, tr.y), tr.y), _rng.Next(0, 360), _props, 1.5f); }
        }

        // ------------------------------------------------------------------ countryside
        static void Countryside()
        {
            // trees on the hills
            for (int i = 0; i < 40; i++)
            {
                float a = R() * 360f, d = 140f + R() * 170f;
                var p = Quaternion.Euler(0, a, 0) * Vector3.forward * d;
                string tree = R() < 0.5f ? "island_tree_01" : R() < 0.5f ? "tree_small_02" : "jacaranda_tree";
                float sc = tree == "jacaranda_tree" ? 0.45f + R() * 0.2f : 1.2f + R() * 0.8f;
                LevelKit.Place(tree, new Vector3(p.x, Height(p.x, p.z) - 0.2f, p.z), R() * 360, _props, sc);
            }
            // grass clumps & flowers near the town edge (no shadows: cheap)
            for (int i = 0; i < 70; i++)
            {
                float a = R() * 360f, d = 60f + R() * 90f;
                var p = Quaternion.Euler(0, a, 0) * Vector3.forward * d;
                if (Mathf.Abs(p.x) < 9f || Mathf.Abs(p.z) < 9f) continue; // keep streets clear
                var g = LevelKit.Place(R() < 0.7f ? "grass_medium_02" : R() < 0.5f ? "grass_medium_01" : "dandelion_01",
                    new Vector3(p.x, Height(p.x, p.z) - 0.02f, p.z), R() * 360, _props, 0.9f + R() * 0.6f);
                if (g) foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 25; i++)
            {
                float a = R() * 360f, d = 130f + R() * 150f;
                var p = Quaternion.Euler(0, a, 0) * Vector3.forward * d;
                LevelKit.Place(R() < 0.5f ? "boulder_01" : "shrub_02", new Vector3(p.x, Height(p.x, p.z) - 0.3f, p.z), R() * 360, _props, 0.8f + R());
            }
            // disembodied rooms: fragments of interiors standing alone in the grass
            DisembodiedRoom(new Vector2(-150, 95), 30f);
            DisembodiedRoom(new Vector2(165, -120), -60f);
            DisembodiedRoom(new Vector2(-90, -175), 150f);
            DisembodiedRoom(new Vector2(200, 140), 10f);
        }

        static void DisembodiedRoom(Vector2 c, float yaw)
        {
            float y = Height(c.x, c.y);
            var g = new GameObject("Disembodied Room").transform;
            g.SetParent(_props, false);
            g.position = new Vector3(c.x, y, c.y);
            g.rotation = Quaternion.Euler(0, yaw, 0);
            var geo = new GeoBuilder(64f);
            // a patch of floor + one or two wall pieces with wallpaper and molding
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(g, false);
            floor.transform.localPosition = new Vector3(0, 0.05f, 0);
            floor.transform.localScale = new Vector3(5f, 0.1f, 4f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(R() < 0.5f ? "L5_Parquet" : "L4_Carpet");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(g, false);
            wall.transform.localPosition = new Vector3(0, 1.45f, 2f);
            wall.transform.localScale = new Vector3(5f, 2.9f, 0.18f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(R() < 0.5f ? "L5_Damask" : "L4_Wall");
            if (R() < 0.6f)
            {
                var w2 = Object.Instantiate(wall, g);
                w2.transform.localPosition = new Vector3(-2.5f, 1.45f, 0);
                w2.transform.localRotation = Quaternion.Euler(0, 90, 0);
                w2.transform.localScale = new Vector3(4f, 2.9f, 0.18f);
            }
            var sofa = LevelKit.Place(R() < 0.5f ? "Sofa_01" : "sofa_02", g.TransformPoint(new Vector3(0.3f, 0.1f, 1.4f)), yaw + 180, g);
            LevelKit.Place("vintage_oil_lamp", g.TransformPoint(new Vector3(1.9f, 0.1f, 1.6f)), yaw, g, 0.8f);
            if (R() < 0.6f) LevelKit.Place("fancy_picture_frame_0" + RI(1, 3), g.TransformPoint(new Vector3(-0.8f, 1.8f, 1.9f)), yaw + 180, g, 1.2f);
            if (R() < 0.5f) LevelKit.Place("Hotel_DoorFrame", g.TransformPoint(new Vector3(-1.6f, 0.1f, 2f)), yaw, g);
        }

        static void Items()
        {
            var it = LevelKit.Group("Items", _root).transform;
            LevelKit.PlaceItem("note_94_1", new Vector3(8f * Mathf.Sin(30 * Mathf.Deg2Rad), 0.65f, 8f * Mathf.Cos(30 * Mathf.Deg2Rad)), 0, it);
            LevelKit.PlaceItem("almond_water", new Vector3(-6f, 0.4f, -7f), 0, it);
            LevelKit.PlaceItem("almond_water", new Vector3(34.3f, Height(34, 30) + 0.9f, 30.2f), 0, it);
            LevelKit.PlaceItem("ration", new Vector3(33.7f, Height(34, 30) + 0.9f, 29.8f), 0, it);
            LevelKit.PlaceItem("battery", new Vector3(_spawnPos.x + 1f, Height(_spawnPos.x + 1f, _spawnPos.y) + 0.4f, _spawnPos.y), 0, it);
        }

        static void Bounds()
        {
            var b = LevelKit.Group("Bounds", _root).transform;
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject("Edge").AddComponent<BoxCollider>();
                w.transform.SetParent(b, false);
                w.transform.position = Quaternion.Euler(0, i * 90, 0) * new Vector3(0, 20, Half - 20);
                w.transform.rotation = Quaternion.Euler(0, i * 90, 0);
                w.size = new Vector3(Half * 2, 80, 2);
            }
        }
    }
}
