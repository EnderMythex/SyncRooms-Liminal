using System.Collections;
using System.IO;
using UnityEngine;

namespace SyncRooms.Core
{
    /// <summary>
    /// Automated visual QA: launch the build with "-qa" to visit every level, take screenshots
    /// from the spawn (4 directions) and quit. Screenshots go to &lt;build&gt;/QA/.
    /// </summary>
    public class QaCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!System.Environment.CommandLine.Contains("-qa")) return;
            var go = new GameObject("[QA]");
            DontDestroyOnLoad(go);
            go.AddComponent<QaCapture>();
        }

        IEnumerator Start()
        {
            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "QA");
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "MainMenu.png"));
            yield return new WaitForSeconds(1f);
            foreach (var l in LevelCatalog.Levels)
            {
                GameManager.Instance.LoadLevel(l.scene);
                yield return new WaitForSeconds(2f);
                while (GameManager.Instance.IsLoading) yield return null;
                yield return new WaitForSeconds(6f);
                var player = FindAnyObjectByType<Player.FirstPersonController>();
                for (int i = 0; i < 4; i++)
                {
                    if (player) player.Teleport(player.transform.position, i * 90f + player.transform.eulerAngles.y * 0);
                    yield return new WaitForSeconds(2.5f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"{l.scene}_{i}.png"));
                    yield return new WaitForSeconds(0.5f);
                }
                Debug.Log($"[QA] {l.scene} done, fps≈{1f / Time.smoothDeltaTime:0}, monsters={FindObjectsByType<Monsters.Monster>(FindObjectsSortMode.None).Length}");
                var st = UnityEngine.Rendering.VolumeManager.instance.stack;
                var ve = st.GetComponent<UnityEngine.Rendering.HighDefinition.VisualEnvironment>();
                var gs = st.GetComponent<UnityEngine.Rendering.HighDefinition.GradientSky>();
                var ex = st.GetComponent<UnityEngine.Rendering.HighDefinition.Exposure>();
                Debug.Log($"[QA-SKY] {l.scene} skyType={ve.skyType.value} gradExp={gs.exposure.value} (override {gs.exposure.overrideState}) mode={gs.skyIntensityMode.value} intensity={gs.GetIntensityFromSettings()} expLimits={ex.limitMin.value}..{ex.limitMax.value} comp={ex.compensation.value}");
                if (l.scene == "Level4" && player)
                {
                    // line up every entity in front of the camera for a visual check
                    var cam = Camera.main.transform;
                    int k = 0;
                    foreach (var name in new[] { "Hound", "Deathmoth", "Smiler", "Toon", "Slime", "SlimeBig" })
                    {
                        var prefab = Gen.Registry.Prefab(name);
                        if (!prefab) { Debug.Log("[QA] missing monster prefab " + name); continue; }
                        var pos = cam.position + cam.forward * 4.5f + cam.right * ((k - 2.5f) * 1.6f);
                        pos.y = player.transform.position.y + 0.05f;
                        var m = Instantiate(prefab, pos, Quaternion.LookRotation(-cam.forward));
                        foreach (var mm in m.GetComponentsInChildren<Monsters.Monster>()) mm.enabled = false;
                        k++;
                    }
                    GameManager.Instance.State.flashlightOn = false;
                    yield return new WaitForSeconds(1.5f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, "Monsters.png"));
                    yield return new WaitForSeconds(0.5f);
                }
            }
            Application.Quit();
        }
    }
}
