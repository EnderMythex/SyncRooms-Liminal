using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.Inventory
{
    /// <summary>Applies the effect of using an item from a given slot.</summary>
    public static class ItemActions
    {
        public static string UseLabel(ItemDefinition def) => def.use switch
        {
            ItemUse.Drink => "Boire",
            ItemUse.Eat => "Manger",
            ItemUse.Heal => "Utiliser",
            ItemUse.Battery => "Recharger la lampe",
            ItemUse.Flashlight => "Allumer / éteindre",
            ItemUse.Read => "Lire",
            _ => null,
        };

        public static void Use(int slot)
        {
            var gm = GameManager.Instance;
            var stack = gm.Inventory.Slots[slot];
            if (stack.IsEmpty) return;
            var def = stack.item;
            var s = gm.State;
            bool consumed = def.consumeOnUse;

            switch (def.use)
            {
                case ItemUse.Drink:
                    s.sanity = Mathf.Min(100f, s.sanity + def.amount);
                    Sfx.Play2D("SFX/sfx_drink");
                    gm.UI.Hud.Notify($"{def.displayName} : +{def.amount:0} santé mentale");
                    break;
                case ItemUse.Eat:
                    s.sanity = Mathf.Min(100f, s.sanity + def.amount);
                    Sfx.Play2D("SFX/sfx_eat");
                    gm.UI.Hud.Notify($"{def.displayName} : +{def.amount:0} santé mentale");
                    break;
                case ItemUse.Heal:
                    s.sanity = Mathf.Min(100f, s.sanity + def.amount);
                    Sfx.Play2D("SFX/sfx_bag");
                    gm.UI.Hud.Notify($"Vous vous sentez mieux (+{def.amount:0})");
                    break;
                case ItemUse.Battery:
                    if (!gm.Inventory.Has(Flashlight.ItemId)) { gm.UI.Hud.Notify("Il vous faut une lampe torche"); return; }
                    if (s.battery >= 99f) { gm.UI.Hud.Notify("La batterie est déjà pleine"); return; }
                    s.battery = Mathf.Min(100f, s.battery + def.amount);
                    Sfx.Play2D("SFX/sfx_flashlight");
                    gm.UI.Hud.Notify($"Batterie : {s.battery:0}%");
                    break;
                case ItemUse.Flashlight:
                    Object.FindAnyObjectByType<Flashlight>()?.Toggle();
                    consumed = false;
                    break;
                case ItemUse.Read:
                    gm.UI.ShowNote(def.displayName, def.noteText);
                    consumed = false;
                    break;
                default:
                    gm.UI.Hud.Notify(def.description);
                    return;
            }
            if (consumed) gm.Inventory.RemoveAt(slot);
        }

        /// <summary>Drops an item from a slot in front of the player as a physical pickup.</summary>
        public static void Drop(int slot)
        {
            var gm = GameManager.Instance;
            var stack = gm.Inventory.Slots[slot];
            if (stack.IsEmpty) return;
            var def = stack.item;
            var cam = Camera.main;
            if (cam == null || def.worldPrefab == null) return;
            if (def.use == ItemUse.Flashlight && gm.Inventory.Count(def.id) <= 1) gm.State.flashlightOn = false;
            Vector3 pos = cam.transform.position + cam.transform.forward * 0.8f;
            var go = Object.Instantiate(def.worldPrefab, pos, Quaternion.Euler(0, cam.transform.eulerAngles.y, 0));
            if (go.TryGetComponent(out Rigidbody rb)) rb.linearVelocity = cam.transform.forward * 1.5f;
            gm.Inventory.RemoveAt(slot);
            Sfx.Play2D("SFX/sfx_bag", 0.5f);
        }
    }
}
