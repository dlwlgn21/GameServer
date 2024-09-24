using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Server.Game.Item
{
    public class Inventory
    {
        public Dictionary<int, Item> ItemMap { get; private set; } = new Dictionary<int, Item>();
        public void Add(Item item)
        {
            ItemMap.Add(item.ItemDbId, item);
        }

        public Item GetOrNull(int itemId)
        {
            Item item = null;
            if (ItemMap.TryGetValue(itemId, out item)) 
                return item;
            Debug.Assert(false);
            return null;
        }

        public Item FindByConditionOrNull(Func<Item, bool> condition)
        {
            Debug.Assert(condition != null);
            foreach (Item item in ItemMap.Values)
            {
                if (condition.Invoke(item))
                    return item;
            }
            return null;
        }

        public int? GetEmptySlotOrNull()
        {
            for (int slot = 0; slot < 20; ++slot)
            {
                Item item = ItemMap.Values.FirstOrDefault(item => item.Slot == slot);
                if (item == null)
                    return slot;
            }
            return null;
        }
    }
}
