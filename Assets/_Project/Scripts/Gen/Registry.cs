using System.Collections.Generic;
using SyncRooms.Inventory;
using UnityEngine;

namespace SyncRooms.Gen
{
    /// <summary>Runtime lookup of materials, prefabs and items by name (filled by the editor pipeline).</summary>
    public class Registry : ScriptableObject
    {
        [System.Serializable] public class MatEntry { public string name; public Material mat; }
        [System.Serializable] public class PrefabEntry { public string name; public GameObject prefab; }

        public List<MatEntry> materials = new();
        public List<PrefabEntry> prefabs = new();

        Dictionary<string, Material> _m;
        Dictionary<string, GameObject> _p;
        static Registry _inst;

        public static Registry I
        {
            get
            {
                if (_inst == null) _inst = Resources.Load<Registry>("Registry");
                return _inst;
            }
        }

        public static Material Mat(string name)
        {
            var r = I;
            if (r._m == null)
            {
                r._m = new Dictionary<string, Material>();
                foreach (var e in r.materials) if (e.mat) r._m[e.name] = e.mat;
            }
            return r._m.TryGetValue(name, out var m) ? m : null;
        }

        public static GameObject Prefab(string name)
        {
            var r = I;
            if (r._p == null)
            {
                r._p = new Dictionary<string, GameObject>();
                foreach (var e in r.prefabs) if (e.prefab) r._p[e.name] = e.prefab;
            }
            if (r._p.TryGetValue(name, out var p)) return p;
            Debug.LogWarning("[Registry] missing prefab " + name);
            return null;
        }

        public static ItemDefinition Item(string id) => ItemDatabase.Instance.Get(id);
    }
}
