using UnityEngine;

namespace SyncRooms.Inventory
{
    public enum ItemUse
    {
        None,       // collectible / lore
        Drink,      // restores sanity + stamina (Almond Water)
        Eat,        // restores stamina regen
        Heal,       // restores sanity a lot
        Battery,    // refills flashlight
        Flashlight, // equips the flashlight
        Read,       // opens the note reader
        Key         // consumed automatically by locked doors
    }

    [CreateAssetMenu(menuName = "SyncRooms/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 6)] public string description;
        public Sprite icon;
        public GameObject worldPrefab;
        [Min(1)] public int maxStack = 1;
        public ItemUse use;
        public float amount = 25f;
        public bool consumeOnUse = true;
        [TextArea(4, 20)] public string noteText;
    }
}
