using SyncRooms.Core;
using SyncRooms.Inventory;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>A physical item that can be picked up into the inventory.</summary>
    public class WorldItem : MonoBehaviour, IInteractable
    {
        public ItemDefinition item;
        [Min(1)] public int count = 1;

        public string Prompt => item == null ? null :
            count > 1 ? $"[E] Ramasser {item.displayName} x{count}" : $"[E] Ramasser {item.displayName}";
        public bool CanInteract => item != null;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            int left = gm.Inventory.Add(item, count);
            if (left == count)
            {
                gm.UI.Hud.Notify("Inventaire plein");
                return;
            }
            Sfx.Play2D("SFX/sfx_pickup", 0.6f);
            if (item.use == ItemUse.Read) Sfx.Play2D("SFX/sfx_paper", 0.7f);
            if (left > 0) count = left;
            else Destroy(gameObject);
        }
    }
}
