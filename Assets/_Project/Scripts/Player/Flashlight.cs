using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.Player
{
    /// <summary>Handheld flashlight: needs the "flashlight" item, drains battery, flickers when low.</summary>
    public class Flashlight : MonoBehaviour
    {
        public Light spot;
        public float intensityLumen = 900f;
        public float drainPerMinute = 6f;
        public const string ItemId = "flashlight";

        float _flickerTimer;

        void Start() => Apply();

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var state = gm.State;

            if (!gm.InputBlocked && Controls.FlashlightPressed) Toggle();

            if (state.flashlightOn && !gm.Inventory.Has(ItemId)) state.flashlightOn = false;
            if (state.flashlightOn)
            {
                state.battery = Mathf.Max(0f, state.battery - drainPerMinute / 60f * Time.deltaTime);
                if (state.battery <= 0f)
                {
                    state.flashlightOn = false;
                    gm.UI.Hud.Notify("Batterie vide");
                }
            }
            Apply();
        }

        public void Toggle()
        {
            var gm = GameManager.Instance;
            if (!gm.Inventory.Has(ItemId)) { gm.UI.Hud.Notify("Vous n'avez pas de lampe torche"); return; }
            if (!gm.State.flashlightOn && gm.State.battery <= 0f) { gm.UI.Hud.Notify("Batterie vide — trouvez une pile"); Sfx.Play2D("SFX/sfx_flashlight", 0.6f); return; }
            gm.State.flashlightOn = !gm.State.flashlightOn;
            Sfx.Play2D("SFX/sfx_flashlight", 0.7f);
        }

        void Apply()
        {
            var gm = GameManager.Instance;
            bool on = gm != null && gm.State.flashlightOn;
            spot.enabled = on;
            if (!on) return;
            float b = gm.State.battery;
            float k = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(0f, 40f, b));
            if (b < 15f)
            {
                _flickerTimer -= Time.deltaTime;
                if (_flickerTimer <= 0f) _flickerTimer = Random.Range(0.03f, 0.4f);
                if (_flickerTimer < 0.08f) k *= Random.Range(0.05f, 0.5f);
            }
            spot.intensity = intensityLumen * k;
        }
    }
}
