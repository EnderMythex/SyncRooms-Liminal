using System.Collections.Generic;
using UnityEngine;

namespace SyncRooms.Inventory
{
    [CreateAssetMenu(menuName = "SyncRooms/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new();

        Dictionary<string, ItemDefinition> _byId;

        static ItemDatabase _instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance == null) _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                return _instance;
            }
        }

        public ItemDefinition Get(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, ItemDefinition>();
                foreach (var it in items)
                    if (it != null) _byId[it.id] = it;
            }
            return _byId.TryGetValue(id, out var def) ? def : null;
        }
    }
}
