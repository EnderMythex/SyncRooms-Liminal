using SyncRooms.Core;
using SyncRooms.Inventory;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Water cooler / vending machine / fountain: gives an item a limited number of times.</summary>
    public class Dispenser : MonoBehaviour, IInteractable
    {
        public ItemDefinition item;
        public int uses = 2;
        public string verb = "Remplir une bouteille";
        public string emptyMessage = "Vide.";
        public string sfx = "SFX/sfx_drink";

        public string Prompt => item == null ? null : uses > 0 ? $"[E] {verb} ({item.displayName})" : "[E] " + emptyMessage;
        public bool CanInteract => item != null;

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (uses <= 0) { gm.UI.Hud.Notify(emptyMessage); return; }
            if (gm.Inventory.Add(item) > 0) { gm.UI.Hud.Notify("Inventaire plein"); return; }
            uses--;
            Sfx.PlayAt(sfx, transform.position + Vector3.up, 0.6f);
        }
    }
}
