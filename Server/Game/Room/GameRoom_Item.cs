using Google.Protobuf.Protocol;
using Server.Game.Job;
using Server.Game.Object;

using System.Diagnostics;

using Server.DB;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        public void HadnleEqiupItem(Player player, C_EquipItem eqiupPkt)
        {
            Debug.Assert(player != null);
            if (player == null)
                return;

            Server.Game.Item.Item item = player.Inven.GetOrNull(eqiupPkt.ItemDbId);
            Debug.Assert(item != null);




            // 메모리 선 적용.
            item.IsEquiped = eqiupPkt.IsEquiped;

            // Noti To DB
            DbTransaction.EqiupItemNoti(player, item);

            // Noti To Client
            S_EquipItem equipOkPkt = new S_EquipItem();
            equipOkPkt.ItemDbId = eqiupPkt.ItemDbId;
            equipOkPkt.IsEquiped = eqiupPkt.IsEquiped;
            player.Session.Send(equipOkPkt);
        }

    }
}
