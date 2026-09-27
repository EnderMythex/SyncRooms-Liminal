using SyncRooms.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SyncRooms.UI
{
    /// <summary>Pause menu + settings panel. The settings panel is also reused by the main menu.</summary>
    public class PauseMenu : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _main, _settings;

        public static PauseMenu Create(Transform parent)
        {
            var rt = UIFactory.Rect("Pause", parent).Stretch();
            var pm = rt.gameObject.AddComponent<PauseMenu>();
            pm.Build(rt);
            return pm;
        }

        void Build(RectTransform root)
        {
            _group = root.gameObject.AddComponent<CanvasGroup>();
            var dim = UIFactory.Image("Dim", root, new Color(0, 0, 0, 0.8f));
            dim.rectTransform.Stretch();
            dim.raycastTarget = true;

            _main = UIFactory.Rect("Main", root).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(520, 420));
            var title = UIFactory.Text("Title", _main, "PAUSE", 48, UIFactory.Ink, TextAnchor.UpperLeft, UIFactory.Typewriter);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(520, 70));
            var list = UIFactory.Rect("List", _main).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(-18, -100), new Vector2(520, 300));
            UIFactory.VLayout(list.gameObject, 4);
            UIFactory.Button("Resume", list, "Reprendre", () => GameManager.Instance.SetPaused(false)).Height(52);
            UIFactory.Button("Settings", list, "Paramètres", () => ShowSettings(true)).Height(52);
            UIFactory.Button("Menu", list, "Menu principal", () => GameManager.Instance.QuitToMenu()).Height(52);
            UIFactory.Button("Quit", list, "Quitter le jeu", Quit).Height(52);

            _settings = BuildSettings(root, () => ShowSettings(false));
            SetOpen(false);
        }

        public static RectTransform BuildSettings(RectTransform parent, System.Action onBack)
        {
            var gm = GameManager.Instance;
            var s = gm.Settings;
            var panel = UIFactory.Rect("Settings", parent).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(820, 680));
            var title = UIFactory.Text("Title", panel, "PARAMÈTRES", 40, UIFactory.Ink, TextAnchor.UpperLeft, UIFactory.Typewriter);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(820, 60));
            var list = UIFactory.Rect("List", panel).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -80), new Vector2(820, 580));
            UIFactory.VLayout(list.gameObject, 10);

            UIFactory.Slider("Sensitivity", list, "Sensibilité souris", 0.2f, 6f, s.mouseSensitivity, v => { s.mouseSensitivity = v; gm.SaveSettings(); }, v => v.ToString("0.0")).Height(44);
            UIFactory.Slider("FOV", list, "Champ de vision", 55f, 95f, s.fov, v => { s.fov = Mathf.Round(v); gm.SaveSettings(); }, v => Mathf.Round(v) + "°").Height(44);
            UIFactory.Slider("Volume", list, "Volume général", 0f, 1f, s.masterVolume, v => { s.masterVolume = v; gm.SaveSettings(); }, v => Mathf.RoundToInt(v * 100) + "%").Height(44);
            string[] q = { "Basse", "Moyenne", "Haute", "Ultra (ray tracing)" };
            UIFactory.Toggle("Quality", list, "Qualité graphique", () => q[Mathf.Clamp(s.quality, 0, 3)], () => { s.quality = (s.quality + 1) % 4; gm.SaveSettings(); }).Height(44);
            string[] vd = { "Normale", "Loin", "Illimitée" };
            UIFactory.Toggle("ViewDistance", list, "Distance d'affichage", () => vd[Mathf.Clamp(s.viewDistance, 0, 2)], () => { s.viewDistance = (s.viewDistance + 1) % 3; gm.SaveSettings(); }).Height(44);
            UIFactory.Toggle("Bob", list, "Balancement de la tête", () => s.headBob ? "Oui" : "Non", () => { s.headBob = !s.headBob; gm.SaveSettings(); }).Height(44);
            UIFactory.Toggle("Invert", list, "Inverser l'axe Y", () => s.invertY ? "Oui" : "Non", () => { s.invertY = !s.invertY; gm.SaveSettings(); }).Height(44);
            UIFactory.Button("Back", list, "‹ Retour", () => onBack()).Height(52);
            return panel;
        }

        void ShowSettings(bool show)
        {
            _main.gameObject.SetActive(!show);
            _settings.gameObject.SetActive(show);
        }

        public void SetOpen(bool open)
        {
            _group.alpha = open ? 1 : 0;
            _group.blocksRaycasts = open;
            _group.interactable = open;
            ShowSettings(false);
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
