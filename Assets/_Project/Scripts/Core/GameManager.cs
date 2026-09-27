using System;
using System.Collections;
using SyncRooms.Inventory;
using SyncRooms.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SyncRooms.Core
{
    [Serializable]
    public class GameSettings
    {
        public float mouseSensitivity = 2f;
        public float fov = 70f;
        public float masterVolume = 0.9f;
        public bool invertY;
        public bool headBob = true;
        public int quality = 2; // 0 low, 1 medium, 2 high, 3 ultra (ray tracing when supported)
        public int viewDistance = 0; // 0 normal, 1 far, 2 unlimited
    }

    [Serializable]
    public class PlayerState
    {
        public float sanity = 100f;
        public float battery = 100f;
        public bool flashlightOn;
    }

    /// <summary>
    /// Persistent root: owns the inventory, player state, settings, UI and level transitions.
    /// Created automatically before the first scene loads.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerInventory Inventory { get; private set; }
        public PlayerState State { get; private set; } = new();
        public GameSettings Settings { get; private set; } = new();
        public GameUI UI { get; private set; }

        public bool IsLoading { get; private set; }
        public bool IsPaused { get; private set; }
        public event Action SettingsChanged;

        const string SaveKey = "syncrooms.save";
        const string SettingsKey = "syncrooms.settings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            Instance = this;
            Inventory = gameObject.AddComponent<PlayerInventory>();
            LoadSettings();
            UI = GameUI.Create(transform);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool inMenu = scene.name == LevelCatalog.MainMenu;
            UI.SetGameplayVisible(!inMenu);
            SetPaused(false);
            var info = LevelCatalog.Find(scene.name);
            if (info.HasValue) UI.ShowLevelTitle(info.Value);
            ApplySettings();
            Player.PlayerTrail.Clear();
            InCutscene = false;
            if (!IsLoading) StartCoroutine(UI.Fade(0f, 1.5f)); // scene opened directly (editor play)
            var ls = World.LevelSettings.Current;
            if (ls != null && ls.arrival != Player.ArrivalKind.None && scene.name != LevelCatalog.MainMenu)
                StartCoroutine(Player.PlayerCutscene.Arrive(ls.arrival, ls.spawnPoint));
        }

        void Update()
        {
            if (IsLoading) return;
            if (SceneManager.GetActiveScene().name == LevelCatalog.MainMenu) return;
            if (Controls.PausePressed)
            {
                if (UI.AnyPanelOpen) UI.CloseAllPanels();
                else SetPaused(!IsPaused);
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            UI.ShowPause(paused);
            RefreshCursor();
        }

        /// <summary>Locks the cursor while playing, frees it for menus.</summary>
        public void RefreshCursor()
        {
            bool free = IsPaused || UI.AnyPanelOpen || SceneManager.GetActiveScene().name == LevelCatalog.MainMenu;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        public bool InCutscene { get; set; }
        public bool InputBlocked => IsPaused || IsLoading || InCutscene || UI.AnyPanelOpen;

        /// <summary>Monster hit: sanity damage + red flash. At 0 the player blacks out (PlayerVitals).</summary>
        public void Hurt(float amount)
        {
            State.sanity = Mathf.Max(0f, State.sanity - amount);
            UI.Hud.Flash(new Color(0.6f, 0f, 0f, 0.55f));
            Sfx.Play2D("SFX/sfx_hurt", 0.9f, 0.1f);
        }

        // ---------------------------------------------------------------- Levels
        public void NewGame()
        {
            Inventory.Clear();
            State = new PlayerState();
            LoadLevel(LevelCatalog.Levels[0].scene);
        }

        /// <summary>Plays the exit animation, then loads the next level.</summary>
        public void ExitVia(Player.ExitKind kind, Transform anchor, string scene, string sfx)
        {
            if (IsLoading) return;
            StartCoroutine(ExitRoutine(kind, anchor, scene, sfx));
        }

        IEnumerator ExitRoutine(Player.ExitKind kind, Transform anchor, string scene, string sfx)
        {
            UI.CloseAllPanels();
            yield return Player.PlayerCutscene.Exit(kind, anchor);
            LoadLevel(scene, kind == Player.ExitKind.None ? sfx : null);
        }

        public void LoadLevel(string scene, string transitionSfx = null)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(scene, transitionSfx));
        }

        IEnumerator LoadRoutine(string scene, string sfx)
        {
            IsLoading = true;
            if (!string.IsNullOrEmpty(sfx)) Sfx.Play2D(sfx);
            yield return UI.Fade(1f, 1.2f);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null;
            if (LevelCatalog.Find(scene).HasValue) Save(scene);
            IsLoading = false;
            yield return UI.Fade(0f, 1.5f);
        }

        public void EndGame()
        {
            if (IsLoading) return;
            StartCoroutine(EndRoutine());
        }

        IEnumerator EndRoutine()
        {
            IsLoading = true;
            yield return UI.Fade(1f, 3f);
            UI.ShowLevelTitle(new LevelInfo("", "FIN", "Vous avez trouvé le calme... pour l'instant.",
                "Merci d'avoir joué à SyncRooms : Liminal — Levels 4, 5, 94 et 37"));
            PlayerPrefs.DeleteKey(SaveKey);
            yield return new WaitForSecondsRealtime(7f);
            IsLoading = false;
            LoadLevel(LevelCatalog.MainMenu);
        }

        public void QuitToMenu()
        {
            SetPaused(false);
            LoadLevel(LevelCatalog.MainMenu);
        }

        // ---------------------------------------------------------------- Save
        [Serializable] class SaveData { public string level; public string inventory; public PlayerState state; }

        public bool HasSave => PlayerPrefs.HasKey(SaveKey);

        public void Save(string level)
        {
            var d = new SaveData { level = level, inventory = Inventory.ToJson(), state = State };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        public void Continue()
        {
            if (!HasSave) { NewGame(); return; }
            var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            Inventory.FromJson(d.inventory);
            State = d.state ?? new PlayerState();
            LoadLevel(string.IsNullOrEmpty(d.level) ? LevelCatalog.Levels[0].scene : d.level);
        }

        // ---------------------------------------------------------------- Settings
        void LoadSettings()
        {
            if (PlayerPrefs.HasKey(SettingsKey))
                Settings = JsonUtility.FromJson<GameSettings>(PlayerPrefs.GetString(SettingsKey)) ?? new GameSettings();
            // Ultra (ray tracing) is opt-in from the settings menu
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(Settings));
            PlayerPrefs.Save();
            ApplySettings();
        }

        void ApplySettings()
        {
            AudioListener.volume = Settings.masterVolume;
            int q = Mathf.Clamp(Settings.quality, 0, QualitySettings.names.Length - 1);
            if (QualitySettings.GetQualityLevel() != q) QualitySettings.SetQualityLevel(q, true);
            UltraGraphics.Apply(q);
            SettingsChanged?.Invoke();
        }
    }
}
