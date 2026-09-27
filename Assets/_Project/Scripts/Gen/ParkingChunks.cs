using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEngine;

namespace SyncRooms.Gen
{
    /// <summary>Level 1 — "Parking Zone": endless concrete parking garage (pillars, stalls, covered cars, puddles).</summary>
    public class ParkingChunks : ChunkBuilder
    {
        const float S = 24f, H = 3.0f;
        public int seed = 101;

        public override void Build(Transform chunk, Vector2Int c, System.Random rng)
        {
            var g = new ChunkGeo();
            g.Surface("L4_Concrete", SurfaceType.Concrete);
            g.Floor("L4_Concrete", new Rect(0, 0, S, S), 0);
            g.Floor("L5_OldConcrete", new Rect(0, 0, S, S), H, false);
            int hash = Mathf.Abs(ChunkWorld.Hash(seed, c.x, c.y));
            // pillars with a painted band
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    float x = 4 + i * 8, z = 4 + j * 8;
                    g.Box("L5_OldConcrete", new Vector3(x - 0.35f, 0, z - 0.35f), new Vector3(x + 0.35f, H, z + 0.35f), false, false);
                    g.Box("PaintYellow", new Vector3(x - 0.36f, 0, z - 0.36f), new Vector3(x + 0.36f, 0.9f, z + 0.36f), true, false);
                }
            // stall lines (two rows) + dashed center line
            for (float x = 0.5f; x < S; x += 2.6f)
            {
                g.Floor("PaintLine", new Rect(x, 1f, 0.1f, 5f), 0.006f);
                g.Floor("PaintLine", new Rect(x, S - 6f, 0.1f, 5f), 0.006f);
            }
            for (float x = 0.5f; x < S; x += 3f) g.Floor("PaintYellow", new Rect(x, 11.95f, 1.5f, 0.1f), 0.006f);
            // occasional partition walls on the owned edges (with wide openings)
            if (hash % 3 == 0) g.Wall("L5_OldConcrete", "L5_OldConcrete", new Vector2(0, 0), new Vector2(S, 0), 0.3f, 0, H, new List<Vector3> { new(12, 7f, H) });
            if (hash % 5 == 1) g.Wall("L5_OldConcrete", "L5_OldConcrete", new Vector2(0, 0), new Vector2(0, S), 0.3f, 0, H, new List<Vector3> { new(12, 7f, H) });
            // puddles
            for (int k = 0; k < 3; k++)
                if (rng.NextDouble() < 0.5)
                {
                    float x = (float)rng.NextDouble() * 20 + 2, z = (float)rng.NextDouble() * 20 + 2, w = 1f + (float)rng.NextDouble() * 2f;
                    g.Floor("Puddle", new Rect(x, z, w, w * 0.7f), 0.008f);
                }
            // lights hanging from the ceiling
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    var p = new Vector3(8 + i * 8 - 4, H - 0.02f, 8 + j * 8);
                    if (p.z > S) continue;
                    bool dead = rng.NextDouble() < 0.15;
                    if (!dead) { ChunkWorld.Spawn("mounted_fluorescent_lights", chunk, p + Vector3.down * 0.04f, 90); ChunkWorld.Point(chunk, p + Vector3.down * 0.3f, 2200f, 11f, 5200f, rng.NextDouble() < 0.08); }
                }
            // parked cars in the stalls
            for (float x = 1.8f; x < S - 1; x += 2.6f)
            {
                if (rng.NextDouble() < 0.3) ChunkWorld.Spawn(rng.NextDouble() < 0.9 ? "covered_car" : "VintageVan", chunk, new Vector3(x, 0, 3.5f), rng.NextDouble() < 0.5 ? 0 : 180);
                if (rng.NextDouble() < 0.3) ChunkWorld.Spawn("covered_car", chunk, new Vector3(x, 0, S - 3.5f), rng.NextDouble() < 0.5 ? 0 : 180);
            }
            // clutter & supplies
            if (rng.NextDouble() < 0.4) for (int k = 0; k < 3; k++) ChunkWorld.Spawn("Barrel_01", chunk, new Vector3(22.5f - k * 0.6f, 0, 22.5f), rng.Next(0, 360));
            if (rng.NextDouble() < 0.35)
            {
                var p = new Vector3(1.5f + (float)rng.NextDouble() * 20, 0, 12 + (rng.NextDouble() < 0.5 ? -2.5f : 2.5f));
                for (int k = 0; k < 3; k++) ChunkWorld.Spawn("cardboard_box_01", chunk, p + new Vector3(k * 0.45f, 0, 0), rng.Next(0, 360));
                ChunkWorld.SpawnItem(rng.NextDouble() < 0.6 ? "almond_water" : rng.NextDouble() < 0.5 ? "battery" : "ration", chunk, p + new Vector3(0.4f, 0.5f, 0.6f));
            }
            if (rng.NextDouble() < 0.25) ChunkWorld.Spawn("metal_trash_can", chunk, new Vector3(3.2f, 0, 12f), 0, 0.7f);
            // rare stairwell exit (to the Hub)
            if (c.sqrMagnitude >= 4 && hash % 14 == 3) Stairwell(chunk, g);
            if (c == Vector2Int.zero) ChunkWorld.SpawnItem("flashlight", chunk, new Vector3(12.5f, 0.15f, 13.2f));
            g.Emit(chunk, "Geo");
        }

        static void Stairwell(Transform chunk, ChunkGeo g)
        {
            var min = new Vector2(10.2f, 14.5f);
            float w = 3.6f, d = 3.2f;
            g.Wall("L4_Concrete", "L4_Concrete", min, min + new Vector2(w, 0), 0.2f, 0, H, new List<Vector3> { new(w / 2, 1.0f, 2.12f) });
            g.Wall("L4_Concrete", "L4_Concrete", min + new Vector2(w, d), min + new Vector2(0, d), 0.2f, 0, H);
            g.Wall("L4_Concrete", "L4_Concrete", min + new Vector2(0, d), min, 0.2f, 0, H);
            g.Wall("L4_Concrete", "L4_Concrete", min + new Vector2(w, 0), min + new Vector2(w, d), 0.2f, 0, H);
            ChunkWorld.Spawn("ExitSign", chunk, new Vector3(min.x + w / 2, 2.8f, min.y - 0.12f), 180);
            var ex = new GameObject("Exit → Hub");
            ex.transform.SetParent(chunk, false);
            ex.transform.localPosition = new Vector3(min.x + w / 2, 1f, min.y + d / 2);
            var bc = ex.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(2.4f, 2f, 1.6f);
            var le = ex.AddComponent<LevelExit>();
            le.targetScene = "Hub";
            le.isTrigger = true;
            le.exitKind = ExitKind.Stairs;
            ChunkWorld.Point(chunk, new Vector3(min.x + w / 2, 2.6f, min.y + d / 2), 600f, 5f, 3200f, true);
        }
    }
}
