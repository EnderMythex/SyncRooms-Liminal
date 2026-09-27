using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.Monsters
{
    /// <summary>The Level 7 leviathan: circles below the surface around a center, sometimes rising close to it.</summary>
    public class Swimmer : MonoBehaviour
    {
        public Vector3 center;
        public float radius = 60f, depth = 14f, speed = 4f;
        float _angle, _callTimer = 25f;

        void Update()
        {
            _angle += speed / radius * Time.deltaTime;
            float rise = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 0.05f)), 6f);   // rare approach to the surface
            float y = -Mathf.Lerp(depth, 3.5f, rise);
            var pos = center + new Vector3(Mathf.Cos(_angle) * radius, y, Mathf.Sin(_angle) * radius);
            var dir = pos - transform.position;
            transform.position = pos;
            if (dir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(-dir.normalized);
            _callTimer -= Time.deltaTime;
            if (_callTimer <= 0f)
            {
                _callTimer = Random.Range(35f, 80f);
                Sfx.PlayAt("Monsters/leviathan_call", transform.position + Vector3.up * 10f, 1f, 400f, 0.1f);
            }
        }
    }
}
