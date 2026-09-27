using SyncRooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.World
{
    /// <summary>
    /// Level 94 day/night: the sun travels, the sky and exposure dim, street lamps turn on, the siren poles stop
    /// their cheerful music and a siren wails at dusk. At night entities roam outside — hide in the houses.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Current { get; private set; }
        public Light sun;
        public Volume volume;
        public float dayLength = 360f, nightLength = 180f, transition = 25f;
        [Range(0, 1)] public float startTime = 0.15f;
        public Light[] lamps;
        public GameObject[] musicSources;
        public Color dayTop = new(0.22f, 0.42f, 0.85f), dayMid = new(0.62f, 0.76f, 0.95f), dayBottom = new(0.55f, 0.55f, 0.5f);
        public Color nightTop = new(0.01f, 0.012f, 0.03f), nightMid = new(0.02f, 0.025f, 0.05f), nightBottom = new(0.01f, 0.01f, 0.015f);

        public bool IsNight => _t >= dayLength;
        public float Night01 { get; private set; }
        float _t;
        bool _wasNight;
        GradientSky _sky;
        Exposure _exp;
        float _sunLux, _dayExpMin, _dayExpMax;

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }

        void Start()
        {
            _t = startTime * dayLength;
            if (volume && volume.profile)
            {
                volume.profile.TryGet(out _sky);
                volume.profile.TryGet(out _exp);
                // work on a runtime copy so the asset isn't modified
                volume.profile = Instantiate(volume.profile);
                volume.profile.TryGet(out _sky);
                volume.profile.TryGet(out _exp);
                if (_exp) { _dayExpMin = _exp.limitMin.value; _dayExpMax = _exp.limitMax.value; }
            }
            _sunLux = sun ? sun.intensity : 90000f;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t > dayLength + nightLength) _t = 0;
            // night factor with smooth transitions
            float n;
            if (_t < dayLength - transition) n = _t < transition ? 1f - _t / transition : 0f;
            else if (_t < dayLength) n = (_t - (dayLength - transition)) / transition;
            else n = 1f;
            n = Mathf.SmoothStep(0, 1, n);
            Night01 = n;

            if (sun)
            {
                float dayK = Mathf.Clamp01(_t / dayLength);
                float elev = Mathf.Lerp(12f, 168f, dayK);
                sun.transform.rotation = Quaternion.Euler(IsNight ? 35f : elev, -35f + dayK * 60f, 0);
                sun.intensity = Mathf.Lerp(_sunLux, 0.35f, n);        // at night the "sun" becomes a faint moon
                sun.colorTemperature = Mathf.Lerp(5600f, 8500f, n);
            }
            if (_sky)
            {
                _sky.top.value = Color.Lerp(dayTop, nightTop, n);
                _sky.middle.value = Color.Lerp(dayMid, nightMid, n);
                _sky.bottom.value = Color.Lerp(dayBottom, nightBottom, n);
                _sky.exposure.value = Mathf.Lerp(16.5f, 3.5f, n);
            }
            if (_exp)
            {
                _exp.limitMin.value = Mathf.Lerp(_dayExpMin, 4f, n);
                _exp.limitMax.value = Mathf.Lerp(_dayExpMax, 7.5f, n);
            }
            if (lamps != null) foreach (var l in lamps) if (l) l.enabled = n > 0.5f;

            if (IsNight != _wasNight)
            {
                _wasNight = IsNight;
                if (musicSources != null) foreach (var m in musicSources) if (m) m.SetActive(!IsNight);
                var gm = GameManager.Instance;
                if (IsNight)
                {
                    Sfx.Play2D("SFX/sfx_siren", 0.7f, 0);
                    gm?.UI.Hud.Notify("La musique s'est arrêtée. Rentrez dans une maison !");
                }
                else gm?.UI.Hud.Notify("Le jour se lève. La musique reprend.");
            }
        }
    }
}
