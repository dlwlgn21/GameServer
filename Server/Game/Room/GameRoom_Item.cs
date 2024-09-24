using Google.Protobuf.Protocol;
using Server.Game.Job;
using Server.Game.Object;

using System.Diagnostics;

using Server.DB;
using Server.Game.Item;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        public void HadnleEqiupItem(Player player, C_EquipItem eqiupPkt)
        {
            Debug.Assert(player != null);
            if (player == null)
                return;
            player.HandleEquipItem(eqiupPkt);
        }

    }
}
