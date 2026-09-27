using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEngine;

namespace SyncRooms.Gen
{
    /// <summary>
    /// Level 4 — endless office floor. Each 24m chunk is BSP-split into rooms; chunk edges carry openings
    /// derived from a shared edge hash so neighbouring chunks always line up. Rare chunks contain the
    /// locked "Stairwell B" (keycard) leading to Level 5, others the keycard itself.
    /// </summary>
    public class OfficeChunks : ChunkBuilder
    {
        const float S = 24f, Cell = 1.2f, H = 2.7f, T = 0.12f;
        const int N = 20;
        public int seed = 404;

        struct Op { public float at, w; public bool door; }

        // openings on the south edge (along X) of chunk (x,z) and the west edge (along Z)
        List<Op> Edge(bool west, int x, int z)
        {
            var r = new System.Random(ChunkWorld.Hash(seed * 7 + (west ? 1 : 2), x, z));
            var l = new List<Op>();
            int n = r.NextDouble() < 0.45 ? 2 : 1;
            int first = 3 + r.Next(6);
            for (int i = 0; i < n; i++)
            {
                int cell = i == 0 ? first : first + 7 + r.Next(5);
                l.Add(new Op { at = cell * Cell + Cell * 0.5f, w = r.NextDouble() < 0.4 ? 1.0f : 1.8f, door = r.NextDouble() < 0.35 });
            }
            return l;
        }

        class Room { public RectInt c; public Rect r; public string type; public readonly List<Vector2> doors = new(); public readonly List<Rect> used = new(); }
        class Split { public bool alongZ; public int pos, a, b; public readonly List<Op> ops = new(); }

        public override void Build(Transform chunk, Vector2Int c, System.Random rng)
        {
            var geo = new ChunkGeo();
            geo.Surface("L4_Carpet", SurfaceType.Carpet);
            geo.Surface("L4_Tile", SurfaceType.Tile);
            var south = Edge(false, c.x, c.y); var north = Edge(false, c.x, c.y + 1);
            var west = Edge(true, c.x, c.y); var east = Edge(true, c.x + 1, c.y);
            bool origin = c == Vector2Int.zero;

            // --- BSP (splits avoid the edge openings so corridors never get blocked)
            var splits = new List<Split>();
            var rooms = new List<Room>();
            bool Clear(bool alongZ, int k)
            {
                float m = k * Cell;
                if (origin && Mathf.Abs(m - 12f) < 2f) return false;       // keep the spawn point in a room
                var edges = alongZ ? new[] { south, north } : new[] { west, east };
                foreach (var e in edges) foreach (var o in e) if (Mathf.Abs(o.at - m) < o.w * 0.5f + 0.9f) return false;
                return true;
            }
            void Bsp(RectInt r, int depth)
            {
                bool canZ = r.width >= 10, canX = r.height >= 10;
                bool stop = (!canZ && !canX) || (depth > 0 && r.width * r.height < 150 && rng.NextDouble() < 0.5) || (depth == 0 && rng.NextDouble() < 0.12);
                if (!stop)
                {
                    bool alongZ = canZ && (!canX || (r.width > r.height ? rng.NextDouble() < 0.8 : rng.NextDouble() < 0.2));
                    int lo = alongZ ? r.xMin + 5 : r.yMin + 5, hi = alongZ ? r.xMax - 5 : r.yMax - 5;
                    for (int tries = 0; tries < 8 && hi >= lo; tries++)
                    {
                        int k = rng.Next(lo, hi + 1);
                        if (!Clear(alongZ, k)) continue;
                        splits.Add(new Split { alongZ = alongZ, pos = k, a = alongZ ? r.yMin : r.xMin, b = alongZ ? r.yMax : r.xMax });
                        if (alongZ) { Bsp(new RectInt(r.xMin, r.yMin, k - r.xMin, r.height), depth + 1); Bsp(new RectInt(k, r.yMin, r.xMax - k, r.height), depth + 1); }
                        else { Bsp(new RectInt(r.xMin, r.yMin, r.width, k - r.yMin), depth + 1); Bsp(new RectInt(r.xMin, k, r.width, r.yMax - k), depth + 1); }
                        return;
                    }
                }
                rooms.Add(new Room { c = r });
            }
            Bsp(new RectInt(0, 0, N, N), 0);
            foreach (var rm in rooms)
                rm.r = Rect.MinMaxRect(rm.c.xMin * Cell + T, rm.c.yMin * Cell + T, rm.c.xMax * Cell - T, rm.c.yMax * Cell - T);

            // --- internal openings (one per span between junctions)
            foreach (var s in splits)
            {
                var block = new List<int> { s.a, s.b };
                foreach (var o in splits)
                    if (o != s && o.alongZ != s.alongZ && o.pos > s.a && o.pos < s.b && (o.a == s.pos || o.b == s.pos)) block.Add(o.pos);
                block.Sort();
                for (int i = 0; i < block.Count - 1; i++)
                {
                    if (block[i + 1] - block[i] < 3) continue;
                    float at = (block[i] + 1 + (float)rng.NextDouble() * (block[i + 1] - block[i] - 2)) * Cell;
                    s.ops.Add(new Op { at = at, w = rng.NextDouble() < 0.5 ? 1.0f : 1.8f, door = rng.NextDouble() < 0.4 });
                }
            }

            // --- walls
            List<Vector3> V3(List<Op> ops, float offset = 0) { var l = new List<Vector3>(); foreach (var o in ops) l.Add(new Vector3(o.at - offset, o.w, o.w > 1.2f ? 2.25f : 2.12f)); return l; }
            geo.Wall("L4_Wall", "L4_Wall", new Vector2(0, 0), new Vector2(S, 0), T * 2, 0, H, V3(south), "PlasticBlack");
            geo.Wall("L4_Wall", "L4_Wall", new Vector2(0, 0), new Vector2(0, S), T * 2, 0, H, V3(west), "PlasticBlack");
            foreach (var s in splits)
            {
                var from = s.alongZ ? new Vector2(s.pos * Cell, s.a * Cell) : new Vector2(s.a * Cell, s.pos * Cell);
                var to = s.alongZ ? new Vector2(s.pos * Cell, s.b * Cell) : new Vector2(s.b * Cell, s.pos * Cell);
                geo.Wall("L4_Wall", "L4_Wall", from, to, T, 0, H, V3(s.ops, s.a * Cell), "PlasticBlack");
            }
            // door leaves
            foreach (var o in south) if (o.door && o.w < 1.2f) Door(chunk, new Vector3(o.at, 0, 0), 0);
            foreach (var o in west) if (o.door && o.w < 1.2f) Door(chunk, new Vector3(0, 0, o.at), 90);
            foreach (var s in splits)
                foreach (var o in s.ops)
                    if (o.door && o.w < 1.2f) Door(chunk, s.alongZ ? new Vector3(s.pos * Cell, 0, o.at) : new Vector3(o.at, 0, s.pos * Cell), s.alongZ ? 90 : 0);

            // --- rooms: floor, ceiling, lights, furniture
            geo.Floor("L4_Ceiling", new Rect(0, 0, S, S), H, false);
            int hash = ChunkWorld.Hash(seed, c.x, c.y);
            bool stairs = !origin && Mathf.Abs(hash % 13) == 5 && c.sqrMagnitude >= 4;
            bool keycard = !origin && Mathf.Abs(hash % 9) == 4;
            Room biggest = rooms[0];
            foreach (var rm in rooms) if (rm.r.width * rm.r.height > biggest.r.width * biggest.r.height) biggest = rm;
            foreach (var rm in rooms)
            {
                float area = rm.r.width * rm.r.height;
                rm.type = area > 150 ? (rng.NextDouble() < 0.35 ? "empty" : "openplan") : area > 55 ? Pick(rng, "break", "conference", "openplan", "empty", "storage") : Pick(rng, "office", "office", "storage", "empty");
                if (origin && rm.r.Contains(new Vector2(12, 12))) rm.type = "office";
                if (stairs && rm == biggest) rm.type = "stairs";
                string floor = rm.type == "break" ? "L4_Tile" : "L4_Carpet";
                geo.Floor(floor, new Rect(rm.c.xMin * Cell, rm.c.yMin * Cell, rm.c.width * Cell, rm.c.height * Cell), 0);
                Lights(chunk, rm, rng);
                Furnish(chunk, rm, rng);
                if (rng.NextDouble() < 0.18) Loot(chunk, rm, rng);
            }
            if (keycard) { var rm = rooms[rng.Next(rooms.Count)]; ChunkWorld.SpawnItem("keycard_l4", chunk, new Vector3(rm.r.center.x, 0.15f, rm.r.center.y)); }
            if (origin)
            {
                ChunkWorld.SpawnItem("flashlight", chunk, new Vector3(12.6f, 0.15f, 12.4f));
                ChunkWorld.SpawnItem("note_l4_1", chunk, new Vector3(11.6f, 0.15f, 12.8f));
                ChunkWorld.SpawnItem("almond_water", chunk, new Vector3(12.2f, 0.15f, 11.3f));
            }
            geo.Emit(chunk, "Geo");
        }

        static string Pick(System.Random r, params string[] a) => a[r.Next(a.Length)];

        void Door(Transform chunk, Vector3 p, float yaw)
        {
            var frame = ChunkWorld.Spawn("Office_DoorFrame", chunk, p, yaw);
            if (!frame) return;
            var leaf = Instantiate(Registry.Prefab("Office_DoorLeaf"), frame.transform);
            leaf.transform.localPosition = new Vector3(0.44f, 0, 0);
            leaf.transform.localRotation = Quaternion.identity;
            var d = leaf.AddComponent<Door>();
            d.startsOpen = Random.value < 0.4f;
            d.openAngle = Random.Range(70f, 100f);
        }

        void Lights(Transform chunk, Room rm, System.Random rng)
        {
            var r = rm.r;
            int nx = Mathf.Max(1, Mathf.FloorToInt((r.width - 0.6f) / 2.4f)), nz = Mathf.Max(1, Mathf.FloorToInt((r.height - 0.6f) / 3.6f));
            float sx = r.center.x - (nx - 1) * 1.2f, sz = r.center.y - (nz - 1) * 1.8f;
            bool darkRoom = rng.NextDouble() < 0.08;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    var p = new Vector3(sx + i * 2.4f, H, sz + j * 3.6f);
                    var t = ChunkWorld.Spawn("Troffer", chunk, p, 0);
                    bool dead = darkRoom || rng.NextDouble() < 0.1;
                    if (dead && t)
                    {
                        var mr = t.GetComponentInChildren<MeshRenderer>();
                        var mats = mr.sharedMaterials;
                        for (int k = 0; k < mats.Length; k++) if (mats[k] && mats[k].name == "EmissivePanel") mats[k] = Registry.Mat("PlasticWhite");
                        mr.sharedMaterials = mats;
                        continue;
                    }
                    if ((i + j) % 2 == 0 || nx * nz <= 2)
                        ChunkWorld.Point(chunk, p + Vector3.down * 0.2f, 1700f, 7.5f, 4700f, rng.NextDouble() < 0.06);
                }
        }

        void Loot(Transform chunk, Room rm, System.Random rng)
        {
            double d = rng.NextDouble();
            string id = d < 0.5 ? "almond_water" : d < 0.75 ? "battery" : d < 0.9 ? "ration" : "medkit";
            var p = new Vector3(Mathf.Lerp(rm.r.xMin + 0.6f, rm.r.xMax - 0.6f, (float)rng.NextDouble()), 0.15f, Mathf.Lerp(rm.r.yMin + 0.6f, rm.r.yMax - 0.6f, (float)rng.NextDouble()));
            ChunkWorld.SpawnItem(id, chunk, p);
        }

        void Furnish(Transform chunk, Room rm, System.Random rng)
        {
            var r = rm.r;
            var c = r.center;
            switch (rm.type)
            {
                case "office":
                    ChunkWorld.Spawn(rng.NextDouble() < 0.5 ? "OfficeDesk" : "metal_office_desk", chunk, new Vector3(c.x, 0, c.y + 0.3f), 180);
                    ChunkWorld.Spawn("modern_arm_chair_01", chunk, new Vector3(c.x, 0, c.y - 0.55f), rng.Next(-30, 30));
                    ChunkWorld.Spawn("classic_laptop", chunk, new Vector3(c.x + 0.2f, 0.75f, c.y + 0.3f), 180, 0.7f);
                    AgainstWall(chunk, rm, "drawer_cabinet", rng);
                    if (rng.NextDouble() < 0.5) AgainstWall(chunk, rm, "steel_frame_shelves_03", rng);
                    if (rng.NextDouble() < 0.4) AgainstWall(chunk, rm, "potted_plant_02", rng);
                    break;
                case "openplan":
                    for (float x = r.xMin + 2.2f; x < r.xMax - 2f; x += 4.2f)
                        for (float z = r.yMin + 2.2f; z < r.yMax - 2f; z += 4.4f)
                        {
                            ChunkWorld.Spawn("CubicleWall", chunk, new Vector3(x, 0, z + 1.1f), 0);
                            ChunkWorld.Spawn("CubicleWall", chunk, new Vector3(x - 0.8f, 0, z + 0.3f), 90);
                            ChunkWorld.Spawn("OfficeDesk", chunk, new Vector3(x, 0, z + 0.62f), 0);
                            if (rng.NextDouble() < 0.8) ChunkWorld.Spawn(rng.NextDouble() < 0.5 ? "modern_arm_chair_01" : "SchoolChair_01", chunk, new Vector3(x, 0, z - 0.3f), 180 + rng.Next(-40, 40));
                            if (rng.NextDouble() < 0.4) ChunkWorld.Spawn("classic_laptop", chunk, new Vector3(x - 0.3f, 0.75f, z + 0.6f), rng.Next(-20, 20), 0.7f);
                        }
                    break;
                case "break":
                    var vm = AgainstWall(chunk, rm, "VendingMachine", rng);
                    var wc = AgainstWall(chunk, rm, "WaterCooler", rng);
                    foreach (var g in new[] { vm, wc })
                    {
                        if (!g) continue;
                        var d = g.AddComponent<Dispenser>();
                        d.item = Registry.Item("almond_water");
                        d.uses = g == vm ? 3 : 2;
                        d.verb = g == vm ? "Prendre une bouteille" : "Remplir une bouteille";
                        d.emptyMessage = "C'est vide.";
                    }
                    ChunkWorld.Spawn("metal_office_desk", chunk, new Vector3(c.x, 0, c.y), 90, 0.65f);
                    for (int k = 0; k < 3; k++) ChunkWorld.Spawn("plastic_monobloc_chair_01", chunk, new Vector3(c.x + Mathf.Cos(k * 2.1f) * 0.9f, 0, c.y + Mathf.Sin(k * 2.1f) * 0.9f), rng.Next(0, 360));
                    break;
                case "conference":
                    ChunkWorld.Spawn("metal_office_desk", chunk, new Vector3(c.x - 1f, 0, c.y), 0);
                    ChunkWorld.Spawn("metal_office_desk", chunk, new Vector3(c.x + 1f, 0, c.y), 0);
                    for (int k = -1; k <= 1; k++)
                    {
                        ChunkWorld.Spawn("SchoolChair_01", chunk, new Vector3(c.x + k * 1.1f, 0, c.y - 0.9f), rng.Next(-20, 20));
                        ChunkWorld.Spawn("SchoolChair_01", chunk, new Vector3(c.x + k * 1.1f, 0, c.y + 0.9f), 180 + rng.Next(-20, 20));
                    }
                    AgainstWall(chunk, rm, "projector_screen", rng);
                    break;
                case "storage":
                    for (int k = 0; k < 3; k++) AgainstWall(chunk, rm, "steel_frame_shelves_03", rng);
                    for (int k = 0; k < 4; k++) ChunkWorld.Spawn("cardboard_box_01", chunk, new Vector3(c.x + (float)(rng.NextDouble() - 0.5) * r.width * 0.5f, 0, c.y + (float)(rng.NextDouble() - 0.5) * r.height * 0.5f), rng.Next(0, 360));
                    if (rng.NextDouble() < 0.5) ChunkWorld.Spawn("WetFloorSign_01", chunk, new Vector3(c.x, 0, c.y), rng.Next(0, 360));
                    break;
                case "stairs":
                    Stairwell(chunk, rm, rng);
                    break;
            }
            if (rng.NextDouble() < 0.4) Decor(chunk, rm, "wall_clock", 1.9f, rng);
            if (rng.NextDouble() < 0.3) Decor(chunk, rm, "fire_alarm", 1.4f, rng);
        }

        GameObject AgainstWall(Transform chunk, Room rm, string prefab, System.Random rng)
        {
            var r = rm.r;
            for (int i = 0; i < 6; i++)
            {
                int side = rng.Next(4);
                float t = 0.2f + (float)rng.NextDouble() * 0.6f;
                Vector3 p; float yaw;
                switch (side)
                {
                    case 0: p = new Vector3(Mathf.Lerp(r.xMin, r.xMax, t), 0, r.yMin + 0.45f); yaw = 0; break;
                    case 1: p = new Vector3(Mathf.Lerp(r.xMin, r.xMax, t), 0, r.yMax - 0.45f); yaw = 180; break;
                    case 2: p = new Vector3(r.xMin + 0.45f, 0, Mathf.Lerp(r.yMin, r.yMax, t)); yaw = 90; break;
                    default: p = new Vector3(r.xMax - 0.45f, 0, Mathf.Lerp(r.yMin, r.yMax, t)); yaw = -90; break;
                }
                var fp = new Rect(p.x - 0.7f, p.z - 0.7f, 1.4f, 1.4f);
                bool bad = false;
                foreach (var u in rm.used) if (u.Overlaps(fp)) bad = true;
                if (bad || NearOpening(p)) continue;
                rm.used.Add(fp);
                return ChunkWorld.Spawn(prefab, chunk, p, yaw);
            }
            return null;
        }

        bool NearOpening(Vector3 p) => false;

        void Decor(Transform chunk, Room rm, string prefab, float y, System.Random rng)
        {
            var r = rm.r;
            float t = 0.3f + (float)rng.NextDouble() * 0.4f;
            ChunkWorld.Spawn(prefab, chunk, new Vector3(Mathf.Lerp(r.xMin, r.xMax, t), y, r.yMax - 0.02f), 180);
        }

        void Stairwell(Transform chunk, Room rm, System.Random rng)
        {
            // Freestanding concrete core with a keycard door; inside, stairs go down to Level 5.
            var c = rm.r.center;
            var geo = new ChunkGeo();
            float w = 3.6f, d = 3.6f;
            var min = new Vector2(c.x - w / 2, c.y - d / 2);
            var ops = new List<Vector3> { new(w / 2, 1.0f, 2.12f) };
            geo.Wall("L4_Concrete", "L4_Concrete", new Vector2(min.x, min.y), new Vector2(min.x + w, min.y), 0.2f, 0, H, ops);
            geo.Wall("L4_Concrete", "L4_Concrete", new Vector2(min.x + w, min.y + d), new Vector2(min.x, min.y + d), 0.2f, 0, H);
            geo.Wall("L4_Concrete", "L4_Concrete", new Vector2(min.x, min.y + d), new Vector2(min.x, min.y), 0.2f, 0, H);
            geo.Wall("L4_Concrete", "L4_Concrete", new Vector2(min.x + w, min.y), new Vector2(min.x + w, min.y + d), 0.2f, 0, H);
            for (int i = 0; i < 5; i++) geo.Box("L4_Concrete", new Vector3(min.x + 0.4f, 0, min.y + 1.2f + i * 0.45f), new Vector3(min.x + w - 0.4f, 0.02f - i * 0.001f + 0.05f, min.y + 1.2f + (i + 1) * 0.45f), true, false);
            geo.Floor("PaintBlack", new Rect(min.x + 0.4f, min.y + 1.2f, w - 0.8f, 2.2f), 0.03f);
            geo.Emit(chunk, "Stairwell");
            var frame = ChunkWorld.Spawn("Office_DoorFrame", chunk, new Vector3(c.x, 0, min.y), 0);
            if (frame)
            {
                var leaf = Instantiate(Registry.Prefab("Office_DoorLeaf"), frame.transform);
                leaf.transform.localPosition = new Vector3(0.44f, 0, 0);
                var door = leaf.AddComponent<Door>();
                door.requiredKeyId = "keycard_l4";
                door.lockedMessage = "ESCALIER B — Accès réservé. Badge requis.";
            }
            ChunkWorld.Spawn("ExitSign", chunk, new Vector3(c.x, 2.62f, min.y - 0.12f), 180);
            var exit = new GameObject("Exit → Level 5");
            exit.transform.SetParent(chunk, false);
            exit.transform.localPosition = new Vector3(c.x, 1f, c.y + 0.6f);
            var bc = exit.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(2.2f, 2f, 1.6f);
            var ex = exit.AddComponent<LevelExit>();
            ex.targetScene = "Level5";
            ex.isTrigger = true;
            ex.exitKind = ExitKind.Stairs;
            var anchor = new GameObject("StairsAnchor").transform;
            anchor.SetParent(exit.transform, false);
            anchor.localPosition = new Vector3(0, -1f, 1.2f);
            ex.anchor = anchor;
            ChunkWorld.Point(chunk, new Vector3(c.x, 2.3f, c.y), 500f, 5f, 3500f, true);
        }
    }
}
