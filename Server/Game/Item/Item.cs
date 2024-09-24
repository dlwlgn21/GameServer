using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server.Game.Item
{
    public class Item
    {
        public ItemInfo Info { get; set; } = new ItemInfo();
        public int ItemDbId
        {
            get { return Info.ItemDbId; }
            set { Info.ItemDbId = value; }
        }
        public int TemplatedId
        {
            get { return Info.TemplatedId; }
            set { Info.TemplatedId = value; }
        }
        public int Count
        {
            get { return Info.Count; }
            set { Info.Count = value; }
        }
        public int Slot
        {
            get { return Info.Slot; }
            set { Info.Slot = value; }
        }

        public bool IsEquiped
        {
            get { return Info.IsEquiped; }
            set { Info.IsEquiped = value; }
        }


        public ItemType ItemType { get; private set; }
        public bool IsStackable { get; protected set; }
        public Item(ItemType eType)
        {
            ItemType = eType;
        }

        public static Item MakeItem(ItemDb itemDb)
        {
            Item item = null;
            ItemData itemData;
            DataManager.ItemDataMap.TryGetValue(itemDb.TemplatedId, out itemData);
            if (itemData == null)
            {
                Debug.Assert(false);
                return null;
            }

            switch (itemData.itemType)
            {
                case ItemType.None:
                    Debug.Assert(false);
                    break;
                case ItemType.Weapon:
                    item = new Weapon(itemDb.TemplatedId);
                    break;
                case ItemType.Armor:
                    item = new Armor(itemDb.TemplatedId);
                    break;
                case ItemType.Consumable:
                    item = new Consumable(itemDb.TemplatedId);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            if (item != null)
            {
                item.ItemDbId = itemDb.ItemDbId;
                item.Count = itemDb.Count;
                item.Slot = itemDb.Slot;
                item.IsEquiped = itemDb.IsEquiped;
            }
            return item;

        }
    }

    public class Weapon : Item
    {
        public WeaponType WeaponType { get; private set; }
        public int Damage { get; private set; }
        public Weapon(int templatedId) : base(ItemType.Weapon) 
        {
            Init(templatedId);
        }
        void Init(int templatedId)
        {
            ItemData itemData = null;
            DataManager.ItemDataMap.TryGetValue(templatedId, out itemData);
            if (itemData.itemType != ItemType.Weapon || itemData == null)
            {
                Debug.Assert(false);
                return;
            }

            WeaponData data = (WeaponData)itemData;
            {
                TemplatedId = data.id;
                Count = 1;
                WeaponType = data.weaponType;
                Damage = data.damage;
                IsStackable = false;
            }
        }
    }

    public class Armor : Item
    {
        public ArmorType ArmorType { get; private set; }
        public int Defence { get; private set; }
        public Armor(int templatedId) : base(ItemType.Armor)
        {
            Init(templatedId);
        }
        void Init(int templatedId)
        {
            ItemData itemData = null;
            DataManager.ItemDataMap.TryGetValue(templatedId, out itemData);
            if (itemData.itemType != ItemType.Armor || itemData == null)
            {
                Debug.Assert(false);
                return;
            }

            ArmorData data = (ArmorData)itemData;
            {
                TemplatedId = data.id;
                Count = 1;
                ArmorType = data.armorType;
                Defence = data.defence;
                IsStackable = false;
            }
        }
    }

    public class Consumable : Item
    {
        public ConsumableType ConsumableType { get; private set; }
        public int MaxCount { get; private set; }
        public Consumable(int templatedId) : base(ItemType.Consumable)
        {
            Init(templatedId);
        }
        void Init(int templatedId)
        {
            ItemData itemData = null;
            DataManager.ItemDataMap.TryGetValue(templatedId, out itemData);
            if (itemData.itemType != ItemType.Consumable || itemData == null)
            {
                Debug.Assert(false);
                return;
            }

            ConsumableData data = (ConsumableData)itemData;
            {
                TemplatedId = data.id;
                Count = 1;
                ConsumableType = data.consumableType;
                MaxCount = data.maxCount;
                IsStackable = data.maxCount > 1;
            }
        }
    }
}
