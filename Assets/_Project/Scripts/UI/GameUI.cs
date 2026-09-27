using System.Collections;
using SyncRooms.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SyncRooms.UI
{
    /// <summary>Persistent UI root: HUD, inventory, pause/settings, note reader and screen fader.</summary>
    public class GameUI : MonoBehaviour
    {
        public Hud Hud { get; private set; }
        public InventoryUI Inventory { get; private set; }
        public PauseMenu Pause { get; private set; }

        CanvasGroup _gameplay;
        Image _fader;
        CanvasGroup _noteGroup;
        Text _noteTitle, _noteBody;
        bool _noteOpen;

        public bool AnyPanelOpen => Inventory.IsOpen || _noteOpen;

        public static GameUI Create(Transform parent)
        {
            var go = new GameObject("[UI]");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.transform.SetParent(parent, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var ui = go.AddComponent<GameUI>();
            ui.Build((RectTransform)go.transform);
            return ui;
        }

        void Build(RectTransform root)
        {
            var gameplay = UIFactory.Rect("Gameplay", root).Stretch();
            _gameplay = gameplay.gameObject.AddComponent<CanvasGroup>();
            Hud = Hud.Create(gameplay);
            Inventory = InventoryUI.Create(gameplay);
            BuildNote(gameplay);
            Pause = PauseMenu.Create(root);

            _fader = UIFactory.Image("Fader", root, Color.black);
            _fader.rectTransform.Stretch();
            _fader.raycastTarget = false;
            _fader.color = new Color(0, 0, 0, 1);
        }

        void BuildNote(RectTransform parent)
        {
            var rt = UIFactory.Rect("NoteReader", parent).Stretch();
            _noteGroup = rt.gameObject.AddComponent<CanvasGroup>();
            var dim = UIFactory.Image("Dim", rt, new Color(0, 0, 0, 0.75f));
            dim.rectTransform.Stretch();
            dim.raycastTarget = true;
            var paper = UIFactory.Image("Paper", rt, new Color(0.9f, 0.87f, 0.78f, 1f));
            paper.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 900));
            paper.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.2f);
            var inkC = new Color(0.12f, 0.1f, 0.09f, 0.95f);
            _noteTitle = UIFactory.Text("Title", paper.transform, "", 32, inkC, TextAnchor.UpperLeft, UIFactory.Typewriter);
            _noteTitle.rectTransform.Stretch(60, 0, 60, 50);
            _noteBody = UIFactory.Text("Body", paper.transform, "", 23, inkC, TextAnchor.UpperLeft, UIFactory.Typewriter);
            _noteBody.rectTransform.Stretch(60, 60, 60, 120);
            _noteBody.lineSpacing = 1.15f;
            var close = UIFactory.Text("Close", rt, "[E] / [ÉCHAP] Fermer", 18, UIFactory.InkDim);
            close.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(600, 30));
            SetNote(false);
        }

        void SetNote(bool open)
        {
            _noteOpen = open;
            _noteGroup.alpha = open ? 1 : 0;
            _noteGroup.blocksRaycasts = open;
        }

        public void ShowNote(string title, string body)
        {
            _noteTitle.text = title;
            _noteBody.text = body;
            if (Inventory.IsOpen) Inventory.SetOpen(false);
            SetNote(true);
            _noteOpenedFrame = Time.frameCount;
            GameManager.Instance.RefreshCursor();
        }

        int _noteOpenedFrame;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.IsLoading || gm.IsPaused || _gameplay.alpha < 0.5f) return;
            if (_noteOpen && Time.frameCount > _noteOpenedFrame && Controls.InteractPressed) { CloseAllPanels(); return; }
            if (Controls.InventoryPressed)
            {
                if (_noteOpen) SetNote(false);
                Inventory.SetOpen(!Inventory.IsOpen);
                gm.RefreshCursor();
            }
        }

        public void CloseAllPanels()
        {
            Inventory.SetOpen(false);
            SetNote(false);
            GameManager.Instance.RefreshCursor();
        }

        public void SetGameplayVisible(bool visible)
        {
            _gameplay.alpha = visible ? 1 : 0;
            if (!visible) CloseAllPanels();
        }

        public void ShowPause(bool show) => Pause.SetOpen(show);
        public void ShowLevelTitle(LevelInfo info) => Hud.ShowTitle(info);

        public IEnumerator Fade(float target, float duration)
        {
            float start = _fader.color.a;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                _fader.color = new Color(0, 0, 0, Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            _fader.color = new Color(0, 0, 0, target);
        }
    }
}
