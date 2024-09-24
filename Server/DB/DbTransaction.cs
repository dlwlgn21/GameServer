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
        public static DbTransaction Instance { get; } = new DbTransaction();
        
        // Me (GameRoom) -> You (Db) => Me (GameRoom)
        public static void SavePlayerStatus_AllInOne(Player player, GameRoom room)
        {
            if (player == null || room == null)
            {
                Debug.Assert(false);
                return;
            }
            // Me (GameRoom) -> 요 일은 내가 처리 가능하다! 느린 연산 아니자네!
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.StatInfo.Hp;


            // You -> 느리고 비싼 요 일감 처리해줘!
            Instance.Push(() => 
            {
                using (GameDbContext db = new GameDbContext())
                {
                    // DB 접근을 최소화 하기 위해 이런식으로 짬.

                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(playerDb.Hp)).IsModified = true;
                    bool isSaveSuccess = db.SaveChangesEx();
                    if (isSaveSuccess)
                    {
                        // Me -> 그니까 다시 '나'한테 일감을 다시 던져줌.
                        room.Push(() => { Console.WriteLine($"Hp Saved At DB {playerDb.Hp}"); });
                    }
                    else
                    {
                        Debug.Assert(false, "Save Failed!!");
                        return;
                    }
                }
            });

        }
        // Me
        public static void SavePlayerStatus_Step1(Player player, GameRoom room)
        {
            if (player == null || room == null)
            {
                Debug.Assert(false);
                return;
            }
            // Me (GameRoom) -> 요 일은 내가 처리 가능하다! 느린 연산 아니자네!
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.StatInfo.Hp;
            Instance.Push<PlayerDb, GameRoom>(SavePlayerStatus_Step2, playerDb, room);
        }
        // You
        public static void SavePlayerStatus_Step2(PlayerDb playerDb, GameRoom room)
        {
            // You -> 느리고 비싼 요 일감 처리해줘!
            using (GameDbContext db = new GameDbContext())
            {
                // DB 접근을 최소화 하기 위해 이런식으로 짬.

                db.Entry(playerDb).State = EntityState.Unchanged;
                db.Entry(playerDb).Property(nameof(playerDb.Hp)).IsModified = true;
                bool isSaveSuccess = db.SaveChangesEx();
                if (isSaveSuccess)
                {
                    // Me -> 그니까 다시 '나'한테 일감을 다시 던져줌.
                    room.Push(SavePlayerStatus_Step3, playerDb.Hp);
                }
                else
                {
                    Debug.Assert(false, "Save Failed!!");
                    return;
                }
            }
        }
        // Me
        public static void SavePlayerStatus_Step3(int hp)
        {
            Console.WriteLine($"Hp Saved At DB {hp}");
        }

        public static void RewardToPlayer(Player player, RewardData rewradData, GameRoom room)
        {
            Debug.Assert(player != null && rewradData != null && room != null);

            // TODO : 살짝 문제가 있긴 하다...
            // 문제가 있는 부분 -> 타이밍이슈가 발생할 수 있음.
            // 1. DB에다가 저장 요청 -> 이곳에서 동시에 요청할 경우 문제 발생.
            // 2. DB저장 OK
            // 3. 메모리에 적용
            int? slot = player.Inven.GetEmptySlotOrNull();
            if (slot == null)
            {
                Console.WriteLine("Inven Full!!");
                return;
            }

            ItemDb itemDb = new ItemDb()
            {
                TemplatedId = rewradData.itemId,
                Count = rewradData.count,
                Slot = slot.Value,
                OwnerDbId = player.PlayerDbId
            };

            Instance.Push(() =>
            {
                using (GameDbContext db = new GameDbContext())
                {
                    // DB 접근을 최소화 하기 위해 이런식으로 짬.
                    db.Items.Add(itemDb);
                    bool isSaveSuccess = db.SaveChangesEx();
                    Debug.Assert(isSaveSuccess);
                    if (isSaveSuccess)
                    {
                        // Me -> 그니까 다시 '나'한테 일감을 다시 던져줌.
                        room.Push(() => 
                        {
                            Item newItem = Item.MakeItem(itemDb);
                            player.Inven.Add(newItem);

                            {
                                S_AddItem addItemPkt = new S_AddItem();
                                ItemInfo itemInfo = new ItemInfo();
                                itemInfo.MergeFrom(newItem.Info);
                                addItemPkt.Items.Add(itemInfo);
                                player.Session.Send(addItemPkt);
                            }
                        });
                    }
                }
            });
        }
    }
}
