using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
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
    public class DbTransaction : JobSerializer
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
    }
}
