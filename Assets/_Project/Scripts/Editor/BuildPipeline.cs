using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SyncRooms.EditorTools
{
    /// <summary>Batch entry points (unity -executeMethod ...).</summary>
    public static class BatchEntry
    {
        public static void Assets()
        {
            ProjectSetup.Run();
            MaterialLibrary.BuildAll();
            ModelLibrary.BuildAll();
            ItemLibrary.BuildAll();
            RuntimeLibrary.BuildAll();
        }

        public static void Levels()
        {
            LevelBuilderMenu.BuildAllLevels();
        }

        public static void All()
        {
            Assets();
            LevelBuilderMenu.DebugAxes();
            Levels();
        }

        public static void LevelsAndShots()
        {
            Levels();
            AllShots();
        }

        public static void AllShots()
        {
            foreach (var s in new[] { "MainMenu", "Level4", "Level5", "Level94", "Level37" })
            {
                System.Environment.SetEnvironmentVariable("SR_SCENE", s);
                try { LevelBuilderMenu.Screenshots(); } catch (System.Exception e) { Debug.LogError($"[SyncRooms] shots {s}: {e}"); }
            }
        }

        public static void LevelsAndBuild()
        {
            Levels();
            BuildPlayer();
        }

        public static void BuildPlayer()
        {
            var scenes = LevelBuilderMenu.SceneList();
            Directory.CreateDirectory("Builds/Windows");
            var report = UnityEditor.BuildPipeline.BuildPlayer(scenes, "Builds/Windows/SyncRoomsLiminal.exe", BuildTarget.StandaloneWindows64, BuildOptions.CleanBuildCache);
            Debug.Log($"[SyncRooms] Build result: {report.summary.result}, size {report.summary.totalSize / (1024 * 1024)} MB, errors {report.summary.totalErrors}");
        }
    }
}
