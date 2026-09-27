using System.Collections.Generic;
using SyncRooms.Gen;
using UnityEngine;

namespace SyncRooms.Monsters
{
    /// <summary>
    /// Keeps up to `maxAlive` entities around the player: spawns them out of sight at a distance,
    /// despawns the ones left far behind. Optional condition (e.g. only at night).
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        public string[] prefabs;
        public int maxAlive = 2;
        public float minDistance = 22f, maxDistance = 40f, despawnDistance = 70f;
        public float interval = 20f;
        public float firstDelay = 45f;
        public bool onlyAtNight;
        public bool despawnWhenConditionFalse = true;

        readonly List<GameObject> _alive = new();
        float _timer;

        void Start() => _timer = firstDelay;

        void Update()
        {
            var cam = Camera.main;
            if (!cam) return;
            _alive.RemoveAll(g => g == null);
            bool ok = !onlyAtNight || (World.DayNightCycle.Current && World.DayNightCycle.Current.IsNight);
            if (!ok && despawnWhenConditionFalse)
            {
                foreach (var g in _alive) Destroy(g);
                _alive.Clear();
                return;
            }
            foreach (var g in _alive.ToArray())
                if ((g.transform.position - cam.transform.position).sqrMagnitude > despawnDistance * despawnDistance) { Destroy(g); _alive.Remove(g); }
            _timer -= Time.deltaTime;
            if (!ok || _timer > 0 || _alive.Count >= maxAlive || prefabs == null || prefabs.Length == 0) return;
            _timer = interval;
            TrySpawn(cam.transform);
        }

        void TrySpawn(Transform cam)
        {
            for (int i = 0; i < 12; i++)
            {
                float ang = Random.Range(100f, 260f); // behind-ish
                var dir = Quaternion.Euler(0, cam.eulerAngles.y + ang, 0) * Vector3.forward;
                var p = cam.position + dir * Random.Range(minDistance, maxDistance) + Vector3.up * 2f;
                if (!Physics.Raycast(p, Vector3.down, out var hit, 12f, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore)) continue;
                if (Physics.CheckSphere(hit.point + Vector3.up * 1.1f, 0.5f, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore)) continue;
                var prefab = Registry.Prefab(prefabs[Random.Range(0, prefabs.Length)]);
                if (!prefab) return;
                var go = Instantiate(prefab, hit.point + Vector3.up * 0.05f, Quaternion.Euler(0, Random.Range(0, 360), 0));
                _alive.Add(go);
                return;
            }
            // fallback (narrow corridors): appear on the player's own trail, far behind
            var trail = Player.PlayerTrail.All;
            for (int i = 0; i < trail.Count; i++)
            {
                float d = (trail[i] - cam.position).magnitude;
                if (d < minDistance || d > maxDistance) continue;
                var prefab = Registry.Prefab(prefabs[Random.Range(0, prefabs.Length)]);
                if (!prefab) return;
                _alive.Add(Instantiate(prefab, trail[i] + Vector3.up * 0.05f, Quaternion.identity));
                return;
            }
        }
    }
}
