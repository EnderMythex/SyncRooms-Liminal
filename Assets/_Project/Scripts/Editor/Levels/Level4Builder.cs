using System.Collections.Generic;
using System.Linq;
using SyncRooms.Player;
using SyncRooms.UI;
using SyncRooms.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>
    /// Level 4 — "Abandoned Office". BSP-subdivided office floor: open-plan cubicle farms, private offices,
    /// break room with Almond Water dispensers, storage, conference rooms. Drop ceiling with fluorescent troffers
    /// (some dead, some flickering), blacked-out windows, locked stairwell B (keycard) leading to Level 5.
    /// </summary>
    public class Level4Builder
    {
        const float Cell = 1.2f;
        const float H = 2.7f;          // ceiling height
        const float TInt = 0.12f, TExt = 0.3f;

        class Split { public bool alongZ; public int pos, a, b; public readonly List<float> openings = new(); public readonly List<bool> doors = new(); }
        class Room
        {
            public RectInt cells;
            public Rect r;                 // interior rect in meters
            public string type;
            public int dist = -1;
            public readonly List<Vector2> doors = new();
            public readonly List<Rect> used = new();
            public readonly List<Room> links = new();
            public float Area => r.width * r.height;
        }

        readonly System.Random _rng;
        readonly int _w, _d;
        readonly bool _menu;
        readonly List<Split> _splits = new();
        readonly List<Room> _rooms = new();
        Transform _root, _props, _lights;
        GeoBuilder _geo;

        float R() => (float)_rng.NextDouble();
        int RI(int a, int b) => _rng.Next(a, b);
        T Pick<T>(IList<T> l) => l[_rng.Next(l.Count)];

        public Level4Builder(int seed, int wCells, int dCells, bool menu)
        {
            _rng = new System.Random(seed);
            _w = wCells; _d = dCells; _menu = menu;
        }

        [MenuItem("SyncRooms/Levels/Build Level 4 (Abandoned Office)")]
        public static void BuildMenu() => Build();

        public static void Build()
        {
            var scene = LevelKit.NewScene("Level4");
            new Level4Builder(404, 46, 38, false).Generate();
            LevelKit.SaveScene(scene, "Level4");
        }

        public static void BuildMainMenu()
        {
            var scene = LevelKit.NewScene("MainMenu");
            new Level4Builder(7, 22, 14, true).Generate();
            LevelKit.SaveScene(scene, "MainMenu");
        }

        public void Generate()
        {
            _root = LevelKit.Group("Level4").transform;
            _props = LevelKit.Group("Props", _root).transform;
            _lights = LevelKit.Group("Lights", _root).transform;
            _geo = new GeoBuilder(14.4f);
            _geo.SetSurface("L4_Carpet", SurfaceType.Carpet);
            _geo.SetSurface("L4_Tile", SurfaceType.Tile);
            _geo.SetSurface("L4_Concrete", SurfaceType.Concrete);

            Bsp(new RectInt(0, 0, _w, _d), 0);
            BuildRooms();
            PlaceOpenings();
            LinkRooms();
            AssignTypes();
            BuildShell();
            foreach (var room in _rooms) Furnish(room);
            foreach (var room in _rooms) Ceiling(room);
            _geo.Emit(_root, "Architecture");

            var look = new LevelKit.Look
            {
                expMin = 7f, expMax = 10.5f, expComp = -0.4f, fogMFP = 35f, fogMaxHeight = 3f, fogAlbedo = new Color(0.95f, 0.95f, 0.86f),
                bloom = 0.3f, vignette = 0.28f, grain = 0.22f, saturation = -12f, contrast = 6f, temperature = 6f, tint = -4f,
                skyType = 3, gradientTop = new Color(0.02f, 0.02f, 0.02f), gradientMid = new Color(0.02f, 0.02f, 0.02f), gradientBottom = new Color(0.01f, 0.01f, 0.01f),
                shadowDistance = 35f, chroma = 0.08f
            };
            LevelKit.GlobalVolume(_root, "Level4", look);

            if (_menu) { MenuCamera(); return; }

            var start = _rooms.First(r => r.type == "start");
            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(_root, false);
            spawn.position = new Vector3(start.r.center.x, 0.05f, start.r.center.y);
            spawn.rotation = Quaternion.Euler(0, 90, 0);
            LevelKit.Player(_root, spawn.position, 90);
            var ls = LevelKit.Settings(_root, spawn, 1.0f, 1.5f, new[] { "Ambience/amb_office_hvac", "Ambience/amb_fluorescent_hum" }, 0.28f);
            var amb = ls.GetComponent<LevelAmbience>();
            amb.oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_distant_thud", minDelay = 25, maxDelay = 70, volume = 0.5f, minDist = 12, maxDist = 25 },
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_drip", minDelay = 12, maxDelay = 40, volume = 0.35f, minDist = 4, maxDist = 12 },
                new LevelAmbience.RandomOneShot { clip = "SFX/sfx_light_flicker", minDelay = 30, maxDelay = 80, volume = 0.3f, minDist = 6, maxDist = 14 },
            };
            PlaceItems();
        }

        // ------------------------------------------------------------ layout
        void Bsp(RectInt r, int depth)
        {
            bool canZ = r.width >= 10, canX = r.height >= 10;
            bool stop = (!canZ && !canX) || (depth > 2 && r.width * r.height < 170 && R() < 0.45f);
            if (stop) { _rooms.Add(new Room { cells = r }); return; }
            bool alongZ = canZ && (!canX || (r.width > r.height ? R() < 0.8f : R() < 0.2f)); // wall along Z splits X
            if (alongZ)
            {
                int x = RI(r.xMin + 5, r.xMax - 4);
                _splits.Add(new Split { alongZ = true, pos = x, a = r.yMin, b = r.yMax });
                Bsp(new RectInt(r.xMin, r.yMin, x - r.xMin, r.height), depth + 1);
                Bsp(new RectInt(x, r.yMin, r.xMax - x, r.height), depth + 1);
            }
            else
            {
                int z = RI(r.yMin + 5, r.yMax - 4);
                _splits.Add(new Split { alongZ = false, pos = z, a = r.xMin, b = r.xMax });
                Bsp(new RectInt(r.xMin, r.yMin, r.width, z - r.yMin), depth + 1);
                Bsp(new RectInt(r.xMin, z, r.width, r.yMax - z), depth + 1);
            }
        }

        void BuildRooms()
        {
            foreach (var room in _rooms)
            {
                var c = room.cells;
                float x0 = c.xMin * Cell + (c.xMin == 0 ? TExt / 2 : TInt / 2);
                float x1 = c.xMax * Cell - (c.xMax == _w ? TExt / 2 : TInt / 2);
                float z0 = c.yMin * Cell + (c.yMin == 0 ? TExt / 2 : TInt / 2);
                float z1 = c.yMax * Cell - (c.yMax == _d ? TExt / 2 : TInt / 2);
                room.r = Rect.MinMaxRect(x0, z0, x1, z1);
            }
        }

        void PlaceOpenings()
        {
            foreach (var s in _splits)
            {
                // Junctions = endpoints of other splits touching this line.
                var blocked = new List<int> { s.a, s.b };
                foreach (var o in _splits)
                {
                    if (o == s || o.alongZ == s.alongZ) continue;
                    if (o.pos > s.a && o.pos < s.b && (o.a == s.pos || o.b == s.pos)) blocked.Add(o.pos);
                }
                blocked.Sort();
                int count = (s.b - s.a) > 18 ? 2 : 1;
                var spans = new List<(int, int)>();
                for (int i = 0; i < blocked.Count - 1; i++)
                    if (blocked[i + 1] - blocked[i] >= 3) spans.Add((blocked[i], blocked[i + 1]));
                // make sure each sub-span between junctions gets an opening (keeps every room reachable)
                foreach (var (a, b) in spans)
                {
                    for (int k = 0; k < (b - a > 14 ? count : 1); k++)
                    {
                        float at = (a + 1 + R() * (b - a - 2)) * Cell;
                        bool door = R() < 0.55f;
                        s.openings.Add(at);
                        s.doors.Add(door);
                    }
                }
            }
        }

        Room RoomAt(Vector2 p) => _rooms.FirstOrDefault(r => r.r.Contains(p));

        void LinkRooms()
        {
            foreach (var s in _splits)
            {
                for (int i = 0; i < s.openings.Count; i++)
                {
                    float at = s.openings[i];
                    Vector2 p = s.alongZ ? new Vector2(s.pos * Cell, at) : new Vector2(at, s.pos * Cell);
                    Vector2 off = s.alongZ ? Vector2.right * 0.5f : Vector2.up * 0.5f;
                    var ra = RoomAt(p - off);
                    var rb = RoomAt(p + off);
                    if (ra == null || rb == null) continue;
                    ra.links.Add(rb); rb.links.Add(ra);
                    ra.doors.Add(p); rb.doors.Add(p);
                }
            }
        }

        void AssignTypes()
        {
            // start room: smallest room touching the west wall
            var west = _rooms.Where(r => r.cells.xMin == 0).OrderBy(r => r.Area).ToList();
            var start = west.Count > 0 ? west[0] : _rooms[0];
            start.type = "start";
            var q = new Queue<Room>();
            start.dist = 0; q.Enqueue(start);
            while (q.Count > 0)
            {
                var r = q.Dequeue();
                foreach (var n in r.links)
                    if (n.dist < 0) { n.dist = r.dist + 1; q.Enqueue(n); }
            }
            bool hasBreak = false;
            foreach (var r in _rooms.Where(r => r.type == null).OrderByDescending(r => r.Area))
            {
                if (r.Area > 150f) r.type = "openplan";
                else if (r.Area > 60f)
                {
                    if (!hasBreak) { r.type = "break"; hasBreak = true; }
                    else r.type = Pick(new[] { "conference", "openplan", "break", "storage" });
                }
                else r.type = Pick(new[] { "office", "office", "office", "storage", "copy" });
            }
        }

        // ------------------------------------------------------------ shell
        void BuildShell()
        {
            // floors
            foreach (var room in _rooms)
            {
                string fm = room.type == "break" ? "L4_Tile" : "L4_Carpet";
                var c = room.cells;
                _geo.Floor(fm, new Rect(c.xMin * Cell, c.yMin * Cell, c.width * Cell, c.height * Cell), 0f);
            }
            // interior walls
            foreach (var s in _splits)
            {
                var ops = new List<(float, float, float, float)>();
                for (int i = 0; i < s.openings.Count; i++)
                {
                    float at = s.openings[i] - s.a * Cell;
                    ops.Add(s.doors[i] ? (at, 1.0f, 0f, 2.12f) : (at, 1.6f, 0f, 2.25f));
                    var world = s.alongZ ? new Vector3(s.pos * Cell, 0, s.openings[i]) : new Vector3(s.openings[i], 0, s.pos * Cell);
                    if (s.doors[i]) PlaceDoor(world, s.alongZ ? 90 : 0, null);
                }
                var from = s.alongZ ? new Vector2(s.pos * Cell, s.a * Cell) : new Vector2(s.a * Cell, s.pos * Cell);
                var to = s.alongZ ? new Vector2(s.pos * Cell, s.b * Cell) : new Vector2(s.b * Cell, s.pos * Cell);
                _geo.Wall("L4_Wall", "L4_Wall", from, to, TInt, 0f, H, ops, "L4_Wall", "PlasticBlack");
            }
            // exterior walls with blacked-out windows (+ the stairwell door on the east side)
            float W = _w * Cell, D = _d * Cell;
            var eastRooms = _rooms.Where(r => r.cells.xMax == _w).OrderByDescending(r => r.dist).ToList();
            var stairRoom = _menu ? null : eastRooms.FirstOrDefault();
            float stairZ = stairRoom != null ? stairRoom.r.center.y : -99f;
            if (stairRoom != null) stairRoom.doors.Add(new Vector2(W, stairZ));

            ExteriorWall(new Vector2(0, 0), new Vector2(W, 0), 0, -1);          // south
            ExteriorWall(new Vector2(W, D), new Vector2(0, D), 180, -1);        // north
            ExteriorWall(new Vector2(0, D), new Vector2(0, 0), 90, -1);         // west
            ExteriorWall(new Vector2(W, 0), new Vector2(W, D), -90, stairZ);    // east
            if (stairRoom != null) Stairwell(new Vector2(W, stairZ));
        }

        void ExteriorWall(Vector2 from, Vector2 to, float yaw, float doorAlong)
        {
            var dir = (to - from).normalized;
            float len = (to - from).magnitude;
            var ops = new List<(float, float, float, float)>();
            // interior wall junctions on this side
            var junctions = new List<float>();
            foreach (var s in _splits)
            {
                Vector2 e1 = s.alongZ ? new Vector2(s.pos * Cell, s.a * Cell) : new Vector2(s.a * Cell, s.pos * Cell);
                Vector2 e2 = s.alongZ ? new Vector2(s.pos * Cell, s.b * Cell) : new Vector2(s.b * Cell, s.pos * Cell);
                foreach (var e in new[] { e1, e2 })
                {
                    var rel = e - from;
                    if (Mathf.Abs(rel.x * dir.y - rel.y * dir.x) < 0.01f) junctions.Add(Vector2.Dot(rel, dir));
                }
            }
            float doorAt = -1;
            if (doorAlong > -50f)
            {
                doorAt = Vector2.Dot(new Vector2(from.x, doorAlong) - from, dir);
                ops.Add((doorAt, 1.0f, 0f, 2.12f));
            }
            if (!_menu)
            {
                for (float at = 2.4f; at < len - 1.8f; at += 3.6f)
                {
                    if (junctions.Any(j => Mathf.Abs(j - at) < 1.1f)) continue;
                    if (doorAt > 0 && Mathf.Abs(doorAt - at) < 1.8f) continue;
                    ops.Add((at, 1.4f, 0.9f, 2.1f));
                    var p = from + dir * at;
                    LevelKit.Place("OfficeWindow", new Vector3(p.x, 0.9f, p.y), yaw, _props);
                }
            }
            // interior face should look toward the level: left side of from->to
            _geo.Wall("L4_Wall", "L4_Concrete", from, to, TExt, 0f, H, ops, "PaintOffWhite", "PlasticBlack");
        }

        GameObject PlaceDoor(Vector3 pos, float yaw, string keyId, bool locked = false)
        {
            var frame = LevelKit.Place("Office_DoorFrame", pos, yaw, _props);
            if (!frame) return null;
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(ModelLibrary.Get("Office_DoorLeaf"), frame.transform);
            leaf.transform.localPosition = new Vector3(0.44f, 0, 0); // Blender +X becomes Unity -X: the leaf extends toward -X from its hinge
            leaf.transform.localRotation = Quaternion.identity;
            foreach (var t in leaf.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var door = leaf.AddComponent<Door>();
            door.requiredKeyId = keyId;
            door.lockedMessage = keyId != null ? "ESCALIER B — Accès réservé. Badge requis." : "C'est verrouillé.";
            door.startsOpen = keyId == null && R() < 0.35f;
            if (door.startsOpen) door.openAngle = Random.Range(60f, 100f);
            return leaf;
        }

        void Stairwell(Vector2 doorPos)
        {
            float x0 = doorPos.x + TExt / 2, z = doorPos.y;
            PlaceDoor(new Vector3(doorPos.x, 0, z), 90, "keycard_l4");
            LevelKit.Place("ExitSign", new Vector3(doorPos.x - TExt / 2 - 0.04f, 2.62f, z), -90, _props);
            // landing
            _geo.Floor("L4_Concrete", new Rect(x0, z - 1.5f, 2.0f, 3.0f), 0f);
            _geo.Floor("L4_Concrete", new Rect(x0, z - 1.5f, 7.8f, 3.0f), H, false);
            int steps = 12; float run = 0.26f, rise = 0.17f;
            for (int i = 0; i < steps; i++)
            {
                float sx = x0 + 2.0f + i * run;
                _geo.Box("L4_Concrete", new Vector3(sx, -(i + 1) * rise - 0.3f, z - 1.5f), new Vector3(sx + run, -i * rise, z + 1.5f));
            }
            float bx = x0 + 2.0f + steps * run;
            float by = -steps * rise;
            _geo.Floor("L4_Concrete", new Rect(bx, z - 1.5f, 2.2f, 3.0f), by);
            // walls
            _geo.Box("L4_Concrete", new Vector3(x0, by - 0.5f, z - 1.75f), new Vector3(bx + 2.4f, H, z - 1.5f));
            _geo.Box("L4_Concrete", new Vector3(x0, by - 0.5f, z + 1.5f), new Vector3(bx + 2.4f, H, z + 1.75f));
            _geo.Box("L4_Concrete", new Vector3(bx + 2.2f, by - 0.5f, z - 1.5f), new Vector3(bx + 2.4f, H, z + 1.5f));
            // handrail
            _geo.Box("Steel", new Vector3(x0 + 2f, 0.9f - 0.02f, z + 1.42f), new Vector3(bx, 0.95f, z + 1.46f));
            var l = LevelKit.PointLight(_lights, new Vector3(x0 + 1f, 2.3f, z), 700f, Color.white, 8f, true, 3600f);
            var fl = LevelKit.Place("industrial_caged_sconce", new Vector3(x0 + 1f, 2.35f, z + 1.45f), 180, _props, 0.4f);
            var flick = l.gameObject.AddComponent<FlickerLight>();
            flick.lights = new[] { l };
            flick.emissiveRenderers = new Renderer[0];
            flick.flicker = true;
            flick.flickerChance = 0.6f;
            flick.buzz = LevelKit.Audio3D(l.transform, l.transform.position, "SFX/sfx_light_flicker", 0.3f, 8f);
            LevelKit.PointLight(_lights, new Vector3(bx + 1f, by + 2.2f, z), 250f, new Color(1f, 0.6f, 0.4f), 6f, false);

            var trig = new GameObject("Exit → Level 5");
            trig.transform.SetParent(_root, false);
            trig.transform.position = new Vector3(bx + 1.1f, by + 1f, z);
            var bc = trig.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(1.8f, 2f, 2.8f);
            var ex = trig.AddComponent<LevelExit>();
            ex.targetScene = "Level5";
            ex.isTrigger = true;
            ex.transitionSfx = "SFX/sfx_door_open";
            var note = LevelKit.Place("Item_Note", new Vector3(x0 + 0.6f, 0.001f, z - 1.2f), 25, _props);
            if (note)
            {
                var rn = note.AddComponent<ReadableNote>();
                rn.title = "Griffonné au sol";
                rn.text = "L'escalier ne descend pas vers le rez-de-chaussée.\n\nIl descend vers un HÔTEL.\n\nJe l'ai entendu : de la musique, un vieux disque. Ça sent la cire et la poussière.\n\nSi tu descends, ne remonte jamais par le même chemin.";
                note.AddComponent<BoxCollider>().size = new Vector3(0.15f, 0.02f, 0.21f);
            }
        }

        // ------------------------------------------------------------ ceiling & lights
        void Ceiling(Room room)
        {
            var c = room.cells;
            _geo.Floor("L4_Ceiling", new Rect(c.xMin * Cell, c.yMin * Cell, c.width * Cell, c.height * Cell), H, false);
            var r = room.r;
            int nx = Mathf.Max(1, Mathf.FloorToInt((r.width - 0.6f) / 2.4f));
            int nz = Mathf.Max(1, Mathf.FloorToInt((r.height - 0.6f) / 3.6f));
            float sx = r.center.x - (nx - 1) * 1.2f, sz = r.center.y - (nz - 1) * 1.8f;
            float deadChance = room.type == "storage" ? 0.4f : room.type == "start" ? 0f : 0.12f;
            int k = 0;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++, k++)
                {
                    var p = new Vector3(sx + i * 2.4f, H, sz + j * 3.6f);
                    var t = LevelKit.Place("Troffer", p, 0, _props);
                    if (!t) continue;
                    var rend = t.GetComponentInChildren<MeshRenderer>();
                    int emIdx = System.Array.FindIndex(rend.sharedMaterials, m => m && m.name == "EmissivePanel");
                    bool dead = R() < deadChance;
                    if (dead)
                    {
                        var mats = rend.sharedMaterials;
                        mats[emIdx] = MaterialLibrary.Get("PlasticWhite");
                        rend.sharedMaterials = mats;
                        continue;
                    }
                    bool flicker = R() < 0.07f || (room.type == "storage" && R() < 0.3f);
                    bool hasLight = (i + j) % 2 == 0 || nx * nz <= 2 || flicker;
                    if (!hasLight) continue;
                    var l = LevelKit.PointLight(_lights, p + Vector3.down * 0.2f, 1700f, Color.white, 7.5f, false, 4700f);
                    var fl = l.gameObject.AddComponent<FlickerLight>();
                    fl.lights = new[] { l };
                    fl.emissiveRenderers = flicker ? new Renderer[] { rend } : new Renderer[0];
                    fl.emissiveMaterialIndex = emIdx;
                    fl.flicker = flicker;
                    fl.cullDistance = 26f;
                    if (flicker)
                    {
                        foreach (var tr in t.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
                        fl.buzz = LevelKit.Audio3D(l.transform, l.transform.position, "SFX/sfx_light_flicker", 0.3f, 9f);
                    }
                }
            // a few return-air vents between troffers
            if (r.width > 4 && r.height > 4 && R() < 0.7f)
                LevelKit.Place("CeilingVent", new Vector3(r.xMin + 1.8f, H, r.yMin + 1.2f), 0, _props);
            var probe = LevelKit.ReflectionProbe(_lights, new Vector3(r.center.x, H * 0.5f, r.center.y), new Vector3(r.width + 0.2f, H, r.height + 0.2f));
            probe.name = "Probe " + room.type;
        }

        // ------------------------------------------------------------ furniture
        bool Free(Room room, Rect fp)
        {
            if (fp.xMin < room.r.xMin - 0.01f || fp.xMax > room.r.xMax + 0.01f || fp.yMin < room.r.yMin - 0.01f || fp.yMax > room.r.yMax + 0.01f) return false;
            foreach (var d in room.doors)
                if (Inflate(fp, 0.9f).Contains(d)) return false;
            foreach (var u in room.used)
                if (u.Overlaps(fp)) return false;
            return true;
        }

        static Rect Inflate(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        static Bounds PrefabBounds(GameObject prefab, float yaw)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var rs = inst.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(Vector3.zero, Vector3.one * 0.5f);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Object.DestroyImmediate(inst);
            return b;
        }

        /// <summary>Place a prefab with its back against one of the room walls. side: 0=S,1=N,2=W,3=E</summary>
        GameObject AgainstWall(Room room, string prefab, int side, float t, float gap = 0.02f)
        {
            var p = ModelLibrary.Get(prefab);
            if (!p) return null;
            float yaw = side switch { 0 => 0f, 1 => 180f, 2 => 90f, _ => -90f };
            var b = PrefabBounds(p, yaw);
            var r = room.r;
            Vector2 pos;
            switch (side)
            {
                case 0: pos = new Vector2(Mathf.Lerp(r.xMin + b.extents.x, r.xMax - b.extents.x, t), r.yMin + gap - (b.min.z)); break;
                case 1: pos = new Vector2(Mathf.Lerp(r.xMin + b.extents.x, r.xMax - b.extents.x, t), r.yMax - gap - (b.max.z)); break;
                case 2: pos = new Vector2(r.xMin + gap - b.min.x, Mathf.Lerp(r.yMin + b.extents.z, r.yMax - b.extents.z, t)); break;
                default: pos = new Vector2(r.xMax - gap - b.max.x, Mathf.Lerp(r.yMin + b.extents.z, r.yMax - b.extents.z, t)); break;
            }
            var fp = new Rect(pos.x + b.min.x, pos.y + b.min.z, b.size.x, b.size.z);
            if (!Free(room, fp)) return null;
            room.used.Add(fp);
            return LevelKit.Place(prefab, new Vector3(pos.x, 0, pos.y), yaw, _props);
        }

        GameObject AtPoint(Room room, string prefab, Vector2 pos, float yaw, bool force = false, float y = 0f, float scale = 1f)
        {
            var p = ModelLibrary.Get(prefab);
            if (!p) return null;
            var b = PrefabBounds(p, yaw);
            var fp = new Rect(pos.x + b.min.x * scale, pos.y + b.min.z * scale, b.size.x * scale, b.size.z * scale);
            if (!force && !Free(room, fp)) return null;
            if (!force) room.used.Add(fp);
            return LevelKit.Place(prefab, new Vector3(pos.x, y, pos.y), yaw, _props, scale);
        }

        void TryWall(Room room, string prefab, int tries = 8, float gap = 0.02f)
        {
            for (int i = 0; i < tries; i++)
                if (AgainstWall(room, prefab, RI(0, 4), R(), gap)) return;
        }

        void Furnish(Room room)
        {
            switch (room.type)
            {
                case "openplan": OpenPlan(room); break;
                case "break": BreakRoom(room); break;
                case "conference": Conference(room); break;
                case "storage": Storage(room); break;
                case "copy": Copy(room); break;
                default: Office(room); break;
            }
            // wall details everywhere
            if (R() < 0.6f) TryWallDecor(room, "wall_clock", 1.9f);
            if (R() < 0.5f) TryWallDecor(room, "fire_alarm", 1.4f);
            TryWallDecor(room, "OutletSwitch", 0.3f);
            if (R() < 0.4f) TryWall(room, "korean_fire_extinguisher_01");
            if (R() < 0.35f) TryWall(room, "potted_plant_02");
            if (R() < 0.3f) TryWall(room, "trashbag");
        }

        void TryWallDecor(Room room, string prefab, float height)
        {
            for (int i = 0; i < 6; i++)
            {
                int side = RI(0, 4);
                float t = 0.15f + R() * 0.7f;
                var r = room.r;
                Vector2 pos; float yaw;
                switch (side)
                {
                    case 0: pos = new Vector2(Mathf.Lerp(r.xMin, r.xMax, t), r.yMin + 0.02f); yaw = 0; break;
                    case 1: pos = new Vector2(Mathf.Lerp(r.xMin, r.xMax, t), r.yMax - 0.02f); yaw = 180; break;
                    case 2: pos = new Vector2(r.xMin + 0.02f, Mathf.Lerp(r.yMin, r.yMax, t)); yaw = 90; break;
                    default: pos = new Vector2(r.xMax - 0.02f, Mathf.Lerp(r.yMin, r.yMax, t)); yaw = -90; break;
                }
                if (room.doors.Any(d => (d - pos).magnitude < 1.0f)) continue;
                var go = LevelKit.Place(prefab, new Vector3(pos.x, height, pos.y), yaw, _props);
                return;
            }
        }

        void Office(Room room)
        {
            var r = room.r;
            var desk = AtPoint(room, Pick(new[] { "OfficeDesk", "metal_office_desk" }), r.center + new Vector2(0, 0.3f), 180);
            if (desk)
            {
                AtPoint(room, "modern_arm_chair_01", r.center + new Vector2(0, -0.55f), RI(-25, 25), true);
                AtPoint(room, "classic_laptop", r.center + new Vector2(0.2f, 0.3f), 180 + RI(-15, 15), true, 0.75f, 0.7f);
                AtPoint(room, "office_notepads", r.center + new Vector2(-0.45f, 0.2f), RI(0, 360), true, 0.752f, 0.35f);
                if (R() < 0.5f) AtPoint(room, "stationery_supplies", r.center + new Vector2(-0.6f, 0.45f), RI(0, 360), true, 0.752f);
            }
            TryWall(room, "drawer_cabinet");
            if (R() < 0.6f) TryWall(room, "steel_frame_shelves_03");
            if (R() < 0.5f) TryWall(room, "SchoolChair_01");
        }

        void OpenPlan(Room room)
        {
            var r = room.r;
            const float pod = 6.4f, pitch = 8.2f;
            int nx = Mathf.FloorToInt((r.width - 1.6f) / pitch + 0.2f), nz = Mathf.FloorToInt((r.height - 1.6f) / pitch + 0.2f);
            if (nx < 1 || nz < 1) { Office(room); return; }
            float x0 = r.center.x - (nx - 1) * pitch * 0.5f, z0 = r.center.y - (nz - 1) * pitch * 0.5f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var c = new Vector2(x0 + i * pitch, z0 + j * pitch);
                    var fp = new Rect(c.x - pod / 2, c.y - pod / 2, pod, pod);
                    if (!Free(room, Inflate(fp, -0.3f))) continue;
                    room.used.Add(fp);
                    Cluster(c);
                }
            if (R() < 0.7f) TryWall(room, "steel_frame_shelves_03");
            if (R() < 0.7f) TryWall(room, "drawer_cabinet");
            if (R() < 0.4f) TryWall(room, "WaterCooler", 8, 0.1f);
        }

        void Cluster(Vector2 c)
        {
            // spine along X, cross wall along Z, outer walls at x = ±3.2  (cubicles open toward ±Z)
            for (int k = 0; k < 4; k++)
            {
                float o = -2.4f + k * 1.6f;
                LevelKit.Place("CubicleWall", new Vector3(c.x + o, 0, c.y), 0, _props);
                LevelKit.Place("CubicleWall", new Vector3(c.x, 0, c.y + o), 90, _props);
            }
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < 4; k++)
                    LevelKit.Place("CubicleWall", new Vector3(c.x + s * 3.2f, 0, c.y - 2.4f + k * 1.6f), 90, _props);
            // one desk per cubicle, against the spine
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var dp = new Vector3(c.x + sx * 1.6f, 0, c.y + sz * 0.45f);
                    float dy = sz > 0 ? 0f : 180f; // desk front (user side) faces away from the spine
                    LevelKit.Place("OfficeDesk", dp, dy, _props);
                    var chairPos = dp + new Vector3(0, 0, sz * 0.95f);
                    if (R() < 0.85f) LevelKit.Place(Pick(new[] { "modern_arm_chair_01", "SchoolChair_01" }), chairPos + new Vector3(RI(-3, 3) * 0.1f, 0, 0), dy + 180 + RI(-40, 40), _props, 1f, false);
                    if (R() < 0.5f) LevelKit.Place("classic_laptop", dp + new Vector3(-0.3f, 0.75f, 0), dy + RI(-20, 20), _props, 0.7f);
                    if (R() < 0.6f) LevelKit.Place(Pick(new[] { "office_notepads", "binder_notebook", "clipboard", "vintage_stapler", "stationery_supplies" }), dp + new Vector3(0.35f, 0.752f, -sz * 0.05f), RI(0, 360), _props, 0.4f);
                }
        }

        void BreakRoom(Room room)
        {
            AgainstWall(room, "VendingMachine", RI(0, 4), 0.2f + R() * 0.2f, 0.05f);
            TryWall(room, "VendingMachine", 8, 0.05f);
            var wc = AgainstWall(room, "WaterCooler", RI(0, 4), R(), 0.08f);
            if (!wc) TryWall(room, "WaterCooler", 8, 0.08f);
            TryWall(room, "CoffeeCart_01");
            TryWall(room, "trashbag");
            var r = room.r;
            for (int i = 0; i < 2; i++)
            {
                var p = r.center + new Vector2((i - 0.5f) * 2.6f, 0);
                if (AtPoint(room, "metal_office_desk", p, 90 * RI(0, 2), false, 0f, 0.65f))
                    for (int k = 0; k < 3; k++)
                        LevelKit.Place("plastic_monobloc_chair_01", new Vector3(p.x + Mathf.Cos(k * 2.1f) * 0.9f, 0, p.y + Mathf.Sin(k * 2.1f) * 0.9f), RI(0, 360), _props, 1f, false);
            }
            // dispensers
            foreach (var go in _props.GetComponentsInChildren<Transform>().Where(t => t.parent == _props && (t.name.StartsWith("WaterCooler") || t.name.StartsWith("VendingMachine"))))
            {
                if (go.GetComponent<Dispenser>()) continue;
                var d = go.gameObject.AddComponent<Dispenser>();
                d.item = ItemLibrary.Get("almond_water");
                d.uses = go.name.StartsWith("Vending") ? 3 : 2;
                d.verb = go.name.StartsWith("Vending") ? "Prendre une bouteille" : "Remplir une bouteille";
                d.emptyMessage = go.name.StartsWith("Vending") ? "Le distributeur est vide." : "La bonbonne est vide.";
                d.sfx = go.name.StartsWith("Vending") ? "SFX/sfx_pickup" : "SFX/sfx_drink";
            }
        }

        void Conference(Room room)
        {
            var r = room.r;
            bool alongX = r.width > r.height;
            float yaw = alongX ? 0 : 90;
            for (int i = -1; i <= 1; i++)
            {
                var p = r.center + (alongX ? new Vector2(i * 2f, 0) : new Vector2(0, i * 2f));
                AtPoint(room, "metal_office_desk", p, yaw, true);
                room.used.Add(new Rect(p.x - 1.2f, p.y - 1.2f, 2.4f, 2.4f));
                for (int s = -1; s <= 1; s += 2)
                {
                    var off = alongX ? new Vector2(0, s * 0.95f) : new Vector2(s * 0.95f, 0);
                    if (R() < 0.85f) LevelKit.Place("SchoolChair_01", new Vector3(p.x + off.x, 0, p.y + off.y), yaw + (s > 0 ? 180 : 0) + RI(-25, 25), _props, 1f, false);
                }
            }
            TryWall(room, "projector_screen");
            TryWall(room, "potted_plant_02");
        }

        void Storage(Room room)
        {
            for (int i = 0; i < 4; i++) TryWall(room, "steel_frame_shelves_03");
            for (int i = 0; i < 5; i++)
            {
                var p = room.r.center + new Vector2((R() - 0.5f) * room.r.width * 0.6f, (R() - 0.5f) * room.r.height * 0.6f);
                AtPoint(room, "cardboard_box_01", p, RI(0, 360));
            }
            TryWall(room, "WetFloorSign_01");
            TryWall(room, "trashbag");
        }

        void Copy(Room room)
        {
            TryWall(room, "drawer_cabinet");
            TryWall(room, "drawer_cabinet");
            TryWall(room, "CoffeeCart_01");
            for (int i = 0; i < 3; i++) AtPoint(room, "cardboard_box_01", room.r.center + new Vector2(R() - 0.5f, R() - 0.5f) * 2f, RI(0, 360));
        }

        // ------------------------------------------------------------ items & story
        void PlaceItems()
        {
            var start = _rooms.First(r => r.type == "start");
            var itemsRoot = LevelKit.Group("Items", _root).transform;
            // the start office: flashlight + note on the floor near the desk
            var sp = start.r.center;
            LevelKit.PlaceItem("flashlight", new Vector3(sp.x - 0.5f, 0.8f, sp.y + 0.15f), 30, itemsRoot);
            LevelKit.PlaceItem("note_l4_1", new Vector3(sp.x + 0.35f, 0.8f, sp.y + 0.25f), -10, itemsRoot);
            LevelKit.PlaceItem("almond_water", new Vector3(sp.x + 0.75f, 0.8f, sp.y + 0.05f), 0, itemsRoot);

            var ordered = _rooms.Where(r => r.dist >= 0).OrderByDescending(r => r.dist).ToList();
            var keyRoom = ordered[0].r.xMax > (_w * Cell - 1f) && ordered.Count > 1 ? ordered[1] : ordered[0];
            FloorItem(keyRoom, "keycard_l4", itemsRoot);
            var pool = _rooms.Where(r => r != start).ToList();
            string[] loot = { "almond_water", "almond_water", "almond_water", "almond_water", "battery", "battery", "battery", "ration", "ration", "medkit", "pocket_watch", "lighter" };
            foreach (var id in loot) FloorItem(Pick(pool), id, itemsRoot);
            var breakRoom = _rooms.FirstOrDefault(r => r.type == "break") ?? Pick(pool);
            FloorItem(breakRoom, "cassette", itemsRoot);

            // elevator (out of order) on the west wall of a large room
            var westRoom = _rooms.Where(r => r.cells.xMin == 0 && r != start).OrderByDescending(r => r.Area).FirstOrDefault();
            if (westRoom != null)
            {
                var el = AgainstWall(westRoom, "Elevator_Modern", 2, 0.5f, -0.02f);
                if (el)
                {
                    var rn = el.AddComponent<ReadableNote>();
                    rn.title = "Affiche scotchée";
                    rn.text = "HORS SERVICE\n\nMerci d'emprunter l'ESCALIER B (côté est).\nAccès avec badge uniquement.\n\n— Services Généraux";
                }
            }
        }

        void FloorItem(Room room, string id, Transform parent)
        {
            for (int i = 0; i < 20; i++)
            {
                var p = new Vector2(Mathf.Lerp(room.r.xMin + 0.5f, room.r.xMax - 0.5f, R()), Mathf.Lerp(room.r.yMin + 0.5f, room.r.yMax - 0.5f, R()));
                var fp = new Rect(p.x - 0.2f, p.y - 0.2f, 0.4f, 0.4f);
                if (room.used.Any(u => u.Overlaps(fp))) continue;
                LevelKit.PlaceItem(id, new Vector3(p.x, 0.12f, p.y), RI(0, 360), parent);
                room.used.Add(fp);
                return;
            }
            LevelKit.PlaceItem(id, new Vector3(room.r.center.x, 0.12f, room.r.center.y), 0, parent);
        }

        void MenuCamera()
        {
            var rig = new GameObject("Menu Camera");
            rig.transform.SetParent(_root, false);
            var big = _rooms.OrderByDescending(r => r.Area).First();
            rig.transform.position = new Vector3(big.r.xMin + 1.5f, 1.55f, big.r.yMin + 1.2f);
            var cam = rig.AddComponent<Camera>();
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.05f;
            rig.tag = "MainCamera";
            var hd = rig.AddComponent<HDAdditionalCameraData>();
            EditorCapture.ConfigureCamera(hd);
            rig.AddComponent<AudioListener>();
            var menu = new GameObject("Main Menu").AddComponent<MainMenu>();
            menu.transform.SetParent(_root, false);
            menu.cameraRig = rig.transform;
            menu.driftSpeed = 0.25f;
            var ls = LevelKit.Settings(_root, rig.transform, 0, 0, new[] { "Ambience/amb_fluorescent_hum", "Ambience/amb_building_tone" }, 0.35f);
        }
    }
}
