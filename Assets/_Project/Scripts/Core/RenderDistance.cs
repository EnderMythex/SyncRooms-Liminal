namespace SyncRooms.Core
{
    /// <summary>"Distance d'affichage" setting: how far chunks, lights and the camera reach.</summary>
    public static class RenderDistance
    {
        static int Level => GameManager.Instance ? GameManager.Instance.Settings.viewDistance : 0;
        public static int ExtraChunks => Level switch { 1 => 1, 2 => 3, _ => 0 };
        public static float LightMultiplier => Level switch { 1 => 1.8f, 2 => 4f, _ => 1f };
        public static float FarClip => Level switch { 1 => 3000f, 2 => 30000f, _ => 1500f };
    }
}
