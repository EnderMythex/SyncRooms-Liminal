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
    /// Level 37 — "Sublimity". An endless submerged complex of white ceramic tiles and blue-green water:
    /// halls linked by openings, pools (shallow and deep), flooded halls, pillar forests, skylights pouring
    /// sunlight into volumetric haze, and a dark stairway leading away (the end of the demo).
    /// </summary>
    public static class Level37Builder
    {
        const int NX = 5, NZ = 4;
        const float Hall = 14f, T = 0.6f, H = 4.6f;
        const float WaterY = -0.12f;
        static Transform _root, _props, _lights;
        static GeoBuilder _geo;
        static System.Random _rng;
        static float R() => (float)_rng.NextDouble();

        enum Kind { Pool, DeepPool, Flooded, Pillars, Skylight, Pit }

        static Rect HallRect(int i, int j) => new(i * (Hall + T), j * (Hall + T), Hall, Hall);

        [MenuItem("SyncRooms/Levels/Build Level 37 (Sublimity)")]
        public static void Build()
        {
            var scene = LevelKit.NewScene("Level37");
            _rng = new System.Random(37);
            _root = LevelKit.Group("Level37").transform;
            _props = LevelKit.Group("Props", _root).transform;
            _lights = LevelKit.Group("Lights", _root).transform;
            _geo = new GeoBuilder(15f);
            _geo.SetSurface("L37_TileSmall", SurfaceType.Tile);
            _geo.SetSurface("L37_TileLarge", SurfaceType.Tile);

            var kinds = new Kind[NX, NZ];
            for (int i = 0; i < NX; i++)
                for (int j = 0; j < NZ; j++)
                {
                    float r = R();
                    kinds[i, j] = r < 0.28f ? Kind.Pool : r < 0.42f ? Kind.DeepPool : r < 0.6f ? Kind.Flooded : r < 0.8f ? Kind.Pillars : Kind.Skylight;
                }
            kinds[2, 1] = Kind.Flooded;          // arrival hall
            kinds[1, 1] = Kind.Skylight;
            kinds[NX - 1, NZ - 1] = Kind.Pit;    // the way out

            for (int i = 0; i < NX; i++)
                for (int j = 0; j < NZ; j++)
                    BuildHall(i, j, kinds[i, j]);
            _geo.Emit(_root, "Architecture");

            LevelKit.Sun(_lights, new Vector3(68, -30, 0), 100000f, 5800f);
            LevelKit.GlobalVolume(_root, "Level37", new LevelKit.Look
            {
                expMin = 8.5f, expMax = 13.5f, expComp = 0.8f, fogMFP = 42f, fogMaxHeight = 6f, fogAlbedo = new Color(0.92f, 0.98f, 1f),
                bloom = 0.35f, vignette = 0.15f, grain = 0.08f, saturation = -4f, contrast = 6f, temperature = -4f,
                skyType = (int)SkyType.PhysicallyBased, clouds = false, shadowDistance = 80f, chroma = 0.02f
            });

            var arrival = HallRect(2, 1);
            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(_root, false);
            spawn.position = new Vector3(arrival.center.x, -0.4f, arrival.center.y - 3f);
            LevelKit.Player(_root, spawn.position, 0);
            var ls = LevelKit.Settings(_root, spawn, -1.2f, 0f, new[] { "Ambience/amb_pool" }, 0.3f); // negative drain = the level soothes
            ls.GetComponent<LevelAmbience>().oneShots = new[]
            {
                new LevelAmbience.RandomOneShot { clip = "Ambience/amb_drip", minDelay = 6, maxDelay = 18, volume = 0.45f, minDist = 3, maxDist = 14 },
            };
            var rz = new GameObject("Reverb").AddComponent<AudioReverbZone>();
            rz.transform.SetParent(_root, false);
            rz.transform.position = new Vector3(NX * (Hall + T) / 2, 2, NZ * (Hall + T) / 2);
            rz.reverbPreset = AudioReverbPreset.Bathroom;
            rz.minDistance = 200f; rz.maxDistance = 300f;
            Items(kinds);
            LevelKit.SaveScene(scene, "Level37");
        }

        static void BuildHall(int i, int j, Kind kind)
        {
            var r = HallRect(i, j);
            var ops = new[] { new List<O>(), new List<O>(), new List<O>(), new List<O>() };
            // openings toward neighbours (same random choice seen from both sides: seeded by the wall id)
            void Link(int side, bool exists, int wallId)
            {
                if (!exists) return;
                var rr = new System.Random(wallId * 7919);
                int n = rr.NextDouble() < 0.35 ? 2 : 1;
                for (int k = 0; k < n; k++)
                {
                    float at = n == 1 ? 3.5f + (float)rr.NextDouble() * 7f : (k == 0 ? 3.2f : 10.8f);
                    float w = 2.6f + (float)rr.NextDouble() * 1.4f;
                    ops[side].Add(new O(at, w, 3.4f + (float)rr.NextDouble() * 0.6f));
                }
            }
            Link(0, j > 0, 1000 + i * 31 + (j - 1) * 97);           // S (wall between j-1 and j)
            Link(1, j < NZ - 1, 1000 + i * 31 + j * 97);           // N
            Link(2, i > 0, 5000 + (i - 1) * 53 + j * 131);          // W
            Link(3, i < NX - 1, 5000 + i * 53 + j * 131);           // E
            if (kind == Kind.Pit) ops[1].Add(new O(7f, 3f, 3.6f)); // tunnel mouth (north wall, map edge)

            float floorY = kind == Kind.Flooded ? -0.45f : 0f;
            _geo.InnerWalls("L37_TileSmall", r, floorY, H, T, ops, "L37_TileLarge");
            // cove along the floor/wall edge + frieze band
            string lit = kind == Kind.Skylight ? null : "EmissiveSky";

            // ceiling (with a central skylight hole for skylight halls)
            if (kind == Kind.Skylight)
            {
                float hs = 1.7f;
                var c = r.center;
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, c.y - hs), H, false);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, c.y + hs, r.xMax, r.yMax), H, false);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, c.y - hs, c.x - hs, c.y + hs), H, false);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(c.x + hs, c.y - hs, r.xMax, c.y + hs), H, false);
                LevelKit.Place("Skylight", new Vector3(c.x, H, c.y), 0, _props);
                // roof slab on top so the sun only enters through the opening
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin - T, r.yMin - T, r.xMax + T, c.y - hs), H + 0.5f);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin - T, c.y + hs, r.xMax + T, r.yMax + T), H + 0.5f);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin - T, c.y - hs, c.x - hs, c.y + hs), H + 0.5f);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(c.x + hs, c.y - hs, r.xMax + T, c.y + hs), H + 0.5f);
            }
            else
            {
                _geo.Floor("L37_TileLarge", r, H, false);
                _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin - T, r.yMin - T, r.xMax + T, r.yMax + T), H + 0.5f);
                bool dark = R() < 0.18f;
                if (!dark)
                {
                    // bright ceiling light panels
                    for (int k = -1; k <= 1; k += 2)
                    {
                        bool withLight = k > 0;
                        var p = new Vector3(r.center.x + k * 3.5f, H - 0.01f, r.center.y);
                        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        q.name = "Light Panel";
                        q.transform.SetParent(_props, false);
                        q.transform.position = p;
                        q.transform.rotation = Quaternion.Euler(90, 0, 0);
                        q.transform.localScale = new Vector3(1.2f, 5f, 1f);
                        q.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get("EmissiveSky");
                        Object.DestroyImmediate(q.GetComponent<Collider>());
                        if (withLight) LevelKit.RectLight(_lights, new Vector3(r.center.x, H - 0.03f, r.center.y), Quaternion.Euler(90, 0, 0), new Vector2(8f, 5f), 30000f, Color.white, 16f, false, 6800f);
                    }
                }
            }

            switch (kind)
            {
                case Kind.Pool: Pool(r, 1.3f, false); break;
                case Kind.DeepPool: Pool(r, 2.8f, true); break;
                case Kind.Flooded: Flooded(r); break;
                case Kind.Pillars: _geo.Floor("L37_TileLarge", r, 0f); Pillars(r); break;
                case Kind.Skylight: _geo.Floor("L37_TileLarge", r, 0f); Pillars(r, true); break;
                case Kind.Pit: Pit(r); break;
            }
            var probe = LevelKit.ReflectionProbe(_lights, new Vector3(r.center.x, H * 0.5f, r.center.y), new Vector3(Hall, H + 1f, Hall));
        }

        static void Pool(Rect r, float depth, bool deep)
        {
            const float deck = 1.6f;
            var b = Rect.MinMaxRect(r.xMin + deck, r.yMin + deck, r.xMax - deck, r.yMax - deck);
            // deck strips around the basin
            _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, b.yMin), 0f);
            _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, b.yMax, r.xMax, r.yMax), 0f);
            _geo.Floor("L37_TileLarge", Rect.MinMaxRect(r.xMin, b.yMin, b.xMin, b.yMax), 0f);
            _geo.Floor("L37_TileLarge", Rect.MinMaxRect(b.xMax, b.yMin, r.xMax, b.yMax), 0f);
            // basin: inward walls going down + floor
            _geo.InnerWalls("L37_TileSmall", b, -depth, 0f, 0.001f, null);
            _geo.Floor("L37_TileSmall", b, -depth);
            // coping (rounded feel: two thin boxes)
            _geo.Box("L37_TileLarge", new Vector3(b.xMin - 0.25f, 0f, b.yMin - 0.25f), new Vector3(b.xMax + 0.25f, 0.04f, b.yMin), true, false);
            _geo.Box("L37_TileLarge", new Vector3(b.xMin - 0.25f, 0f, b.yMax), new Vector3(b.xMax + 0.25f, 0.04f, b.yMax + 0.25f), true, false);
            _geo.Box("L37_TileLarge", new Vector3(b.xMin - 0.25f, 0f, b.yMin), new Vector3(b.xMin, 0.04f, b.yMax), true, false);
            _geo.Box("L37_TileLarge", new Vector3(b.xMax, 0f, b.yMin), new Vector3(b.xMax + 0.25f, 0.04f, b.yMax), true, false);
            // steps down into the pool along the south edge
            int steps = Mathf.CeilToInt(depth / 0.28f);
            for (int s = 0; s < steps; s++)
            {
                float top = -(s + 1) * (depth / steps);
                _geo.Box("L37_TileSmall", new Vector3(b.center.x - 1.6f, -depth, b.yMin + s * 0.35f), new Vector3(b.center.x + 1.6f, top + depth / steps, b.yMin + (s + 1) * 0.35f), true, false);
            }
            LevelKit.Place("PoolLadder", new Vector3(b.xMax - 1.2f, 0f, b.yMax), 180, _props);
            LevelKit.Place("DrainGrate", new Vector3(b.center.x, -depth + 0.01f, b.center.y), 0, _props);
            Water(b, -depth, deep);
            LevelKit.Audio3D(_props, new Vector3(b.center.x, 0.2f, b.center.y), "Ambience/amb_water_lapping", 0.35f, 14f);
        }

        static void Flooded(Rect r)
        {
            _geo.Floor("L37_TileSmall", r, -0.45f);
            Water(r, -0.45f, false);
            if (R() < 0.6f) Pillars(r, false, -0.45f);
        }

        static void Water(Rect b, float bottom, bool underwater)
        {
            var go = new GameObject("Water");
            go.transform.SetParent(_props, false);
            go.transform.position = new Vector3(b.center.x, WaterY, b.center.y);
            go.transform.localScale = new Vector3(b.width, 1, b.height);
            var ws = go.AddComponent<WaterSurface>();
            ws.surfaceType = WaterSurfaceType.Pool;
            ws.geometryType = WaterGeometryType.Quad;
            ws.refractionColor = new Color(0.1f, 0.62f, 0.6f);
            ws.scatteringColor = new Color(0.02f, 0.38f, 0.34f);
            ws.absorptionDistance = 7f;
            ws.maxRefractionDistance = 1.2f;
            ws.ripplesWindSpeed = 2.5f;       // "a minimal and constant rippling"
            ws.caustics = true;
            ws.causticsIntensity = 0.9f;
            ws.causticsResolution = WaterSurface.WaterCausticsResolution.Caustics256;
            ws.virtualPlaneDistance = Mathf.Max(0.3f, WaterY - bottom);
            ws.startSmoothness = 0.97f;

            var zone = new GameObject("WaterZone");
            zone.transform.SetParent(_props, false);
            float depth = WaterY - bottom;
            zone.transform.position = new Vector3(b.center.x, bottom + depth * 0.5f, b.center.y);
            var bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(b.width, depth + 0.1f, b.height);
            zone.AddComponent<WaterZone>().surfaceHeight = WaterY;
            if (underwater)
            {
                ws.underWater = true;
                ws.volumeBounds = bc;
                ws.underWaterScatteringColor = new Color(0.02f, 0.38f, 0.34f);
            }
        }

        static void Pillars(Rect r, bool ring = false, float y = 0f)
        {
            for (int a = -1; a <= 1; a++)
                for (int b = -1; b <= 1; b++)
                {
                    if (ring && a == 0 && b == 0) continue;
                    if (R() < 0.15f) continue;
                    var p = new Vector3(r.center.x + a * 4.2f, y, r.center.y + b * 4.2f);
                    var pl = LevelKit.Place("Pillar_Tiled", p, 0, _props);
                    if (pl && y < 0) pl.transform.localScale = new Vector3(1, (H - y) / 4.6f, 1);
                }
        }

        static void Pit(Rect r)
        {
            _geo.Floor("L37_TileLarge", r, 0f);
            // tunnel from the north opening, going down into darkness
            float x0 = r.center.x - 1.5f, x1 = r.center.x + 1.5f;
            float z = r.yMax + T * 0.5f;
            int steps = 24;
            for (int s = 0; s < steps; s++)
            {
                float top = -(s + 1) * 0.2f;
                _geo.Box("L37_TileSmall", new Vector3(x0, top - 0.3f, z + s * 0.32f), new Vector3(x1, top, z + (s + 1) * 0.32f), true, false);
            }
            float len = steps * 0.32f + 8f;
            _geo.InnerWalls("L37_TileSmall", Rect.MinMaxRect(x0, z, x1, z + len), -6f, 3.6f, 0.2f,
                new[] { new List<O> { new O(1.5f, 3f, 3.6f) }, new List<O>(), new List<O>(), new List<O>() });
            _geo.Floor("L37_TileSmall", Rect.MinMaxRect(x0, z + steps * 0.32f, x1, z + len), -steps * 0.2f);
            _geo.Floor("L37_TileLarge", Rect.MinMaxRect(x0, z, x1, z + len), 3.6f, false);
            _geo.Floor("L37_TileLarge", Rect.MinMaxRect(x0 - 0.2f, z, x1 + 0.2f, z + len), 4.1f);
            var trig = new GameObject("Exit → End (dark tunnel)");
            trig.transform.SetParent(_root, false);
            trig.transform.position = new Vector3(r.center.x, -steps * 0.2f + 1f, z + len - 3f);
            var bc = trig.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(3f, 2.5f, 3f);
            var le = trig.AddComponent<LevelExit>();
            le.isTrigger = true;
            le.endGame = true;
            le.targetScene = "MainMenu";
            // a note warning at the top of the stairs
            var n = LevelKit.Place("Item_Note", new Vector3(r.center.x + 2.2f, 0.01f, r.yMax - 1f), 12, _props);
            if (n)
            {
                var rn = n.AddComponent<ReadableNote>();
                rn.title = "Gravé dans le carrelage";
                rn.text = "L'escalier descend vers le Level 43.\n\nPersonne n'en est revenu pour dire ce qu'il y a en bas.\n\nPrenez votre temps avant de descendre. Ici, au moins, vous êtes en paix.";
                n.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.05f, 0.25f);
            }
        }

        static void Items(Kind[,] kinds)
        {
            var it = LevelKit.Group("Items", _root).transform;
            var a = HallRect(2, 1);
            LevelKit.PlaceItem("note_37_1", new Vector3(a.xMin + 1f, 0.2f, a.yMax - 1f), 0, it);
            var s = HallRect(1, 1);
            LevelKit.PlaceItem("almond_water", new Vector3(s.center.x + 1f, 0.2f, s.center.y), 0, it);
            LevelKit.PlaceItem("almond_water", new Vector3(HallRect(3, 2).xMin + 0.8f, 0.3f, HallRect(3, 2).yMin + 0.8f), 0, it);
            LevelKit.Place("rubber_duck_toy", new Vector3(HallRect(0, 0).center.x, WaterY - 0.05f, HallRect(0, 0).center.y), 40, _props);
            LevelKit.Place("lifebuoy", new Vector3(HallRect(1, 2).xMin + 0.05f, 1.6f, HallRect(1, 2).center.y), 90, _props);
        }
    }
}
