using System.Collections.Generic;
using SyncRooms.Player;
using SyncRooms.World;
using UnityEngine;

namespace SyncRooms.Gen
{
    /// <summary>Level 2 — "Pipe Dreams": a very long, hot, narrow concrete corridor lined with pipes (along +Z).</summary>
    public class PipeChunks : ChunkBuilder
    {
        const float S = 24f, H = 3.0f, X0 = 10.7f, X1 = 13.3f;
        public int seed = 202;
        public int exitChunk = 10;

        public override bool Exists(Vector2Int c) => c.x == 0 && c.y <= exitChunk;

        public override void Build(Transform chunk, Vector2Int c, System.Random rng)
        {
            var g = new ChunkGeo();
            g.Surface("L4_Concrete", SurfaceType.Concrete);
            g.Surface("Steel", SurfaceType.Metal);
            g.Floor("L4_Concrete", Rect.MinMaxRect(X0, 0, X1, S), 0);
            g.Floor("Steel", Rect.MinMaxRect(11.6f, 0, 12.4f, S), 0.01f);
            g.Floor("L5_OldConcrete", Rect.MinMaxRect(X0, 0, X1, S), H, false);
            bool sideDoor = rng.NextDouble() < 0.35;
            float doorZ = 6 + (float)rng.NextDouble() * 12;
            var leftOps = sideDoor ? new List<Vector3> { new(doorZ, 1.0f, 2.12f) } : null;
            g.Wall("L5_OldConcrete", "L5_OldConcrete", new Vector2(X0 - 0.15f, 0), new Vector2(X0 - 0.15f, S), 0.3f, 0, H, leftOps);
            g.Wall("L5_OldConcrete", "L5_OldConcrete", new Vector2(X1 + 0.15f, 0), new Vector2(X1 + 0.15f, S), 0.3f, 0, H);
            if (sideDoor)
            {
                var frame = ChunkWorld.Spawn("Office_DoorFrame", chunk, new Vector3(X0 - 0.15f, 0, doorZ), 90);
                if (frame)
                {
                    var leaf = Instantiate(Registry.Prefab("Office_DoorLeaf"), frame.transform);
                    leaf.transform.localPosition = new Vector3(0.44f, 0, 0);
                    var d = leaf.AddComponent<Door>();
                    d.requiredKeyId = "never";
                    d.lockedMessage = "La poignée est brûlante.";
                }
            }
            // pipes
            for (int k = 0; k < 4; k++)
            {
                float z = 3 + k * 6;
                if (!(sideDoor && Mathf.Abs(z - doorZ) < 3.2f)) ChunkWorld.Spawn("PipeRun", chunk, new Vector3(X0, 0, z), 90);
                ChunkWorld.Spawn("PipeRun", chunk, new Vector3(X1, 0, z), -90);
                ChunkWorld.Spawn("PipeCeiling", chunk, new Vector3(12, H, z), 0);
            }
            // lighting: some stretches are pitch black
            bool dark = Mathf.Abs(c.y) > 1 && rng.NextDouble() < 0.3;
            if (!dark)
                for (int k = 0; k < 4; k++)
                {
                    bool left = k % 2 == 0;
                    var p = new Vector3(left ? X0 + 0.1f : X1 - 0.1f, 2.4f, 3 + k * 6);
                    ChunkWorld.Spawn("industrial_caged_sconce", chunk, p, left ? 90 : -90, 0.35f);
                    ChunkWorld.Point(chunk, p + new Vector3(left ? 0.35f : -0.35f, -0.1f, 0), 650f, 7f, 2900f, rng.NextDouble() < 0.15);
                }
            if (rng.NextDouble() < 0.5)
            {
                var steam = new GameObject("Steam");
                steam.transform.SetParent(chunk, false);
                steam.transform.localPosition = new Vector3(12, 2.5f, (float)rng.NextDouble() * S);
                var src = steam.AddComponent<AudioSource>();
                src.clip = Core.Sfx.Random("Ambience/amb_steam");
                src.loop = true; src.spatialBlend = 1; src.maxDistance = 12; src.rolloffMode = AudioRolloffMode.Linear; src.volume = 0.5f;
                src.Play();
            }
            if (rng.NextDouble() < 0.3) ChunkWorld.SpawnItem(rng.NextDouble() < 0.6 ? "almond_water" : "battery", chunk, new Vector3(12 + (rng.NextDouble() < 0.5 ? -0.8f : 0.8f), 0.15f, (float)rng.NextDouble() * S));
            if (c.y == exitChunk)
            {
                // the corridor finally ends with a door
                g.Wall("L5_OldConcrete", "L5_OldConcrete", new Vector2(X0 - 0.3f, S), new Vector2(X1 + 0.3f, S), 0.3f, 0, H, new List<Vector3> { new(1.6f, 1.0f, 2.12f) });
                var frame = ChunkWorld.Spawn("Office_DoorFrame", chunk, new Vector3(12, 0, S), 0);
                ChunkWorld.Spawn("ExitSign", chunk, new Vector3(12, 2.85f, S - 0.16f), 180);
                var ex = new GameObject("Exit → Level 1");
                ex.transform.SetParent(chunk, false);
                ex.transform.localPosition = new Vector3(12, 1.1f, S - 0.1f);
                ex.AddComponent<BoxCollider>().size = new Vector3(1f, 2.1f, 0.3f);
                var le = ex.AddComponent<LevelExit>();
                le.targetScene = "Level1";
                le.prompt = "[E] Ouvrir la porte";
                le.exitKind = ExitKind.Walk;
                ChunkWorld.Point(chunk, new Vector3(12, 2.5f, S - 1f), 500f, 5f, 3000f);
            }
            g.Emit(chunk, "Geo");
        }
    }
}
