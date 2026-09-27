using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>
    /// Fluorescent / bulb light: optional random flicker synced with an emissive renderer and a buzz sound.
    /// Also distance-culls itself to keep HDRP's light count under control in huge liminal spaces.
    /// </summary>
    public class FlickerLight : MonoBehaviour
    {
        public Light[] lights;
        public Renderer[] emissiveRenderers;
        public int emissiveMaterialIndex;
        public bool flicker;
        [Range(0, 1)] public float flickerChance = 0.3f;
        public AudioSource buzz;
        public float cullDistance = 38f;

        float[] _baseIntensity;
        Color[] _baseEmissive;
        float _timer;
        float _state = 1f;
        MaterialPropertyBlock _mpb;
        static readonly int EmissiveId = Shader.PropertyToID("_EmissiveColor");
        static Transform _viewer;

        void Start()
        {
            _baseIntensity = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) _baseIntensity[i] = lights[i] ? lights[i].intensity : 0;
            _mpb = new MaterialPropertyBlock();
            _baseEmissive = new Color[emissiveRenderers.Length];
            for (int i = 0; i < emissiveRenderers.Length; i++)
            {
                var mats = emissiveRenderers[i] ? emissiveRenderers[i].sharedMaterials : null;
                if (mats != null && emissiveMaterialIndex < mats.Length && mats[emissiveMaterialIndex])
                    _baseEmissive[i] = mats[emissiveMaterialIndex].GetColor(EmissiveId);
            }
            _timer = Random.Range(0.5f, 6f);
            if (flicker) SetLevel(1f); // property blocks only where needed (keeps SRP batching for static lights)
        }

        void Update()
        {
            if (_viewer == null && Camera.main) _viewer = Camera.main.transform;
            float cd = cullDistance * RenderDistance.LightMultiplier;
            bool near = _viewer == null || (transform.position - _viewer.position).sqrMagnitude < cd * cd;
            foreach (var l in lights) if (l) l.enabled = near && _state > 0.02f;
            if (buzz) buzz.enabled = near;
            if (!flicker || !near) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                if (_state < 1f) { _state = 1f; _timer = Random.Range(2f, 12f) / Mathf.Max(0.05f, flickerChance); }
                else { _state = Random.value < 0.5f ? 0f : Random.Range(0.1f, 0.5f); _timer = Random.Range(0.03f, 0.25f); }
                SetLevel(_state);
            }
        }

        void SetLevel(float k)
        {
            for (int i = 0; i < lights.Length; i++)
                if (lights[i]) lights[i].intensity = _baseIntensity[i] * k;
            for (int i = 0; i < emissiveRenderers.Length; i++)
            {
                var r = emissiveRenderers[i];
                if (!r) continue;
                r.GetPropertyBlock(_mpb, emissiveMaterialIndex);
                _mpb.SetColor(EmissiveId, _baseEmissive[i] * Mathf.Max(0.01f, k));
                r.SetPropertyBlock(_mpb, emissiveMaterialIndex);
            }
            if (buzz) buzz.volume = 0.35f * k;
        }
    }
}
