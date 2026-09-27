using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.Gen
{
    /// <summary>Level 37 — "Sublimity": endless grid of tiled halls, pools, flooded rooms, pillars and skylights.</summary>
    public class PoolChunks : ChunkBuilder
    {
        const float S = 14.6f, T = 0.6f, H = 4.6f, WaterY = -0.12f;
        public int seed = 37;

        enum Kind { Pool, Deep, Flooded, Pillars, Skylight, Pit }

        List<Vector3> Edge(bool west, int x, int z)
        {
            var r = new System.Random(ChunkWorld.Hash(seed * 11 + (west ? 3 : 5), x, z));
            var l = new List<Vector3>();
            int n = r.NextDouble() < 0.35 ? 2 : 1;
            for (int k = 0; k < n; k++)
            {
                float at = n == 1 ? 3.8f + (float)r.NextDouble() * 7f : (k == 0 ? 3.4f : 11.2f);
                l.Add(new Vector3(at, 2.6f + (float)r.NextDouble() * 1.2f, 3.4f + (float)r.NextDouble() * 0.6f));
            }
            return l;
        }

        public override void Build(Transform chunk, Vector2Int c, System.Random rng)
        {
            var g = new ChunkGeo();
            g.Surface("L37_TileSmall", SurfaceType.Tile);
            g.Surface("L37_TileLarge", SurfaceType.Tile);
            int hash = Mathf.Abs(ChunkWorld.Hash(seed, c.x, c.y));
            double k = rng.NextDouble();
            var kind = k < 0.28 ? Kind.Pool : k < 0.4 ? Kind.Deep : k < 0.58 ? Kind.Flooded : k < 0.8 ? Kind.Pillars : Kind.Skylight;
            if (c == Vector2Int.zero) kind = Kind.Flooded;
            if (c.sqrMagnitude >= 9 && hash % 23 == 7) kind = Kind.Pit;
            float floorY = kind == Kind.Flooded ? -0.45f : 0f;
            var r = Rect.MinMaxRect(T / 2, T / 2, S - T / 2, S - T / 2);

            g.Wall("L37_TileSmall", "L37_TileSmall", new Vector2(0, 0), new Vector2(S, 0), T, floorY - 0.5f, H, Edge(false, c.x, c.y));
            g.Wall("L37_TileSmall", "L37_TileSmall", new Vector2(0, 0), new Vector2(0, S), T, floorY - 0.5f, H, Edge(true, c.x, c.y));
            // roof slab over the whole chunk (sun only enters through skylights)
            if (kind != Kind.Skylight) g.Floor("L37_TileLarge", new Rect(0, 0, S, S), H + 0.5f);

            if (kind == Kind.Skylight)
            {
                float hs = 1.7f; var m = r.center;
                g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, m.y - hs), H, false);
                g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, m.y + hs, r.xMax, r.yMax), H, false);
                g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, m.y - hs, m.x - hs, m.y + hs), H, false);
                g.Floor("L37_TileLarge", Rect.MinMaxRect(m.x + hs, m.y - hs, r.xMax, m.y + hs), H, false);
                ChunkWorld.Spawn("Skylight", chunk, new Vector3(m.x, H, m.y), 0);
                // roof with an opening above the skylight
                g.Floor("L37_TileLarge", Rect.MinMaxRect(0, 0, S, m.y - hs), H + 0.5f);
                g.Floor("L37_TileLarge", Rect.MinMaxRect(0, m.y + hs, S, S), H + 0.5f);
                g.Floor("L37_TileLarge", Rect.MinMaxRect(0, m.y - hs, m.x - hs, m.y + hs), H + 0.5f);
                g.Floor("L37_TileLarge", Rect.MinMaxRect(m.x + hs, m.y - hs, S, m.y + hs), H + 0.5f);
            }
            else
            {
                g.Floor("L37_TileLarge", new Rect(0, 0, S, S), H, false);
                if (rng.NextDouble() > 0.15)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(q.GetComponent<Collider>());
                    q.transform.SetParent(chunk, false);
                    q.transform.localPosition = new Vector3(r.center.x, H - 0.01f, r.center.y);
                    q.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    q.transform.localScale = new Vector3(8f, 5f, 1f);
                    q.GetComponent<MeshRenderer>().sharedMaterial = Registry.Mat("EmissiveSky");
                    var lg = new GameObject("Panel Light");
                    lg.transform.SetParent(chunk, false);
                    lg.transform.localPosition = new Vector3(r.center.x, H - 0.03f, r.center.y);
                    lg.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    lg.AddHDLight(LightType.Rectangle);
                    var l = lg.GetComponent<Light>();
                    l.lightUnit = LightUnit.Lumen; l.intensity = 30000f; l.areaSize = new Vector2(8f, 5f);
                    l.useColorTemperature = true; l.colorTemperature = 6800f; l.range = 16f; l.shadows = LightShadows.None;
                }
            }

            switch (kind)
            {
                case Kind.Pool: Pool(g, chunk, r, 1.3f, false, rng); break;
                case Kind.Deep: Pool(g, chunk, r, 2.8f, true, rng); break;
                case Kind.Flooded:
                    g.Floor("L37_TileSmall", r, -0.45f);
                    Water(chunk, r, -0.45f, false);
                    if (rng.NextDouble() < 0.5) Pillars(chunk, r, -0.45f, rng);
                    break;
                case Kind.Pillars: g.Floor("L37_TileLarge", r, 0); Pillars(chunk, r, 0, rng); break;
                case Kind.Skylight: g.Floor("L37_TileLarge", r, 0); Pillars(chunk, r, 0, rng, true); break;
                case Kind.Pit: Pit(g, chunk, r); break;
            }
            if (rng.NextDouble() < 0.2) ChunkWorld.SpawnItem("almond_water", chunk, new Vector3(r.xMin + 0.8f, floorY + 0.2f, r.yMin + 0.8f));
            if (c == new Vector2Int(1, 0)) ChunkWorld.SpawnItem("note_37_1", chunk, new Vector3(r.xMin + 1f, 0.3f, r.yMax - 1f));
            g.Emit(chunk, "Geo");
        }

        static void Pool(ChunkGeo g, Transform chunk, Rect r, float depth, bool deep, System.Random rng)
        {
            const float deck = 1.6f;
            var b = Rect.MinMaxRect(r.xMin + deck, r.yMin + deck, r.xMax - deck, r.yMax - deck);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, b.yMin), 0);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, b.yMax, r.xMax, r.yMax), 0);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, b.yMin, b.xMin, b.yMax), 0);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(b.xMax, b.yMin, r.xMax, b.yMax), 0);
            // basin walls facing inward
            g.Face("L37_TileSmall", new Vector2(b.xMin, b.yMin), new Vector2(b.xMax, b.yMin), -depth, 0);
            g.Face("L37_TileSmall", new Vector2(b.xMax, b.yMax), new Vector2(b.xMin, b.yMax), -depth, 0);
            g.Face("L37_TileSmall", new Vector2(b.xMin, b.yMax), new Vector2(b.xMin, b.yMin), -depth, 0);
            g.Face("L37_TileSmall", new Vector2(b.xMax, b.yMin), new Vector2(b.xMax, b.yMax), -depth, 0);
            g.Floor("L37_TileSmall", b, -depth);
            g.Box("L37_TileLarge", new Vector3(b.xMin - 0.25f, 0, b.yMin - 0.25f), new Vector3(b.xMax + 0.25f, 0.04f, b.yMin), true, false);
            g.Box("L37_TileLarge", new Vector3(b.xMin - 0.25f, 0, b.yMax), new Vector3(b.xMax + 0.25f, 0.04f, b.yMax + 0.25f), true, false);
            int steps = Mathf.CeilToInt(depth / 0.28f);
            for (int s = 0; s < steps; s++)
            {
                float top = -s * (depth / steps);
                g.Box("L37_TileSmall", new Vector3(b.center.x - 1.6f, -depth, b.yMin + s * 0.35f), new Vector3(b.center.x + 1.6f, top, b.yMin + (s + 1) * 0.35f), true, false);
            }
            ChunkWorld.Spawn("PoolLadder", chunk, new Vector3(b.xMax - 1.2f, 0, b.yMax), 180);
            Water(chunk, b, -depth, deep);
            if (rng.NextDouble() < 0.1) ChunkWorld.Spawn("rubber_duck_toy", chunk, new Vector3(b.center.x, WaterY - 0.05f, b.center.y), rng.Next(0, 360));
        }

        static void Water(Transform chunk, Rect b, float bottom, bool underwater)
        {
            var go = new GameObject("Water");
            go.transform.SetParent(chunk, false);
            go.transform.localPosition = new Vector3(b.center.x, WaterY, b.center.y);
            go.transform.localScale = new Vector3(b.width, 1, b.height);
            var zone = new GameObject("WaterZone");
            zone.transform.SetParent(chunk, false);
            float depth = WaterY - bottom;
            zone.transform.localPosition = new Vector3(b.center.x, bottom + depth * 0.5f, b.center.y);
            var bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(b.width, depth + 0.1f, b.height);
            zone.AddComponent<WaterZone>().surfaceHeight = chunk.position.y + WaterY;
            var ws = go.AddComponent<WaterSurface>();
            ws.surfaceType = WaterSurfaceType.Pool;
            ws.geometryType = WaterGeometryType.Quad;
            ws.refractionColor = new Color(0.1f, 0.62f, 0.6f);
            ws.scatteringColor = new Color(0.02f, 0.38f, 0.34f);
            ws.absorptionDistance = 7f;
            ws.maxRefractionDistance = 1.2f;
            ws.ripplesWindSpeed = 2.5f;
            ws.caustics = true;
            ws.causticsIntensity = 0.9f;
            ws.virtualPlaneDistance = Mathf.Max(0.3f, depth);
            if (underwater) { ws.underWater = true; ws.volumeBounds = bc; }
        }

        static void Pillars(Transform chunk, Rect r, float y, System.Random rng, bool ring = false)
        {
            for (int a = -1; a <= 1; a++)
                for (int b = -1; b <= 1; b++)
                {
                    if ((ring && a == 0 && b == 0) || rng.NextDouble() < 0.15) continue;
                    var p = ChunkWorld.Spawn("Pillar_Tiled", chunk, new Vector3(r.center.x + a * 4.2f, y, r.center.y + b * 4.2f), 0);
                    if (p && y < 0) p.transform.localScale = new Vector3(1, (H - y) / 4.6f, 1);
                }
        }

        static void Pit(ChunkGeo g, Transform chunk, Rect r)
        {
            // floor with a stair shaft going down into darkness (the end)
            var hole = Rect.MinMaxRect(r.center.x - 1.5f, r.center.y - 4f, r.center.x + 1.5f, r.center.y + 4f);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, hole.yMin), 0);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, hole.yMax, r.xMax, r.yMax), 0);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, hole.yMin, hole.xMin, hole.yMax), 0);
            g.Floor("L37_TileLarge", Rect.MinMaxRect(hole.xMax, hole.yMin, r.xMax, hole.yMax), 0);
            int steps = 24;
            for (int s = 0; s < steps; s++)
                g.Box("L37_TileSmall", new Vector3(hole.xMin, -(s + 1) * 0.25f - 0.3f, hole.yMin + s * (hole.height / steps)), new Vector3(hole.xMax, -(s + 1) * 0.25f, hole.yMin + (s + 1) * (hole.height / steps)), true, false);
            g.Face("L37_TileSmall", new Vector2(hole.xMin, hole.yMin), new Vector2(hole.xMax, hole.yMin), -7, 0);
            g.Face("L37_TileSmall", new Vector2(hole.xMax, hole.yMax), new Vector2(hole.xMin, hole.yMax), -7, 0);
            g.Face("L37_TileSmall", new Vector2(hole.xMin, hole.yMax), new Vector2(hole.xMin, hole.yMin), -7, 0);
            g.Face("L37_TileSmall", new Vector2(hole.xMax, hole.yMin), new Vector2(hole.xMax, hole.yMax), -7, 0);
            var ex = new GameObject("Exit → End");
            ex.transform.SetParent(chunk, false);
            ex.transform.localPosition = new Vector3(hole.center.x, -5f, hole.yMax - 1f);
            var bc = ex.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(3f, 3f, 2f);
            var le = ex.AddComponent<LevelExit>();
            le.isTrigger = true;
            le.endGame = true;
            var n = ChunkWorld.Spawn("Item_Note", chunk, new Vector3(hole.xMax + 0.6f, 0.01f, hole.yMin - 0.4f), 15);
            if (n)
            {
                var rn = n.AddComponent<ReadableNote>();
                rn.title = "Gravé dans le carrelage";
                rn.text = "L'escalier descend vers le Level 43.\n\nPersonne n'en est revenu pour dire ce qu'il y a en bas.\n\nPrenez votre temps avant de descendre. Ici, au moins, vous êtes en paix.";
                n.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.05f, 0.25f);
            }
        }
    }
}
