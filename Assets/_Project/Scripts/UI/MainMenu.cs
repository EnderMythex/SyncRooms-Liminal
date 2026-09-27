using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.UI
{
    /// <summary>Main menu overlay (lives in the MainMenu scene, over a slowly drifting 3D backdrop).</summary>
    public class MainMenu : MonoBehaviour
    {
        public Transform cameraRig;
        public float driftSpeed = 0.35f;

        RectTransform _main, _levels, _settings, _header;
        Vector3 _rigStart;

        void Start()
        {
            var gm = GameManager.Instance;
            gm.RefreshCursor();
            if (cameraRig) _rigStart = cameraRig.position;

            var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            var shade = UIFactory.Image("Shade", root, new Color(0, 0, 0, 0.35f));
            shade.rectTransform.Stretch();

            _header = UIFactory.Rect("Header", root).Stretch();
            var title = UIFactory.Text("Title", _header, "SYNCROOMS", 96, UIFactory.Ink, TextAnchor.UpperLeft, UIFactory.Typewriter);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -120), new Vector2(1200, 120));
            var sub = UIFactory.Text("Sub", _header, "L I M I N A L", 30, UIFactory.Accent, TextAnchor.UpperLeft, UIFactory.MonoBold);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(146, -236), new Vector2(1200, 40));
            var tag = UIFactory.Text("Tag", _header, "« Si vous ne faites pas attention et que vous sortez de la réalité aux mauvais endroits... »", 18, UIFactory.InkDim, TextAnchor.UpperLeft);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(146, -286), new Vector2(1200, 30));

            _main = UIFactory.Rect("Main", root).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(122, 120), new Vector2(520, 380));
            UIFactory.VLayout(_main.gameObject, 4).childAlignment = TextAnchor.LowerLeft;
            if (gm.HasSave) UIFactory.Button("Continue", _main, "Continuer", gm.Continue, 30).Height(58);
            UIFactory.Button("New", _main, "Nouvelle partie", gm.NewGame, 30).Height(58);
            UIFactory.Button("Hub", _main, "The Hub", () => gm.LoadLevel(LevelCatalog.Hub), 30).Height(58);
            UIFactory.Button("Levels", _main, "Choisir un niveau", () => Show(_levels), 30).Height(58);
            UIFactory.Button("Settings", _main, "Paramètres", () => Show(_settings), 30).Height(58);
            UIFactory.Button("Quit", _main, "Quitter", Quit, 30).Height(58);

            _levels = UIFactory.Rect("Levels", root).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(122, 90), new Vector2(900, 560));
            UIFactory.VLayout(_levels.gameObject, 4).childAlignment = TextAnchor.LowerLeft;
            foreach (var l in LevelCatalog.Levels)
            {
                var info = l;
                UIFactory.Button(l.scene, _levels, $"{l.number}  <color=#E8C76B>« {l.title} »</color>", () => gm.LoadLevel(info.scene), 26).Height(52);
            }
            UIFactory.Button("Back", _levels, "‹ Retour", () => Show(_main), 26).Height(52);

            _settings = PauseMenu.BuildSettings(root, () => Show(_main));
            var credit = UIFactory.Text("Credits", root, "Textures & modèles : Poly Haven, ambientCG (CC0) · Sons : Freesound · Inspiré du Backrooms Wiki (CC BY-SA 3.0)", 14, UIFactory.InkDim, TextAnchor.LowerRight);
            credit.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 20), new Vector2(1400, 24));
            Show(_main);
            StartCoroutine(gm.UI.Fade(0f, 2f));
        }

        void Show(RectTransform panel)
        {
            _main.gameObject.SetActive(panel == _main);
            _levels.gameObject.SetActive(panel == _levels);
            _settings.gameObject.SetActive(panel == _settings);
            _header.gameObject.SetActive(panel == _main);
        }

        void Update()
        {
            if (!cameraRig) return;
            float t = Time.time * driftSpeed;
            cameraRig.position = _rigStart + new Vector3(Mathf.Sin(t * 0.13f) * 1.2f + 1.2f, Mathf.Sin(t * 0.7f) * 0.03f, (1f - Mathf.Cos(t * 0.08f)) * 2.5f);
            cameraRig.rotation = Quaternion.Euler(Mathf.Sin(t * 0.21f) * 2f, 35f + Mathf.Sin(t * 0.09f) * 20f, 0);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
