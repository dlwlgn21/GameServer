using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Game.Item;
using Server.Game.Room;
using Server.Utills;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Server.Game.Object
{
    public class Player : GameObject
    {
        public int PlayerDbId { get; set; }
        public ClientSession Session { get; set; }
        public Inventory Inven { get; private set; } = new Inventory();

        public int WeaponDamage { get; private set; }
        public int ArmorDefence { get; private set; }

        public override int TotalAttack { get { return StatInfo.Attack + WeaponDamage; } }
        public override int TotalDefence { get { return ArmorDefence; } }

        public Player()
        {
            ObjectType = GameObjectType.Player;
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            base.OnDamaged(attacker, damage);
        }
        public override void OnDie(GameObject attacker)
        {
            base.OnDie(attacker);
        }

        public void OnLeaveGame()
        {
            // TODO 
            // DB연동??
            // 1. 피가 깎일때마다 DB 접근 할 필요가 있을까? DB는 최소한으로 접근하는게 성능상 좋다.
            // 인디게임이면 이정도면 괜찮지만 진지한 게임이면 문제가 되긴 함.
            // 문제점
            // 1. 서버 다운되면 아직 저장되지 않은 정보 날아감.
            // 2. 더 심각한 문제는 이런 식으로 작성하면 코드 흐름을 다 막아버린다는 매우 치명적인 문제가 있음. 
            //      보충: 지금 GameRoom에서 OnLeaveGame()호출해주고 있는데
            //      GameRoom은 JobSerializer 상속받아서 쓰레드 하나로 돌아가게 만들어놨음.
            //      근데 GameRoom에서 DB 접근하게 해버리면 일이 밀려!! 느리니까!!
            //      다른 애들도 일이 밀려버림
            // 수정방법-> 비동기 방법 사용?
            // 다른 쓰레드로 DB 일감을 던져버리면 되지 않을까?
            //      결과를 받아서 이어서 처리를 해야 하는 경우가 많음.
            //
            //  즉 결국에 허락받고 그 다음 일 처리하게 만들어주어야 함.

            DbTransaction.SavePlayerStatus_Step1(this, Room);
        }

        public void HandleEquipItem(C_EquipItem eqiupPkt)
        {
            Server.Game.Item.Item item = Inven.GetOrNull(eqiupPkt.ItemDbId);
            Debug.Assert(item != null);

            if (item.ItemType == ItemType.Consumable)
                return;

            // 착용 요청이라면, 겹치는 부위는 해제
            if (eqiupPkt.IsEquiped)
            {
                Server.Game.Item.Item unequipedItem = null;
                if (item.ItemType == ItemType.Weapon)
                {
                    unequipedItem = Inven.FindByConditionOrNull(
                        item => item.IsEquiped && item.ItemType == ItemType.Weapon);
                }
                else if (item.ItemType == ItemType.Armor)
                {
                    ArmorType armorType = ((Armor)item).ArmorType;
                    unequipedItem = Inven.FindByConditionOrNull(
                        item => item.IsEquiped &&
                        item.ItemType == ItemType.Armor &&
                        ((Armor)item).ArmorType == armorType);

                }

                if (unequipedItem != null)
                {
                    // 메모리 선 적용.
                    unequipedItem.IsEquiped = false;

                    // Noti To DB
                    DbTransaction.EqiupItemNoti(this, unequipedItem);

                    // Noti To Client
                    S_EquipItem equipOkPkt = new S_EquipItem();
                    equipOkPkt.ItemDbId = unequipedItem.ItemDbId;
                    equipOkPkt.IsEquiped = unequipedItem.IsEquiped;
                    Session.Send(equipOkPkt);
                }
            }

            {
                // 메모리 선 적용.
                item.IsEquiped = eqiupPkt.IsEquiped;

                // Noti To DB
                DbTransaction.EqiupItemNoti(this, item);

                // Noti To Client
                S_EquipItem equipOkPkt = new S_EquipItem();
                equipOkPkt.ItemDbId = eqiupPkt.ItemDbId;
                equipOkPkt.IsEquiped = eqiupPkt.IsEquiped;
                Session.Send(equipOkPkt);
            }
            RefreshAddtionalStat();
        }


        public void RefreshAddtionalStat()
        {
            WeaponDamage = 0;
            ArmorDefence = 0;

            foreach (Item.Item item in Inven.ItemMap.Values)
            {
                if (!item.IsEquiped)
                    continue;

                switch (item.ItemType)
                {
                    case ItemType.Weapon:
                        WeaponDamage += ((Weapon)item).Damage;
                        break;
                    case ItemType.Armor:
                        ArmorDefence += ((Armor)item).Defence;
                        break;
                }
            }
        }
    }
}
