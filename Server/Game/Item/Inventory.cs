using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server.Game.Item
{
    public class Inventory
    {
        Dictionary<int, Item> _itemMap = new Dictionary<int, Item>();
        public void Add(Item item)
        {
            _itemMap.Add(item.ItemDbId, item);
        }

        public Item GetOrNull(int itemId)
        {
            Item item = null;
            if (_itemMap.TryGetValue(itemId, out item)) 
                return item;
            Debug.Assert(false);
            return null;
        }

        public Item FindByConditionOrNull(Func<Item, bool> condition)
        {
            Debug.Assert(condition != null);
            foreach (Item item in _itemMap.Values)
            {
                if (condition.Invoke(item))
                    return item;
            }
            Debug.Assert(false);
            return null;
        }
    }
}
