using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.Monsters
{
    /// <summary>Trigger volume where entities cannot target the player (house interiors, bunker...).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SafeZone : MonoBehaviour
    {
        static int _inside;
        public static bool PlayerInside => _inside > 0;

        void OnTriggerEnter(Collider c) { if (c.GetComponent<FirstPersonController>()) _inside++; }
        void OnTriggerExit(Collider c) { if (c.GetComponent<FirstPersonController>()) _inside = Mathf.Max(0, _inside - 1); }
        void OnDisable() => _inside = 0;
    }
}
