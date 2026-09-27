using System.Collections.Generic;
using SyncRooms.World;
using SyncRooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.Gen
{
    /// <summary>Per-level chunk content generator (deterministic from the chunk coordinates).</summary>
    public abstract class ChunkBuilder : MonoBehaviour
    {
        public abstract void Build(Transform chunk, Vector2Int c, System.Random rng);
        /// <summary>Chunks that must never be generated (e.g. a 1D corridor level).</summary>
        public virtual bool Exists(Vector2Int c) => true;
    }

    /// <summary>
    /// Streams an endless level around the player: builds chunks inside `radius`, destroys them beyond.
    /// Generation is spread over frames (one chunk per frame) to avoid hitches.
    /// </summary>
    public class ChunkWorld : MonoBehaviour
    {
        public static ChunkWorld Current { get; private set; }
        public float chunkSize = 24f;
        public int radius = 2;
        public int seed = 1234;
        public ChunkBuilder builder;
        public Vector3 origin;

        readonly Dictionary<Vector2Int, GameObject> _chunks = new();
        Transform _player;

        void Awake()
        {
            Current = this;
            if (builder == null) builder = GetComponent<ChunkBuilder>();
        }

        void Start()
        {
            // Build the spawn area synchronously so the player never falls through the floor.
            Locate();
            var pc = PlayerChunk();
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    Ensure(pc + new Vector2Int(x, z));
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Locate()
        {
            if (_player) return;
            var p = FindAnyObjectByType<Player.FirstPersonController>();
            _player = p ? p.transform : (Camera.main ? Camera.main.transform : null);
        }

        public Vector2Int ChunkOf(Vector3 p) => new(Mathf.FloorToInt((p.x - origin.x) / chunkSize), Mathf.FloorToInt((p.z - origin.z) / chunkSize));
        Vector2Int PlayerChunk() => _player ? ChunkOf(_player.position) : Vector2Int.zero;

        public static int Hash(int seed, int x, int z)
        {
            unchecked
            {
                int h = seed * 73856093 ^ x * 19349663 ^ z * 83492791;
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return h;
            }
        }

        bool Ensure(Vector2Int c)
        {
            if (_chunks.ContainsKey(c)) return false;
            if (!builder.Exists(c)) { _chunks[c] = null; return false; }
            var go = new GameObject($"Chunk {c.x},{c.y}");
            go.transform.SetParent(transform, false);
            go.transform.position = origin + new Vector3(c.x * chunkSize, 0, c.y * chunkSize);
            builder.Build(go.transform, c, new System.Random(Hash(seed, c.x, c.y)));
            _chunks[c] = go;
            return true;
        }

        void Update()
        {
            Locate();
            if (_player == null) return;
            var pc = PlayerChunk();
            // build nearest missing chunk first
            bool built = false;
            int rad = radius + RenderDistance.ExtraChunks;
            for (int r = 0; r <= rad && !built; r++)
                for (int x = -r; x <= r && !built; x++)
                    for (int z = -r; z <= r && !built; z++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != r) continue;
                        built = Ensure(pc + new Vector2Int(x, z));
                    }
            if (built) return;
            // unload far chunks
            Vector2Int? drop = null;
            foreach (var kv in _chunks)
                if (Mathf.Max(Mathf.Abs(kv.Key.x - pc.x), Mathf.Abs(kv.Key.y - pc.y)) > rad + 1) { drop = kv.Key; break; }
            if (drop.HasValue)
            {
                if (_chunks[drop.Value]) Destroy(_chunks[drop.Value]);
                _chunks.Remove(drop.Value);
            }
        }

        // ------------------------------------------------------------ helpers for builders
        public static GameObject Spawn(string prefab, Transform parent, Vector3 localPos, float yaw, float scale = 1f)
        {
            var p = Registry.Prefab(prefab);
            if (!p) return null;
            var go = Instantiate(p, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public static GameObject SpawnItem(string id, Transform parent, Vector3 localPos)
        {
            var def = Registry.Item(id);
            if (!def || !def.worldPrefab) return null;
            var go = Instantiate(def.worldPrefab, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            return go;
        }

        public static Light Point(Transform parent, Vector3 localPos, float lumen, float range, float kelvin, bool flicker = false)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddHDLight(LightType.Point);
            var l = go.GetComponent<Light>();
            l.lightUnit = LightUnit.Lumen;
            l.intensity = lumen;
            l.range = range;
            l.useColorTemperature = true;
            l.colorTemperature = kelvin;
            l.shadows = LightShadows.None;
            var fl = go.AddComponent<FlickerLight>();
            fl.lights = new[] { l };
            fl.emissiveRenderers = new Renderer[0];
            fl.flicker = flicker;
            fl.cullDistance = 30f;
            return l;
        }
    }
}
