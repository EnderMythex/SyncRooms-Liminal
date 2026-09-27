using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Slowly sweeps around the Y axis (searchlights).</summary>
    public class Rotator : MonoBehaviour
    {
        public float degreesPerSecond = 12f;
        void Update() => transform.Rotate(0, degreesPerSecond * Time.deltaTime, 0, Space.World);
    }
}
