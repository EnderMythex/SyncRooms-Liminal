using System.Collections.Generic;
using UnityEngine;

namespace SyncRooms.Core
{
    /// <summary>Loads clips from Resources/Audio and plays one-shots (2D or positional).</summary>
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip[]> Cache = new();
        static AudioSource _ui;

        /// <summary>Returns all variants for a base path, e.g. "Footsteps/fs_tile" -> fs_tile_1..n or fs_tile.</summary>
        public static AudioClip[] Variants(string basePath)
        {
            if (Cache.TryGetValue(basePath, out var c)) return c;
            var list = new List<AudioClip>();
            var single = Resources.Load<AudioClip>("Audio/" + basePath);
            if (single) list.Add(single);
            for (int i = 1; i <= 8; i++)
            {
                var v = Resources.Load<AudioClip>($"Audio/{basePath}_{i}");
                if (v) list.Add(v);
            }
            c = list.ToArray();
            Cache[basePath] = c;
            return c;
        }

        public static AudioClip Random(string basePath)
        {
            var v = Variants(basePath);
            return v.Length == 0 ? null : v[UnityEngine.Random.Range(0, v.Length)];
        }

        public static void Play2D(string basePath, float volume = 1f, float pitchJitter = 0.05f)
        {
            var clip = Random(basePath);
            if (!clip) return;
            if (_ui == null)
            {
                var go = new GameObject("UI Audio");
                Object.DontDestroyOnLoad(go);
                _ui = go.AddComponent<AudioSource>();
                _ui.spatialBlend = 0f;
                _ui.ignoreListenerPause = true;
            }
            _ui.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            _ui.PlayOneShot(clip, volume);
        }

        public static void PlayAt(string basePath, Vector3 pos, float volume = 1f, float maxDistance = 25f, float pitchJitter = 0.08f)
        {
            var clip = Random(basePath);
            if (!clip) return;
            var go = new GameObject("OneShot " + clip.name);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = maxDistance;
            src.minDistance = 1f;
            src.dopplerLevel = 0f;
            src.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            src.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, src.pitch) + 0.1f);
        }
    }
}
