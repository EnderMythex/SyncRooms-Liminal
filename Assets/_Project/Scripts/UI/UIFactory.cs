using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SyncRooms.UI
{
    /// <summary>Code-built UGUI helpers with the game's visual style.</summary>
    public static class UIFactory
    {
        public static readonly Color Ink = new(0.93f, 0.91f, 0.84f, 1f);
        public static readonly Color InkDim = new(0.93f, 0.91f, 0.84f, 0.55f);
        public static readonly Color Accent = new(0.91f, 0.78f, 0.42f, 1f);
        public static readonly Color PanelBg = new(0.05f, 0.05f, 0.045f, 0.86f);
        public static readonly Color SlotBg = new(1f, 1f, 1f, 0.06f);
        public static readonly Color SlotHover = new(0.91f, 0.78f, 0.42f, 0.22f);

        static Font _mono, _monoBold, _type;
        public static Font Mono => _mono ??= Resources.Load<Font>("Fonts/IBMPlexMono-Regular");
        public static Font MonoBold => _monoBold ??= Resources.Load<Font>("Fonts/IBMPlexMono-SemiBold");
        public static Font Typewriter => _type ??= Resources.Load<Font>("Fonts/SpecialElite-Regular");

        static Sprite _white;
        public static Sprite White
        {
            get
            {
                if (_white) return _white;
                var t = Texture2D.whiteTexture;
                _white = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                return _white;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ? sprite : White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(string name, Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, Font font = null)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font ? font : Mono;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, UnityAction onClick, int fontSize = 26)
        {
            var bg = Image(name, parent, new Color(1, 1, 1, 0.0f));
            bg.raycastTarget = true;
            var btn = bg.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(1, 1, 1, 0.0f);
            colors.highlightedColor = new Color(0.91f, 0.78f, 0.42f, 0.18f);
            colors.pressedColor = new Color(0.91f, 0.78f, 0.42f, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var txt = Text("Label", bg.transform, label, fontSize, Ink, TextAnchor.MiddleLeft, MonoBold);
            txt.rectTransform.Stretch(18, 0, 10, 0);
            btn.onClick.AddListener(() => Core.Sfx.Play2D("SFX/sfx_ui_click", 0.4f));
            btn.onClick.AddListener(onClick);
            return btn;
        }

        public static Slider Slider(string name, Transform parent, string label, float min, float max, float value,
            Action<float> onChange, Func<float, string> format)
        {
            var row = Rect(name, parent);
            var lbl = Text("Label", row, label, 22, Ink, TextAnchor.MiddleLeft);
            lbl.rectTransform.anchorMin = new Vector2(0, 0);
            lbl.rectTransform.anchorMax = new Vector2(0.45f, 1);
            lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
            var val = Text("Value", row, format(value), 22, Accent, TextAnchor.MiddleRight);
            val.rectTransform.anchorMin = new Vector2(0.85f, 0);
            val.rectTransform.anchorMax = new Vector2(1, 1);
            val.rectTransform.offsetMin = val.rectTransform.offsetMax = Vector2.zero;

            var root = Rect("Slider", row);
            root.anchorMin = new Vector2(0.47f, 0.5f);
            root.anchorMax = new Vector2(0.82f, 0.5f);
            root.sizeDelta = new Vector2(0, 20);
            var track = Image("Track", root, new Color(1, 1, 1, 0.15f));
            track.rectTransform.Stretch(0, 8, 0, 8);
            var fillArea = Rect("Fill Area", root).Stretch(0, 8, 0, 8);
            var fill = Image("Fill", fillArea, Accent);
            fill.rectTransform.Stretch();
            var handleArea = Rect("Handle Area", root).Stretch();
            var handle = Image("Handle", handleArea, Ink);
            handle.rectTransform.sizeDelta = new Vector2(14, 0);
            handle.raycastTarget = true;

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = min;
            s.maxValue = max;
            s.value = value;
            s.onValueChanged.AddListener(v => { val.text = format(v); onChange(v); });
            return s;
        }

        public static Button Toggle(string name, Transform parent, string label, Func<string> valueText, Action onClick)
        {
            var b = Button(name, parent, "", () => { }, 22);
            var lbl = b.GetComponentInChildren<Text>();
            lbl.font = Mono;
            void Refresh() => lbl.text = $"{label}<color=#E8C76B>  {valueText()}</color>";
            b.onClick.AddListener(() => { onClick(); Refresh(); });
            Refresh();
            return b;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing, RectOffset pad = null)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = pad ?? new RectOffset();
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return v;
        }

        public static LayoutElement Height(this Component c, float h)
        {
            // apply to the ancestor that is the direct child of a layout group (e.g. a slider's row)
            var target = c.transform;
            while (target.parent != null && target.parent.GetComponent<LayoutGroup>() == null) target = target.parent;
            if (target.parent == null) target = c.transform;
            if (!target.TryGetComponent(out LayoutElement le)) le = target.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            return le;
        }
    }
}
