using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Trigger box of water: slows the player, switches footsteps, splash on entry.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaterZone : MonoBehaviour
    {
        public float surfaceHeight;   // world Y of the water surface
        public float SurfaceHeight => surfaceHeight;

        public float DepthAt(Vector3 feet) => Mathf.Max(0f, surfaceHeight - feet.y);

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out FirstPersonController fpc)) return;
            fpc.CurrentWater = this;
            Sfx.PlayAt("Footsteps/fs_water", other.transform.position, 0.8f);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out FirstPersonController fpc) && fpc.CurrentWater == this)
                fpc.CurrentWater = null;
        }
    }
}
