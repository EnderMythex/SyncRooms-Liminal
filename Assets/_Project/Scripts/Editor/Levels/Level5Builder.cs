using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using O = SyncRooms.EditorTools.GeoBuilder.Opening;

namespace SyncRooms.EditorTools
{
    /// <summary>
    /// Level 5 — "Terror Hotel". 1930s hotel: marble lobby with reception and gramophone, damask corridors
    /// with numbered doors, the Beverly Room (doors on every surface + unfinished mahjong game), the boiler room,
    /// an endless mirrored corridor, and Room 237 whose window opens onto Level 94.
    /// </summary>
    public static class Level5Builder
    {
        const float T = 0.24f;
        static Transform _root, _props, _lights;
        static GeoBuilder _geo;
        static System.Random _rng;
        static float R() => (float)_rng.NextDouble();

        // spaces
        static readonly Rect Lobby = Rect.MinMaxRect(-10, 0, 10, 28);
        static readonly Rect East = Rect.MinMaxRect(10 + T, 12.5f, 50, 15.5f);
        static readonly Rect North = Rect.MinMaxRect(28.5f, 15.5f + T, 31.5f, 40);
        static readonly Rect Room237 = Rect.MinMaxRect(26, 40 + T, 34, 46.5f);
        static readonly Rect South = Rect.MinMaxRect(28.5f, -10, 31.5f, 12.5f - T);
        static readonly Rect Boiler = Rect.MinMaxRect(22, -24, 38, -10 - T);
        static readonly Rect Beverly = Rect.MinMaxRect(50 + T, 5, 68, 23);
        static readonly Rect West = Rect.MinMaxRect(-34, 12.5f, -10 - T, 15.5f);
        const float LobbyH = 4.5f, CorrH = 3.2f, BevH = 7f, BoilH = 5f, RoomH = 3f;

        [MenuItem("SyncRooms/Levels/Build Level 5 (Terror Hotel)")]
        public static void Build()
        {
            var scene = LevelKit.NewScene("Level5");
            _rng = new System.Random(5);
            _root = LevelKit.Group("Level5").transform;
            _props = LevelKit.Group("Props", _root).transform;
            _lights = LevelKit.Group("Lights", _root).transform;
            _geo = new GeoBuilder(16f);
            _geo.SetSurface("L5_HotelCarpet", SurfaceType.Carpet);
            _geo.SetSurface("L5_Marble", SurfaceType.Tile);
            _geo.SetSurface("L5_Parquet", SurfaceType.Wood);
            _geo.SetSurface("L5_OldConcrete", SurfaceType.Concrete);

            Shell();
            LobbyDress();
            Corridor(East, true, 201);
            Corridor(North, false, 301);
            Corridor(South, false, 101);
            Corridor(West, true, 401, mirrorEnd: false);
            BeverlyRoom();
            BoilerRoom();
            Room();
            _geo.Emit(_root, "Architecture");

            LevelKit.GlobalVolume(_root, "Level5", new LevelKit.Look
            {
                expMin = 6.5f, expMax = 9.5f, expComp = -0.4f, fogMFP = 40f, fogMaxHeight = 8f, fogAlbedo = new Color(1f, 0.93f, 0.82f),
                bloom = 0.35f, vignette = 0.3f, grain = 0.2f, saturation = -6f, contrast = 10f, temperature = 10f,
                skyType = 3, gradientTop = new Color(0.01f, 0.01f, 0.01f), gradientMid = new Color(0.01f, 0.01f, 0.01f), gradientBottom = new Color(0.01f, 0.01f, 0.01f),
                shadowDistance = 40f, chroma = 0.06f
            });

            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(_root, false);
            spawn.position = new Vector3(0, 0.05f, -0.45f);   // inside the elevator cab (arrival animation)
            spawn.rotation = Quaternion.identity;
            LevelKit.Player(_root, spawn.position, 0); // in front of the elevator (its scissor gate is closed)
            var ls = LevelKit.Settings(_root, spawn, 1.5f, 2f, new[] { "Ambience/amb_building_tone" }, 0.22f);
            ls.arrival = SyncRooms.Player.ArrivalKind.Elevator;
            var gate = LevelKit.Place("Elevator_Gate", new Vector3(0.6f, 0, -0.02f), 0, _props);
            if (gate)
            {
                gate.name = "ElevatorGate";
                foreach (var t in gate.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            }
            // endless corridors around the authored hotel
            var hotel = StreamedLevels.World<SyncRooms.Gen.HotelChunks>(_root, 24f, 2, 55, new Vector3(-58, 0, 2));
            hotel.excluded = Rect.MinMaxRect(-33.9f, -24.6f, 68.6f, 46.8f);
            hotel.forcedEastOpen = new Vector2Int(0, 0);
            SyncRooms.Gen.FollowProbe.Create(_root, new Vector3(30, 6, 30));
            var sp = StreamedLevels.Spawner(_root, 3, 30f, 30f, "Deathmoth");
            // service door in the boiler room → Level 2
            var bf = LevelKit.Place("Hotel_DoorFrame", new Vector3(30, 0, Boiler.yMin - T * 0.5f), 0, _props);
            if (bf)
            {
                var bl = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get("Hotel_DoorLeaf"), bf.transform);
                bl.transform.localPosition = new Vector3(0.475f, 0, 0);
                foreach (var t in bl.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                var ex = bl.AddComponent<LevelExit>();
                ex.targetScene = "Level2";
                ex.prompt = "[E] Porte de service — « TUYAUTERIE »";
                ex.exitKind = SyncRooms.Player.ExitKind.Walk;
                ex.anchor = bf.transform;
            }
            ls.GetComponent<LevelAmbience>().oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_whispers", minDelay = 40, maxDelay = 100, volume = 0.35f, minDist = 5, maxDist = 12 },
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_distant_thud", minDelay = 30, maxDelay = 80, volume = 0.45f, minDist = 10, maxDist = 25 },
            };
            Items();
            LevelKit.SaveScene(scene, "Level5");
        }

        // ------------------------------------------------------------------ geometry
        static List<O>[] Ops() => new[] { new List<O>(), new List<O>(), new List<O>(), new List<O>() };

        static void Shell()
        {
            var lo = Ops();
            lo[0].Add(new O(10, 1.2f, 2.3f));                 // elevator (S)
            lo[3].Add(new O(14, 2.0f, 3.0f));                 // → east corridor
            lo[2].Add(new O(14, 2.0f, 3.0f));                 // → west corridor
            _geo.Space(Lobby, 0, LobbyH, T, "L5_Marble", "L5_Damask", "L5_Plaster", lo, "L5_WoodPanel");
            _geo.Floor("L5_HotelCarpet", Rect.MinMaxRect(-1.6f, 0.4f, 1.6f, 23.6f), 0.006f);

            var eo = Ops();
            eo[2].Add(new O(1.5f, 2.0f, 3.0f));
            eo[3].Add(new O(1.5f, 1.0f, 2.2f));
            eo[1].Add(new O(30 - East.xMin, 3.0f, CorrH));
            eo[0].Add(new O(30 - East.xMin, 3.0f, CorrH));
            _geo.Space(East, 0, CorrH, T, "L5_HotelCarpet", "L5_Damask", "L5_Plaster", eo, "L5_WoodPanel");

            var no = Ops();
            no[0].Add(new O(1.5f, 3.0f, CorrH));
            no[1].Add(new O(1.5f, 1.0f, 2.2f));
            _geo.Space(North, 0, CorrH, T, "L5_HotelCarpet", "L5_Damask", "L5_Plaster", no, "L5_WoodPanel");

            var so = Ops();
            so[1].Add(new O(1.5f, 3.0f, CorrH));
            so[0].Add(new O(1.5f, 1.0f, 2.2f));
            _geo.Space(South, 0, CorrH, T, "L5_HotelCarpet", "L5_Damask", "L5_Plaster", so, "L5_WoodPanel");

            var wo = Ops();
            wo[3].Add(new O(1.5f, 2.0f, 3.0f));
            wo[2].Add(new O(1.5f, 3.0f, CorrH));             // opens onto the endless corridors
            _geo.Space(West, 0, CorrH, T, "L5_HotelCarpet", "L5_Damask", "L5_Plaster", wo, "L5_WoodPanel");

            var bo = Ops();
            bo[2].Add(new O(14 - Beverly.yMin, 1.0f, 2.2f));
            _geo.Space(Beverly, 0, BevH, T, "L5_Parquet", "L5_Damask", "L5_Plaster", bo, "L5_WoodPanel");

            var bb = Ops();
            bb[1].Add(new O(30 - Boiler.xMin, 1.0f, 2.2f));
            bb[0].Add(new O(30 - Boiler.xMin, 1.0f, 2.2f));   // service door → Level 2
            _geo.Space(Boiler, 0, BoilH, T, "L5_OldConcrete", "L5_Brick", "L5_OldConcrete", bb, "L5_Brick");

            var ro = Ops();
            ro[0].Add(new O(30 - Room237.xMin, 1.0f, 2.2f));
            ro[1].Add(new O(4f, 1.6f, 2.5f, 0.75f));          // the window
            _geo.Space(Room237, 0, RoomH, T, "L5_HotelCarpet", "L5_Damask", "L5_Plaster", ro, "PaintCream");

            // Doors on the real openings
            OpenDoor(new Vector3(East.xMax, 0, 14), -90, true);                          // → Beverly
            OpenDoor(new Vector3(30, 0, North.yMax), 180, false, "hotel_key");           // Room 237 (locked)
            OpenDoor(new Vector3(30, 0, South.yMin), 0, false);                          // → boiler
            LevelKit.Place("Elevator_ArtDeco", new Vector3(0, 0, 0), 0, _props);
            var el = LevelKit.PointLight(_lights, new Vector3(0, 2.1f, -0.45f), 450f, Color.white, 4f, false, 2900f);
        }

        /// <summary>Real, openable hotel door (frame centered in the wall thickness).</summary>
        static GameObject OpenDoor(Vector3 wallPoint, float yaw, bool startsOpen, string key = null)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var frame = LevelKit.Place("Hotel_DoorFrame", wallPoint - fwd * (T * 0.5f), yaw, _props);
            var leaf = Leaf(frame, key, startsOpen, 0);
            return leaf;
        }

        static GameObject Leaf(GameObject frame, string key, bool startsOpen, int number)
        {
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get("Hotel_DoorLeaf"), frame.transform);
            // Blender +X becomes Unity -X: the leaf extends toward -X from its hinge.
            leaf.transform.localPosition = new Vector3(0.475f, 0, 0);
            leaf.transform.localRotation = Quaternion.identity;
            foreach (var t in leaf.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var d = leaf.AddComponent<Door>();
            d.requiredKeyId = key;
            d.startsOpen = startsOpen;
            d.openAngle = startsOpen ? 75f : 95f;
            d.lockedMessage = key == "hotel_key" ? "Chambre 237. Fermée à clé." : "C'est verrouillé. Quelque chose respire derrière.";
            if (number > 0) Numbers(leaf.transform, number);
            return leaf;
        }

        static void Numbers(Transform leaf, int n)
        {
            string s = n.ToString();
            float spacing = 0.04f;
            for (int i = 0; i < s.Length; i++)
            {
                var d = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get("Digit_" + s[i]), leaf);
                float x = -0.475f + (i - (s.Length - 1) * 0.5f) * -spacing;
                d.transform.localPosition = new Vector3(x, 1.635f, 0.034f);
                d.transform.localRotation = Quaternion.identity;
                d.transform.localScale = Vector3.one * 1.7f;
                foreach (var c in d.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
        }

        /// <summary>Decorative locked door flush against a wall. `inward` = direction into the space.</summary>
        static void FakeDoor(Vector3 wallPoint, float yaw, int number, float y = 0f, Quaternion? extra = null)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var frame = LevelKit.Place("Hotel_DoorFrame", wallPoint + fwd * 0.03f + Vector3.up * y, yaw, _props);
            if (!frame) return;
            if (extra.HasValue) frame.transform.rotation = extra.Value;
            Leaf(frame, "never", false, number);
        }

        // ------------------------------------------------------------------ dressing
        static void Wainscot(Vector2 p0, Vector2 p1, float yaw, List<(float a, float b)> skip = null, bool crown = true, float h = 3.2f)
        {
            float len = (p1 - p0).magnitude;
            var dir = (p1 - p0) / len;
            for (float s = 0; s < len - 0.05f; s += 1f)
            {
                float seg = Mathf.Min(1f, len - s);
                float mid = s + seg * 0.5f;
                bool blocked = false;
                if (skip != null) foreach (var k in skip) if (mid > k.a - 0.5f && mid < k.b + 0.5f) blocked = true;
                var p = p0 + dir * mid;
                if (!blocked)
                {
                    var w = LevelKit.Place("Wainscot", new Vector3(p.x, 0, p.y), yaw, _props);
                    if (w && seg < 0.999f) w.transform.localScale = new Vector3(seg, 1, 1);
                }
                if (crown)
                {
                    var c = LevelKit.Place("CrownMolding", new Vector3(p.x, h, p.y), yaw, _props);
                    if (c && seg < 0.999f) c.transform.localScale = new Vector3(seg, 1, 1);
                }
            }
        }

        /// <summary>Dress the 4 walls of a rect (wainscot + crown), skipping opening ranges given in local "at" coordinates.</summary>
        static void DressWalls(Rect r, float h, List<(float a, float b)>[] skip, bool wainscot = true)
        {
            // yaw: object front (+Z) must face into the room
            var sides = new (Vector2 p0, Vector2 p1, float yaw)[]
            {
                (new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), 0f),
                (new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), 180f),
                (new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), 90f),
                (new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), -90f),
            };
            for (int i = 0; i < 4; i++)
            {
                var sd = sides[i];
                if (wainscot) Wainscot(sd.p0, sd.p1, sd.yaw, skip?[i], true, h);
            }
        }

        static void Sconce(Vector3 wallPoint, float yaw, bool flicker = false)
        {
            LevelKit.Place("Sconce_ArtDeco", wallPoint, yaw, _props);
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var l = LevelKit.PointLight(_lights, wallPoint + fwd * 0.14f + Vector3.up * 0.05f, 320f, Color.white, 6.5f, false, 2600f);
            var fl = l.gameObject.AddComponent<FlickerLight>();
            fl.lights = new[] { l };
            fl.emissiveRenderers = new Renderer[0];
            fl.flicker = flicker;
            fl.cullDistance = 24f;
        }

        static void Chandelier(Vector3 ceilingPoint, bool shadows, string model = "Chandelier_01")
        {
            LevelKit.Place(model, ceilingPoint + Vector3.down * 0.9f, R() * 360f, _props, 1.2f);
            LevelKit.Place("caged_hanging_light", ceilingPoint, 0, _props, 0.01f); // tiny anchor (keeps the chain point referenced)
            var l = LevelKit.PointLight(_lights, ceilingPoint + Vector3.down * 0.55f, 2600f, Color.white, 14f, shadows, 2700f);
            l.GetComponent<HDAdditionalLightData>().shapeRadius = 0.25f;
        }

        static void LobbyDress()
        {
            var skip = new[] { new List<(float, float)> { (9.4f, 10.6f) }, new List<(float, float)>(), new List<(float, float)> { (13, 15) }, new List<(float, float)> { (13, 15) } };
            DressWalls(Lobby, LobbyH, skip);
            for (int i = 0; i < 4; i++)
                for (int s = -1; s <= 1; s += 2)
                {
                    var c = LevelKit.Place("Column_ArtDeco", new Vector3(s * 5.5f, 0, 5 + i * 5f), 0, _props);
                    if (c) c.transform.localScale = new Vector3(1, LobbyH / 3.6f, 1);
                }
            for (int i = 0; i < 3; i++) Chandelier(new Vector3(0, LobbyH, 6 + i * 7.5f), i == 1, i == 1 ? "Chandelier_02" : "Chandelier_01");
            // reception
            LevelKit.Place("ReceptionDesk", new Vector3(0, 0, 24.6f), 180, _props);
            LevelKit.Place("Bookshelf_Hotel", new Vector3(-2.2f, 0, 27.78f), 180, _props);
            LevelKit.Place("Bookshelf_Hotel", new Vector3(2.2f, 0, 27.78f), 180, _props);
            LevelKit.Place("vintage_grandfather_clock_01", new Vector3(0, 0, 27.75f), 180, _props);
            LevelKit.Place("mantel_clock_01", new Vector3(-1.1f, 1.1f, 24.7f), 160, _props);
            // seating groups
            SeatGroup(new Vector3(-3.4f, 0, 9f), 90);
            SeatGroup(new Vector3(3.4f, 0, 17f), -90);
            LevelKit.Place("sofa_02", new Vector3(-9.35f, 0, 20f), 90, _props);
            LevelKit.Place("gothic_coffee_table", new Vector3(-7.9f, 0, 20f), 0, _props, 0.7f);
            LevelKit.Place("GreenChair_01", new Vector3(-7.6f, 0, 22.2f), 160, _props);
            // walls: cabinets, bookshelves, consoles, frames, mirror
            LevelKit.Place("vintage_cabinet_01", new Vector3(-9.64f, 0, 5f), 90, _props);
            LevelKit.Place("GothicCabinet_01", new Vector3(9.42f, 0, 21f), -90, _props);
            LevelKit.Place("Bookshelf_Hotel", new Vector3(-9.78f, 0, 9f), 90, _props);
            LevelKit.Place("Bookshelf_Hotel", new Vector3(-9.78f, 0, 10.3f), 90, _props);
            LevelKit.Place("ClassicConsole_01", new Vector3(9.7f, 0, 8f), -90, _props);
            LevelKit.Place("Gramophone", new Vector3(9.55f, 0.95f, 8f), -90, _props);
            var g = LevelKit.Audio3D(_props, new Vector3(9.4f, 1.3f, 8f), "Music/music_gramophone_1", 0.55f, 28f, false);
            var ta = g.gameObject.AddComponent<TimedAudio>();
            ta.clips = new[] { AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Resources/Audio/Music/music_gramophone_1.mp3"), AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Resources/Audio/Music/music_gramophone_2.mp3") };
            ta.minGap = 4; ta.maxGap = 12;
            LevelKit.Place("ClassicConsole_01", new Vector3(9.7f, 0, 3.2f), -90, _props);
            LevelKit.Place("marble_bust_01", new Vector3(9.6f, 0.95f, 3.2f), -90, _props);
            LevelKit.Place("antique_ceramic_vase_01", new Vector3(-9.3f, 0, 25.5f), 0, _props, 1.6f);
            LevelKit.Place("vintage_oil_lamp", new Vector3(1.2f, 1.1f, 24.5f), 0, _props, 0.6f);
            var lamp = LevelKit.PointLight(_lights, new Vector3(1.2f, 1.45f, 24.5f), 250f, Color.white, 5f, false, 2400f);
            Frame("fancy_picture_frame_01", new Vector3(-9.99f, 2.2f, 15.8f), 90);
            Frame("fancy_picture_frame_02", new Vector3(9.99f, 2.2f, 12f), -90);
            Frame("fancy_picture_frame_01", new Vector3(-9.99f, 2.2f, 3f), 90);
            Frame("ornate_mirror_01", new Vector3(9.99f, 1.6f, 17f), -90, 1.6f);
            // wall sconces
            for (int i = 0; i < 5; i++)
            {
                Sconce(new Vector3(-10f, 2.3f, 3 + i * 5.5f), 90);
                Sconce(new Vector3(10f, 2.3f, 3 + i * 5.5f), -90);
            }
            LevelKit.ReflectionProbe(_lights, new Vector3(0, LobbyH / 2, 14), new Vector3(20, LobbyH, 28));
        }

        static void Frame(string model, Vector3 p, float yaw, float scale = 1.3f)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            LevelKit.Place(model, p + fwd * 0.02f, yaw, _props, scale);
        }

        static void SeatGroup(Vector3 c, float yaw)
        {
            var rot = Quaternion.Euler(0, yaw, 0);
            LevelKit.Place("Sofa_01", c + rot * new Vector3(0, 0, -1.3f), yaw, _props);
            LevelKit.Place("CoffeeTable_01", c, yaw + 90, _props, 0.75f);
            LevelKit.Place("ArmChair_01", c + rot * new Vector3(-1.4f, 0, 0.3f), yaw + 120, _props);
            LevelKit.Place("ArmChair_01", c + rot * new Vector3(1.4f, 0, 0.3f), yaw - 120, _props);
            LevelKit.Place("Ottoman_01", c + rot * new Vector3(0, 0, 1.4f), yaw, _props, 0.8f);
            LevelKit.Place("tea_set_01", c + Vector3.up * 0.4f, R() * 360, _props, 0.6f);
        }

        /// <summary>Corridor dressing: wainscot, crown, sconces, numbered fake doors, frames.</summary>
        static void Corridor(Rect r, bool alongX, int firstNumber, bool mirrorEnd = false)
        {
            float len = alongX ? r.width : r.height;
            var skipA = new List<(float, float)>();
            var skipB = new List<(float, float)>();
            int number = firstNumber;
            for (float s = 2.5f; s < len - 2f; s += 4.5f)
            {
                bool nearJunction = alongX ? Mathf.Abs(r.xMin + s - 30f) < 2.6f : Mathf.Abs((r.yMin + s) - 14f) < 2.6f;
                if (nearJunction) continue;
                if (alongX)
                {
                    FakeDoor(new Vector3(r.xMin + s, 0, r.yMax), 180, number++);
                    FakeDoor(new Vector3(r.xMin + s + 2.2f, 0, r.yMin), 0, number++);
                    Sconce(new Vector3(r.xMin + s + 1.1f, 2.1f, r.yMax), 180, R() < 0.12f);
                    Sconce(new Vector3(r.xMin + s - 1.1f, 2.1f, r.yMin), 0, R() < 0.12f);
                    if (R() < 0.35f) Frame("fancy_picture_frame_0" + (1 + _rng.Next(2)), new Vector3(r.xMin + s - 1.1f, 2.0f, r.yMax), 180, 0.9f);
                }
                else
                {
                    FakeDoor(new Vector3(r.xMin, 0, r.yMin + s), 90, number++);
                    FakeDoor(new Vector3(r.xMax, 0, r.yMin + s + 2.2f), -90, number++);
                    Sconce(new Vector3(r.xMin, 2.1f, r.yMin + s + 1.1f), 90, R() < 0.12f);
                    Sconce(new Vector3(r.xMax, 2.1f, r.yMin + s - 1.1f), -90, R() < 0.12f);
                }
                skipA.Add((s - 0.7f, s + 0.7f));
                skipB.Add((s + 2.2f - 0.7f, s + 2.2f + 0.7f));
            }
            // wainscot along the long sides (openings are skipped by 'skip' lists)
            if (alongX)
            {
                skipA.Add((30 - r.xMin - 1.6f, 30 - r.xMin + 1.6f));
                skipB.Add((30 - r.xMin - 1.6f, 30 - r.xMin + 1.6f));
                Wainscot(new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), 180, skipA, true, CorrH);
                Wainscot(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), 0, skipB, true, CorrH);
            }
            else
            {
                Wainscot(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), 90, skipA, true, CorrH);
                Wainscot(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), -90, skipB, true, CorrH);
            }
            for (float s = 6f; s < len; s += 18f)
            {
                var c = alongX ? new Vector3(r.xMin + s, CorrH / 2, r.center.y) : new Vector3(r.center.x, CorrH / 2, r.yMin + s);
                LevelKit.ReflectionProbe(_lights, c, alongX ? new Vector3(9.2f, CorrH, r.height) : new Vector3(r.width, CorrH, 9.2f));
            }
            if (mirrorEnd)
            {
                // Full-height mirror at the far end: with a planar probe the corridor seems endless.
                var m = GameObject.CreatePrimitive(PrimitiveType.Quad);
                m.name = "Endless Mirror";
                m.transform.SetParent(_props, false);
                m.transform.position = new Vector3(r.xMin + 0.02f, CorrH / 2, r.center.y);
                m.transform.rotation = Quaternion.Euler(0, -90, 0);
                m.transform.localScale = new Vector3(r.height, CorrH, 1);
                m.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get("Mirror");
                Object.DestroyImmediate(m.GetComponent<Collider>());
                m.AddComponent<BoxCollider>().size = new Vector3(1, 1, 0.1f);
                var pp = new GameObject("Mirror Probe");
                pp.transform.SetParent(_lights, false);
                pp.transform.position = m.transform.position;
                pp.transform.rotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-90, 0, 0);
                var probe = pp.AddComponent<PlanarReflectionProbe>();
                probe.influenceVolume.shape = InfluenceShape.Box;
                probe.influenceVolume.boxSize = new Vector3(r.height + 0.2f, 0.5f, CorrH + 0.2f);
                probe.mode = ProbeSettings.Mode.Realtime;
                probe.realtimeMode = ProbeSettings.RealtimeMode.EveryFrame;
            }
        }

        static void BeverlyRoom()
        {
            var r = Beverly;
            var skip = new[] { new List<(float, float)>(), new List<(float, float)>(), new List<(float, float)> { (8, 10) }, new List<(float, float)>() };
            DressWalls(r, BevH, skip, false);
            int n = 500;
            // doors everywhere: along each wall, at floor level and floating higher up
            for (float s = 1.5f; s < r.width - 1f; s += 2.1f)
            {
                FakeDoor(new Vector3(r.xMin + s, 0, r.yMin), 0, n++);
                FakeDoor(new Vector3(r.xMin + s, 0, r.yMax), 180, n++);
                if (R() < 0.5f) FakeDoor(new Vector3(r.xMin + s + 1f, 0, r.yMax), 180, n++, 3.4f);
                if (R() < 0.5f) FakeDoor(new Vector3(r.xMin + s + 1f, 0, r.yMin), 0, n++, 3.6f);
            }
            for (float s = 1.5f; s < r.height - 1f; s += 2.1f)
            {
                if (Mathf.Abs(r.yMin + s - 14f) > 1.4f) FakeDoor(new Vector3(r.xMin, 0, r.yMin + s), 90, n++);
                FakeDoor(new Vector3(r.xMax, 0, r.yMin + s), -90, n++);
                if (R() < 0.5f) FakeDoor(new Vector3(r.xMax, 0, r.yMin + s + 1f), -90, n++, 3.5f);
            }
            // doors on the ceiling
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(Mathf.Lerp(r.xMin + 2, r.xMax - 2, R()), BevH - 0.03f, Mathf.Lerp(r.yMin + 2, r.yMax - 2, R()));
                FakeDoor(p, 0, n++, 0, Quaternion.Euler(-90, R() * 360, 0));
            }
            var c = r.center;
            LevelKit.Place("MahjongTable", new Vector3(c.x, 0, c.y), 0, _props);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                LevelKit.Place("GreenChair_01", new Vector3(c.x, 0, c.y) + dir * 0.95f, a + 180 + (i == 2 ? 25 : 0), _props);
            }
            for (int i = 0; i < 4; i++)
                Chandelier(new Vector3(c.x + (i % 2 - 0.5f) * 8f, BevH, c.y + (i / 2 - 0.5f) * 8f), false, "Chandelier_02");
            var tl = LevelKit.SpotLight(_lights, new Vector3(c.x, BevH - 0.3f, c.y), Quaternion.Euler(90, 0, 0), 1800f, Color.white, 12f, 32f, true, 2700f);
            LevelKit.Audio3D(_props, new Vector3(c.x, 1f, c.y), "Ambience/amb_whispers_2", 0.25f, 14f);
            LevelKit.ReflectionProbe(_lights, new Vector3(c.x, BevH / 2, c.y), new Vector3(r.width, BevH, r.height));
        }

        static void BoilerRoom()
        {
            var r = Boiler;
            var c = r.center;
            LevelKit.Place("Boiler", new Vector3(c.x, 0, c.y - 1f), 90, _props);
            LevelKit.Place("modular_industrial_pipes_01", new Vector3(r.xMin + 0.2f, 0, c.y - 3f), 90, _props, 1.5f);
            LevelKit.Place("modular_industrial_pipes_01", new Vector3(r.xMin + 0.2f, 0, c.y + 1f), 90, _props, 1.5f);
            LevelKit.Place("modular_industrial_pipes_01", new Vector3(r.xMax - 0.2f, 0, c.y - 2f), -90, _props, 1.5f);
            for (int i = 0; i < 5; i++) LevelKit.Place("Barrel_01", new Vector3(r.xMax - 1f - (i % 2) * 0.7f, 0, r.yMin + 1f + i * 0.65f), R() * 360, _props);
            LevelKit.Place("old_military_compressor", new Vector3(r.xMin + 2f, 0, r.yMin + 2f), 30, _props);
            LevelKit.Place("metal_tool_chest", new Vector3(r.xMin + 4f, 0, r.yMax - 0.6f), 180, _props);
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(c.x - 5 + i * 5, BoilH - 0.1f, c.y + 3.5f);
                LevelKit.Place("caged_hanging_light", p + Vector3.down * 0.75f, 0, _props, 0.8f);
                var l = LevelKit.PointLight(_lights, p + Vector3.down * 0.9f, 900f, Color.white, 11f, i == 1, 2100f);
                var fl = l.gameObject.AddComponent<FlickerLight>();
                fl.lights = new[] { l };
                fl.emissiveRenderers = new Renderer[0];
                fl.flicker = i == 2;
            }
            LevelKit.PointLight(_lights, new Vector3(c.x, 0.6f, c.y - 3.1f), 400f, new Color(1f, 0.35f, 0.1f), 5f, false); // firebox glow
            LevelKit.Audio3D(_props, new Vector3(c.x, 1.5f, c.y), "Ambience/amb_boiler", 0.7f, 24f);
            LevelKit.ReflectionProbe(_lights, new Vector3(c.x, BoilH / 2, c.y), new Vector3(r.width, BoilH, r.height));
        }

        static void Room()
        {
            var r = Room237;
            DressWalls(r, RoomH, new[] { new List<(float, float)> { (3.4f, 4.6f) }, new List<(float, float)> { (3, 5) }, new List<(float, float)>(), new List<(float, float)>() });
            LevelKit.Place("ArmChair_01", new Vector3(r.xMin + 1.5f, 0, r.yMax - 2f), 140, _props);
            LevelKit.Place("ClassicNightstand_01", new Vector3(r.xMax - 0.4f, 0, r.yMax - 1.5f), -90, _props);
            LevelKit.Place("vintage_oil_lamp", new Vector3(r.xMax - 0.4f, 0.7f, r.yMax - 1.5f), 0, _props, 0.5f);
            LevelKit.Place("vintage_suitcase", new Vector3(r.xMin + 1f, 0, r.yMin + 1f), 20, _props, 0.6f);
            LevelKit.Place("Ottoman_01", new Vector3(r.center.x + 1f, 0, r.center.y), 10, _props, 0.8f);
            // window onto Level 94: blinding daylight
            var win = GameObject.CreatePrimitive(PrimitiveType.Quad);
            win.name = "Window Light";
            win.transform.SetParent(_props, false);
            win.transform.position = new Vector3(r.xMin + 4f, 0.75f + 0.875f, r.yMax + 0.1f);
            win.transform.rotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(0, 180, 0);
            win.transform.localScale = new Vector3(1.6f, 1.75f, 1);
            win.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get("EmissiveSky");
            Object.DestroyImmediate(win.GetComponent<Collider>());
            var ex = new GameObject("Exit → Level 94 (window)");
            ex.transform.SetParent(_root, false);
            ex.transform.position = new Vector3(r.xMin + 4f, 1.6f, r.yMax);
            var bc = ex.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.6f, 1.75f, 0.3f);
            var le = ex.AddComponent<LevelExit>();
            le.targetScene = "Level94";
            le.prompt = "[E] Enjamber la fenêtre";
            le.exitKind = SyncRooms.Player.ExitKind.Window;
            le.transitionSfx = "SFX/sfx_door_open";
            LevelKit.RectLight(_lights, new Vector3(r.xMin + 4f, 1.62f, r.yMax - 0.05f), Quaternion.Euler(0, 180, 0), new Vector2(1.6f, 1.75f), 9000f, Color.white, 14f, true, 6500f);
            Sconce(new Vector3(r.xMin, 2.0f, r.center.y), 90, true);
            LevelKit.ReflectionProbe(_lights, new Vector3(r.center.x, RoomH / 2, r.center.y), new Vector3(r.width, RoomH, r.height));
        }

        static void Items()
        {
            var it = LevelKit.Group("Items", _root).transform;
            LevelKit.PlaceItem("hotel_key", new Vector3(0.9f, 1.13f, 24.85f), 30, it);
            LevelKit.PlaceItem("note_l5_1", new Vector3(-0.4f, 1.13f, 24.4f), -8, it);
            LevelKit.PlaceItem("almond_water", new Vector3(-3.4f, 0.45f, 9.1f), 0, it);
            LevelKit.PlaceItem("almond_water", new Vector3(40f, 0.1f, 14.8f), 0, it);
            LevelKit.PlaceItem("almond_water", new Vector3(58f, 0.85f, 14.3f), 0, it);
            LevelKit.PlaceItem("battery", new Vector3(24f, 0.1f, -11f), 0, it);
            LevelKit.PlaceItem("battery", new Vector3(-28f, 0.1f, 13.2f), 0, it);
            LevelKit.PlaceItem("medkit", new Vector3(26f, 0.1f, -22.5f), 20, it);
            LevelKit.PlaceItem("ration", new Vector3(36f, 0.1f, -12f), 0, it);
        }
    }
}
