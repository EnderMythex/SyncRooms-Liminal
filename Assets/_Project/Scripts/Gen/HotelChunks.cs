using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEngine;

namespace SyncRooms.Gen
{
    /// <summary>
    /// Level 5 — endless hotel corridors around the authored lobby. Each 24m chunk is a crossing of 3m wide
    /// corridors (arms open/closed from shared edge hashes), sometimes widened into a lounge; walls are damask
    /// over wood wainscot, lined with numbered locked doors and art-deco sconces.
    /// </summary>
    public class HotelChunks : ChunkBuilder
    {
        const float S = 24f, H = 3.2f, W = 3f;
        public int seed = 55;
        public Rect excluded;             // authored area (world XZ)
        public Vector2Int forcedEastOpen;  // chunk whose east arm must connect to the authored corridor

        bool Open(bool west, int x, int z)
        {
            if (west && x == forcedEastOpen.x + 1 && z == forcedEastOpen.y) return true;
            var r = new System.Random(ChunkWorld.Hash(seed * 13 + (west ? 1 : 2), x, z));
            return r.NextDouble() < 0.72;
        }

        public override bool Exists(Vector2Int c)
        {
            var w = ChunkWorld.Current;
            var o = w ? w.origin : Vector3.zero;
            var rect = new Rect(o.x + c.x * S, o.z + c.y * S, S, S);
            return !rect.Overlaps(excluded);
        }

        public override void Build(Transform chunk, Vector2Int c, System.Random rng)
        {
            var g = new ChunkGeo();
            g.Surface("L5_HotelCarpet", SurfaceType.Carpet);
            bool n = Open(false, c.x, c.y + 1), s = Open(false, c.x, c.y), wst = Open(true, c.x, c.y), e = Open(true, c.x + 1, c.y);
            bool lounge = rng.NextDouble() < 0.15;
            float hj = lounge ? 4.5f : W / 2;                 // half-size of the central junction
            var J = Rect.MinMaxRect(12 - hj, 12 - hj, 12 + hj, 12 + hj);
            g.Floor("L5_HotelCarpet", J, 0);
            g.Floor("L5_Plaster", J, H, false);
            int num = 600 + (Mathf.Abs(ChunkWorld.Hash(seed, c.x, c.y)) % 300);

            // arms
            void Arm(bool open, Vector2 dir)
            {
                var side = new Vector2(-dir.y, dir.x);
                var a = new Vector2(12, 12) + dir * hj;           // junction edge
                var b = new Vector2(12, 12) + dir * 12f;          // chunk edge
                if (!open)
                {
                    // close the junction on this side
                    WallBand(g, a - side * hj, a + side * hj);
                    return;
                }
                var r0 = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - (dir.x == 0 ? W / 2 : 0), Mathf.Min(a.y, b.y) - (dir.y == 0 ? W / 2 : 0),
                                         Mathf.Max(a.x, b.x) + (dir.x == 0 ? W / 2 : 0), Mathf.Max(a.y, b.y) + (dir.y == 0 ? W / 2 : 0));
                g.Floor("L5_HotelCarpet", r0, 0);
                g.Floor("L5_Plaster", r0, H, false);
                // two long walls facing the corridor
                WallBand(g, b + side * W / 2, a + side * W / 2);
                WallBand(g, a - side * W / 2, b - side * W / 2);
                // lounge: fill the junction edge beside the arm
                if (lounge)
                {
                    WallBand(g, a + side * W / 2, a + side * hj);
                    WallBand(g, a - side * hj, a - side * W / 2);
                }
                float len = (b - a).magnitude;
                for (float t = 1.8f; t < len - 1f; t += 4.5f)
                {
                    var p = a + dir * t;
                    float yawL = Mathf.Atan2(-side.x, -side.y) * Mathf.Rad2Deg;   // facing into the corridor from +side wall
                    FakeDoor(chunk, p + side * W / 2, yawL, num++);
                    FakeDoor(chunk, p + dir * 2.2f - side * W / 2, yawL + 180, num++);
                    Sconce(chunk, p + dir * 1.1f + side * W / 2, yawL, rng);
                    Sconce(chunk, p + dir * 3.3f - side * W / 2, yawL + 180, rng);
                }
            }
            Arm(n, Vector2.up); Arm(s, Vector2.down); Arm(e, Vector2.right); Arm(wst, Vector2.left);

            if (lounge)
            {
                ChunkWorld.Spawn("Sofa_01", chunk, new Vector3(12, 0, 12 + hj - 0.5f), 180);
                ChunkWorld.Spawn("ArmChair_01", chunk, new Vector3(12 - 1.6f, 0, 12 + hj - 1.8f), 130);
                ChunkWorld.Spawn("ArmChair_01", chunk, new Vector3(12 + 1.6f, 0, 12 + hj - 1.8f), -130);
                ChunkWorld.Spawn("CoffeeTable_01", chunk, new Vector3(12, 0, 12 + hj - 1.9f), 0, 0.7f);
                ChunkWorld.Spawn("Chandelier_02", chunk, new Vector3(12, H - 0.9f, 12), 0, 1.1f);
                var l = ChunkWorld.Point(chunk, new Vector3(12, H - 0.6f, 12), 2200f, 12f, 2700f, rng.NextDouble() < 0.2);
                if (rng.NextDouble() < 0.5) ChunkWorld.Spawn("Bookshelf_Hotel", chunk, new Vector3(12 - hj + 0.25f, 0, 12 - hj + 1.4f), 90);
            }
            else ChunkWorld.Point(chunk, new Vector3(12, H - 0.2f, 12), 500f, 6f, 2700f, rng.NextDouble() < 0.1);
            if (rng.NextDouble() < 0.2) ChunkWorld.SpawnItem(rng.NextDouble() < 0.6 ? "almond_water" : "battery", chunk, new Vector3(12.6f, 0.15f, 11.4f));
            g.Emit(chunk, "Geo");
        }

        /// <summary>Wall face from p0 to p1 (facing its left side): wood wainscot below 1m, damask above, chair rail.</summary>
        static void WallBand(ChunkGeo g, Vector2 p0, Vector2 p1)
        {
            g.Face("L5_WoodPanel", p0, p1, 0, 1.0f);
            g.Face("L5_Damask", p0, p1, 1.0f, H);
            var dir = (p1 - p0).normalized;
            var nrm = new Vector2(-dir.y, dir.x);
            g.Face("L5_WoodPanel", p0 + nrm * 0.03f, p1 + nrm * 0.03f, 0.98f, 1.05f);
            g.Quad("L5_WoodPanel", new Vector3(p0.x, 1.05f, p0.y), new Vector3(p0.x + nrm.x * 0.03f, 1.05f, p0.y + nrm.y * 0.03f),
                new Vector3(p1.x + nrm.x * 0.03f, 1.05f, p1.y + nrm.y * 0.03f), new Vector3(p1.x, 1.05f, p1.y));
        }

        static void FakeDoor(Transform chunk, Vector2 wall, float yaw, int number)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var frame = ChunkWorld.Spawn("Hotel_DoorFrame", chunk, new Vector3(wall.x, 0, wall.y) + fwd * 0.03f, yaw);
            if (!frame) return;
            var leaf = Instantiate(Registry.Prefab("Hotel_DoorLeaf"), frame.transform);
            leaf.transform.localPosition = new Vector3(0.475f, 0, 0);
            var d = leaf.AddComponent<Door>();
            d.requiredKeyId = "never";
            d.lockedMessage = "Chambre " + number + ". Fermée à clé.";
            string sn = number.ToString();
            for (int i = 0; i < sn.Length; i++)
            {
                var dg = ChunkWorld.Spawn("Digit_" + sn[i], leaf.transform, new Vector3(-0.475f - (i - (sn.Length - 1) * 0.5f) * 0.04f, 1.635f, 0.034f), 0, 1.7f);
                if (dg) foreach (var col in dg.GetComponentsInChildren<Collider>()) Destroy(col);
            }
        }

        static void Sconce(Transform chunk, Vector2 wall, float yaw, System.Random rng)
        {
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var p = new Vector3(wall.x, 2.1f, wall.y);
            ChunkWorld.Spawn("Sconce_ArtDeco", chunk, p, yaw);
            if (rng.NextDouble() < 0.85) ChunkWorld.Point(chunk, p + fwd * 0.14f + Vector3.up * 0.05f, 320f, 6.5f, 2600f, rng.NextDouble() < 0.12);
        }
    }
}
