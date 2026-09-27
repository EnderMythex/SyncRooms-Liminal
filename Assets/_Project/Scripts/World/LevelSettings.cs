using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>One per level scene: spawn point and how the level affects sanity.</summary>
    public class LevelSettings : MonoBehaviour
    {
        public static LevelSettings Current { get; private set; }

        public Transform spawnPoint;
        [Tooltip("Sanity lost per minute while in this level.")]
        public float sanityDrainPerMinute = 1.5f;
        [Tooltip("Extra drain per minute when the flashlight is off (dark levels).")]
        public float darknessDrainPerMinute;
        [Tooltip("Ambient loops played 2D for the whole level (Resources/Audio paths).")]
        public string[] ambientLoops;
        public float ambientVolume = 0.5f;
        public Player.ArrivalKind arrival = Player.ArrivalKind.None;

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }
    }
}
