using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Plays the level's 2D ambient loops and random positional one-shots around the player.</summary>
    public class LevelAmbience : MonoBehaviour
    {
        [System.Serializable]
        public class RandomOneShot
        {
            public string clip;           // Resources/Audio base path
            public float minDelay = 15f, maxDelay = 45f;
            public float volume = 0.5f;
            public float minDist = 6f, maxDist = 20f;
            [HideInInspector] public float timer;
        }

        public RandomOneShot[] oneShots;

        void Start()
        {
            var lvl = GetComponent<LevelSettings>();
            if (lvl == null) lvl = LevelSettings.Current;
            if (lvl != null && lvl.ambientLoops != null)
            {
                foreach (var path in lvl.ambientLoops)
                {
                    var clip = Sfx.Random(path);
                    if (!clip) continue;
                    var src = gameObject.AddComponent<AudioSource>();
                    src.clip = clip;
                    src.loop = true;
                    src.spatialBlend = 0f;
                    src.volume = lvl.ambientVolume;
                    src.time = Random.Range(0f, clip.length * 0.9f);
                    src.Play();
                }
            }
            if (oneShots != null)
                foreach (var o in oneShots) o.timer = Random.Range(o.minDelay * 0.3f, o.maxDelay);
        }

        void Update()
        {
            if (oneShots == null || Camera.main == null) return;
            var p = Camera.main.transform.position;
            foreach (var o in oneShots)
            {
                o.timer -= Time.deltaTime;
                if (o.timer > 0f) continue;
                o.timer = Random.Range(o.minDelay, o.maxDelay);
                Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(o.minDist, o.maxDist);
                Sfx.PlayAt(o.clip, p + new Vector3(dir.x, Random.Range(-0.5f, 1.5f), dir.y), o.volume, o.maxDist * 2f, 0.15f);
            }
        }
    }
}
