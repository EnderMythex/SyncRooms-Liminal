using SyncRooms.Core;
using SyncRooms.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.Player
{
    /// <summary>Sanity simulation + its audiovisual feedback (vignette, grain, aberration, heartbeat).</summary>
    public class PlayerVitals : MonoBehaviour
    {
        public Volume sanityVolume;       // local post-process volume, weight driven by insanity
        public AudioSource heartbeat;
        public AudioSource underwater;

        FirstPersonController _fpc;
        float _whisperTimer = 20f;
        bool _blackingOut;

        void Awake() => _fpc = GetComponent<FirstPersonController>();

        void Start()
        {
            if (sanityVolume && sanityVolume.profile == null) sanityVolume.profile = BuildProfile();
        }

        static VolumeProfile BuildProfile()
        {
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var v = p.Add<Vignette>(true);
            v.intensity.Override(0.55f);
            v.smoothness.Override(0.6f);
            v.color.Override(new Color(0.05f, 0f, 0f));
            var ca = p.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.8f);
            var fg = p.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Medium3);
            fg.intensity.Override(0.7f);
            var ld = p.Add<LensDistortion>(true);
            ld.intensity.Override(-0.25f);
            var ca2 = p.Add<ColorAdjustments>(true);
            ca2.saturation.Override(-45f);
            return p;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.IsPaused) return;
            var s = gm.State;
            var lvl = LevelSettings.Current;

            if (lvl != null && !_blackingOut)
            {
                float drain = lvl.sanityDrainPerMinute;
                if (!s.flashlightOn) drain += lvl.darknessDrainPerMinute;
                s.sanity = Mathf.Clamp(s.sanity - drain / 60f * Time.deltaTime, 0f, 100f);
            }

            float insanity = Mathf.InverseLerp(55f, 0f, s.sanity); // effects start below 55%
            float pulse = 1f + Mathf.Sin(Time.time * Mathf.Lerp(2f, 7f, insanity)) * 0.08f * insanity;
            if (sanityVolume) sanityVolume.weight = Mathf.Clamp01(insanity * pulse);

            if (heartbeat)
            {
                heartbeat.volume = Mathf.Lerp(heartbeat.volume, insanity * 0.8f, Time.deltaTime * 2f);
                heartbeat.pitch = Mathf.Lerp(0.9f, 1.35f, insanity);
                if (heartbeat.volume > 0.01f && !heartbeat.isPlaying) heartbeat.Play();
                else if (heartbeat.volume <= 0.01f && heartbeat.isPlaying) heartbeat.Stop();
            }

            if (underwater)
            {
                bool under = _fpc && _fpc.HeadUnderwater;
                if (under && !underwater.isPlaying) underwater.Play();
                if (!under && underwater.isPlaying) underwater.Stop();
            }

            // Whispers when sanity is low
            if (insanity > 0.3f)
            {
                _whisperTimer -= Time.deltaTime * insanity;
                if (_whisperTimer <= 0f)
                {
                    _whisperTimer = Random.Range(12f, 30f);
                    Vector3 p = transform.position + Random.onUnitSphere.normalized * 3f;
                    p.y = transform.position.y + 1.6f;
                    Sfx.PlayAt("Ambience/amb_whispers", p, 0.55f * insanity, 10f);
                }
            }

            if (s.sanity <= 0f && !_blackingOut) StartCoroutine(Blackout());
        }

        System.Collections.IEnumerator Blackout()
        {
            _blackingOut = true;
            var gm = GameManager.Instance;
            gm.UI.Hud.Notify("Vous perdez connaissance...");
            yield return gm.UI.Fade(1f, 2f);
            var lvl = LevelSettings.Current;
            if (lvl && lvl.spawnPoint) _fpc.Teleport(lvl.spawnPoint.position, lvl.spawnPoint.eulerAngles.y);
            gm.State.sanity = 45f;
            yield return new WaitForSeconds(1f);
            yield return gm.UI.Fade(0f, 2f);
            gm.UI.Hud.Notify("Vous vous réveillez, désorienté.");
            _blackingOut = false;
        }
    }
}
