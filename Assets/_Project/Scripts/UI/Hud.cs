using System.Collections.Generic;
using SyncRooms.Core;
using SyncRooms.Inventory;
using SyncRooms.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SyncRooms.UI
{
    /// <summary>Crosshair, interaction prompt, notifications, vitals and hotbar.</summary>
    public class Hud : MonoBehaviour
    {
        Text _prompt;
        Image _crosshair;
        RectTransform _notifyRoot;
        readonly List<(Text text, float time)> _notes = new();
        Image _sanityFill, _staminaFill, _batteryFill;
        CanvasGroup _batteryGroup, _vitalsGroup;
        Text _title, _subtitle;
        CanvasGroup _titleGroup;
        float _titleTimer;
        HotbarSlot[] _hotbar;
        Image _flash;

        class HotbarSlot
        {
            public Image bg, icon;
            public Text count;
        }

        public static Hud Create(Transform parent)
        {
            var rt = UIFactory.Rect("HUD", parent).Stretch();
            var hud = rt.gameObject.AddComponent<Hud>();
            hud.Build(rt);
            return hud;
        }

        void Build(RectTransform root)
        {
            _crosshair = UIFactory.Image("Crosshair", root, new Color(1, 1, 1, 0.55f));
            _crosshair.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 4));

            _prompt = UIFactory.Text("Prompt", root, "", 24, UIFactory.Ink);
            _prompt.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(900, 40));
            var sh = _prompt.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(1.5f, -1.5f);

            _notifyRoot = UIFactory.Rect("Notifications", root);
            _notifyRoot.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 170), new Vector2(700, 300));
            UIFactory.VLayout(_notifyRoot.gameObject, 6).childAlignment = TextAnchor.LowerLeft;

            // Vitals (bottom-left)
            var vitals = UIFactory.Rect("Vitals", root).Place(Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(300, 110));
            _vitalsGroup = vitals.gameObject.AddComponent<CanvasGroup>();
            _sanityFill = Bar(vitals, "SANTÉ MENTALE", 0, new Color(0.78f, 0.86f, 0.9f));
            _staminaFill = Bar(vitals, "ENDURANCE", 1, new Color(0.9f, 0.9f, 0.85f));
            var bat = UIFactory.Rect("BatteryRow", vitals).Stretch();
            _batteryGroup = bat.gameObject.AddComponent<CanvasGroup>();
            _batteryFill = Bar(bat, "LAMPE", 2, UIFactory.Accent);

            // Hotbar
            var hb = UIFactory.Rect("Hotbar", root).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(5 * 86, 80));
            _hotbar = new HotbarSlot[PlayerInventory.HotbarSize];
            for (int i = 0; i < _hotbar.Length; i++)
            {
                var s = new HotbarSlot();
                s.bg = UIFactory.Image("Slot" + i, hb, new Color(0, 0, 0, 0.45f));
                s.bg.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 86 + 3, 0), new Vector2(78, 78));
                s.icon = UIFactory.Image("Icon", s.bg.transform, Color.white);
                s.icon.rectTransform.Stretch(8, 8, 8, 8);
                s.icon.preserveAspect = true;
                var key = UIFactory.Text("Key", s.bg.transform, (i + 1).ToString(), 15, UIFactory.InkDim, TextAnchor.UpperLeft);
                key.rectTransform.Stretch(5, 2, 2, 2);
                s.count = UIFactory.Text("Count", s.bg.transform, "", 17, UIFactory.Ink, TextAnchor.LowerRight, UIFactory.MonoBold);
                s.count.rectTransform.Stretch(2, 2, 6, 2);
                _hotbar[i] = s;
            }

            _flash = UIFactory.Image("Flash", root, new Color(0, 0, 0, 0));
            _flash.rectTransform.Stretch();

            // Level title card
            var title = UIFactory.Rect("LevelTitle", root).Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 220));
            _titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.alpha = 0;
            _title = UIFactory.Text("Title", title, "", 64, UIFactory.Ink, TextAnchor.MiddleCenter, UIFactory.Typewriter);
            _title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1400, 130));
            _subtitle = UIFactory.Text("Subtitle", title, "", 20, UIFactory.InkDim);
            _subtitle.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 60));
        }

        static Image Bar(RectTransform parent, string label, int row, Color c)
        {
            float y = 80 - row * 34;
            var l = UIFactory.Text(label, parent, label, 14, UIFactory.InkDim, TextAnchor.LowerLeft);
            l.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(0, y + 8), new Vector2(300, 18));
            var bg = UIFactory.Image(label + "Bg", parent, new Color(1, 1, 1, 0.12f));
            bg.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(0, y), new Vector2(260, 5));
            var fill = UIFactory.Image(label + "Fill", bg.transform, c);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.rectTransform.Stretch();
            return fill;
        }

        public void Flash(Color c) => _flash.color = c;

        public void SetPrompt(string text)
        {
            _prompt.text = text ?? "";
            _crosshair.color = string.IsNullOrEmpty(text) ? new Color(1, 1, 1, 0.45f) : UIFactory.Accent;
            _crosshair.rectTransform.sizeDelta = string.IsNullOrEmpty(text) ? new Vector2(4, 4) : new Vector2(7, 7);
        }

        public void Notify(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            var t = UIFactory.Text("Note", _notifyRoot, message, 20, UIFactory.Ink, TextAnchor.LowerLeft);
            t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
            t.Height(28);
            _notes.Add((t, Time.unscaledTime));
            if (_notes.Count > 6) { Destroy(_notes[0].text.gameObject); _notes.RemoveAt(0); }
        }

        public void ShowTitle(LevelInfo info)
        {
            _title.text = $"{info.number}\n<size=40>« {info.title} »</size>";
            _subtitle.text = info.subtitle;
            _titleTimer = 7f;
        }

        void Update()
        {
            if (_flash.color.a > 0f)
            {
                var fc = _flash.color;
                fc.a = Mathf.MoveTowards(fc.a, 0f, Time.unscaledDeltaTime * 0.8f);
                _flash.color = fc;
            }
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                float age = Time.unscaledTime - _notes[i].time;
                var c = _notes[i].text.color;
                c.a = Mathf.Clamp01(5f - age);
                _notes[i].text.color = c;
                if (age > 5f) { Destroy(_notes[i].text.gameObject); _notes.RemoveAt(i); }
            }

            if (_titleTimer > 0f)
            {
                _titleTimer -= Time.unscaledDeltaTime;
                _titleGroup.alpha = Mathf.Clamp01(Mathf.Min(7f - _titleTimer, _titleTimer) / 1.5f);
            }

            var gm = GameManager.Instance;
            if (gm == null) return;
            _sanityFill.fillAmount = gm.State.sanity / 100f;
            var fpc = FindPlayer();
            _staminaFill.fillAmount = fpc ? fpc.Stamina / fpc.maxStamina : 1f;
            bool hasLight = gm.Inventory.Has(Flashlight.ItemId);
            _batteryGroup.alpha = hasLight ? 1f : 0f;
            _batteryFill.fillAmount = gm.State.battery / 100f;
            // Vitals fade out when everything is fine.
            bool calm = gm.State.sanity > 70f && (!fpc || fpc.Stamina > fpc.maxStamina * 0.98f);
            _vitalsGroup.alpha = Mathf.MoveTowards(_vitalsGroup.alpha, calm ? 0.35f : 1f, Time.unscaledDeltaTime * 2f);

            var slots = gm.Inventory.Slots;
            for (int i = 0; i < _hotbar.Length; i++)
            {
                var s = slots[i];
                _hotbar[i].icon.enabled = !s.IsEmpty && s.item.icon;
                if (!s.IsEmpty) _hotbar[i].icon.sprite = s.item.icon;
                _hotbar[i].count.text = !s.IsEmpty && s.count > 1 ? s.count.ToString() : "";
            }

            if (!gm.InputBlocked)
            {
                int hk = Controls.HotbarPressed;
                if (hk >= 0) gm.Inventory.Select(hk);
                int sc = Controls.Scroll;
                if (sc != 0) gm.Inventory.Select((gm.Inventory.SelectedSlot + sc + PlayerInventory.HotbarSize) % PlayerInventory.HotbarSize);
                if (Controls.UsePressed && !slots[gm.Inventory.SelectedSlot].IsEmpty)
                {
                    Player.PlayerAvatar.Instance?.PlayUse();
                    ItemActions.Use(gm.Inventory.SelectedSlot);
                }
            }
            for (int i = 0; i < _hotbar.Length; i++)
                _hotbar[i].bg.color = i == gm.Inventory.SelectedSlot ? new Color(0.91f, 0.78f, 0.42f, 0.35f) : new Color(0, 0, 0, 0.45f);
        }

        FirstPersonController _fpc;
        FirstPersonController FindPlayer()
        {
            if (_fpc == null) _fpc = FindAnyObjectByType<FirstPersonController>();
            return _fpc;
        }
    }
}
