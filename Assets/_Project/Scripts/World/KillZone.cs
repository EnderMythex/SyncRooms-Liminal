using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Falling into lethal places (the freezing ocean): the player is pulled back to the spawn, hurt.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class KillZone : MonoBehaviour
    {
        public string message = "L'eau glacée vous engourdit... vous remontez à bord de justesse.";
        public float damage = 25f;

        void OnTriggerEnter(Collider c)
        {
            if (!c.TryGetComponent(out FirstPersonController p)) return;
            var ls = LevelSettings.Current;
            if (ls && ls.spawnPoint) p.Teleport(ls.spawnPoint.position, ls.spawnPoint.eulerAngles.y);
            GameManager.Instance.Hurt(damage);
            GameManager.Instance.UI.Hud.Notify(message);
        }
    }
}
