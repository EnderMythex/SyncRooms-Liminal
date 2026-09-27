using System;
using System.Collections.Generic;
using UnityEngine;

namespace SyncRooms.Inventory
{
    [Serializable]
    public class ItemStack
    {
        public ItemDefinition item;
        public int count;
        public bool IsEmpty => item == null || count <= 0;
    }

    /// <summary>Slot-based inventory. Lives on the persistent GameManager so it survives level changes.</summary>
    public class PlayerInventory : MonoBehaviour
    {
        public const int SlotCount = 20;   // 5 x 4 grid; the first 5 slots form the hotbar
        public const int HotbarSize = 5;

        [SerializeField] ItemStack[] slots = new ItemStack[SlotCount];

        public event Action Changed;
        public event Action<ItemDefinition, int> ItemAdded;

        public IReadOnlyList<ItemStack> Slots => slots;
        /// <summary>Hotbar slot held in hand (0..4).</summary>
        public int SelectedSlot { get; private set; }
        public void Select(int i) { SelectedSlot = Mathf.Clamp(i, 0, HotbarSize - 1); Changed?.Invoke(); }

        void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i] ??= new ItemStack();
        }

        public int Count(string id)
        {
            int n = 0;
            foreach (var s in slots)
                if (!s.IsEmpty && s.item.id == id) n += s.count;
            return n;
        }

        public bool Has(string id) => Count(id) > 0;

        /// <summary>Adds items, filling existing stacks first. Returns how many could not fit.</summary>
        public int Add(ItemDefinition item, int count = 1)
        {
            int remaining = count;
            foreach (var s in slots)
            {
                if (remaining == 0) break;
                if (!s.IsEmpty && s.item == item && s.count < item.maxStack)
                {
                    int add = Mathf.Min(item.maxStack - s.count, remaining);
                    s.count += add;
                    remaining -= add;
                }
            }
            foreach (var s in slots)
            {
                if (remaining == 0) break;
                if (s.IsEmpty)
                {
                    int add = Mathf.Min(item.maxStack, remaining);
                    s.item = item;
                    s.count = add;
                    remaining -= add;
                }
            }
            int added = count - remaining;
            if (added > 0)
            {
                ItemAdded?.Invoke(item, added);
                Changed?.Invoke();
            }
            return remaining;
        }

        public bool Remove(string id, int count = 1)
        {
            if (Count(id) < count) return false;
            for (int i = slots.Length - 1; i >= 0 && count > 0; i--)
            {
                var s = slots[i];
                if (s.IsEmpty || s.item.id != id) continue;
                int take = Mathf.Min(s.count, count);
                s.count -= take;
                count -= take;
                if (s.count <= 0) s.item = null;
            }
            Changed?.Invoke();
            return true;
        }

        public void RemoveAt(int index, int count = 1)
        {
            var s = slots[index];
            if (s.IsEmpty) return;
            s.count -= count;
            if (s.count <= 0) { s.item = null; s.count = 0; }
            Changed?.Invoke();
        }

        /// <summary>Swap or merge two slots (drag &amp; drop).</summary>
        public void Move(int from, int to)
        {
            if (from == to) return;
            var a = slots[from];
            var b = slots[to];
            if (!a.IsEmpty && !b.IsEmpty && a.item == b.item && b.count < b.item.maxStack)
            {
                int add = Mathf.Min(b.item.maxStack - b.count, a.count);
                b.count += add;
                a.count -= add;
                if (a.count <= 0) a.item = null;
            }
            else
            {
                slots[from] = b;
                slots[to] = a;
            }
            Changed?.Invoke();
        }

        public void Clear()
        {
            foreach (var s in slots) { s.item = null; s.count = 0; }
            Changed?.Invoke();
        }

        // --- Save / load (item ids only, so it survives across sessions) ---
        [Serializable] class SaveData { public string[] ids; public int[] counts; }

        public string ToJson()
        {
            var d = new SaveData { ids = new string[SlotCount], counts = new int[SlotCount] };
            for (int i = 0; i < SlotCount; i++)
            {
                d.ids[i] = slots[i].IsEmpty ? "" : slots[i].item.id;
                d.counts[i] = slots[i].IsEmpty ? 0 : slots[i].count;
            }
            return JsonUtility.ToJson(d);
        }

        public void FromJson(string json)
        {
            var d = JsonUtility.FromJson<SaveData>(json);
            if (d?.ids == null) return;
            var db = ItemDatabase.Instance;
            for (int i = 0; i < SlotCount && i < d.ids.Length; i++)
            {
                var def = string.IsNullOrEmpty(d.ids[i]) ? null : db.Get(d.ids[i]);
                slots[i].item = def;
                slots[i].count = def ? d.counts[i] : 0;
            }
            Changed?.Invoke();
        }
    }
}
