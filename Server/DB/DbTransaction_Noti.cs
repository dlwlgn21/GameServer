using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game.Item;
using Server.Game.Job;
using Server.Game.Object;
using Server.Game.Room;
using Server.Utills;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server.DB
{
    public partial class DbTransaction : JobSerializer
    {
        public static void EqiupItemNoti(Player player, Item item)
        {
            Debug.Assert(player != null && item != null);

            ItemDb itemDb = new ItemDb()
            {
                ItemDbId = item.ItemDbId,
                IsEquiped = item.IsEquiped
            };

            Instance.Push(()=>
            {
                using (GameDbContext db = new GameDbContext())
                {
                    db.Entry(itemDb).State = EntityState.Unchanged;
                    db.Entry(itemDb).Property(nameof(itemDb.IsEquiped)).IsModified = true;

                    bool isSuccess = db.SaveChangesEx();
                    Debug.Assert(isSuccess == true);
                    if (!isSuccess)
                    {
                        // TODO : 기획에 따라 실패했으면 킥!
                        return;
                    }
                }
            });
        }
    }
}
