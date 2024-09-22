using Google.Protobuf;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Game.Item;
using Server.Game.Object;
using Server.Game.Room;
using Server.Utills;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        public int AccountDbId { get; private set; }
        public List<LobbyPlayerInfo> LobbyPlayers { get; set; } = new List<LobbyPlayerInfo>();

        public void HandleLogin(C_Login loginPkt)
        {
            // TODO : 이런저런 보안 체크으..
            if (ServerState != PlayerServerState.ServerStateLogin)
                return;
            LobbyPlayers.Clear();

            // TODO : 문제가 있긴 있다.
            // - 동시에 다른 사람이 악의적으로 같은 UniqueId를 보낸다면?
            // - 악의적으로 같은 패킷을 여러번 보낸다면? (한 1초에 100번 씩...)
            // - 쌩뚱맞은 타이밍에 그냥 이 패킷을 보낸다면? (이미 게임에 들어가 있는데...)
            // - 상태로 구분해서 체크해주는게 좋다.

            using (GameDbContext db = new GameDbContext())
            {
                AccountDb findAcc = db.Accounts
                    .Include(acc => acc.Players)
                    .Where(acc => acc.AccountName == loginPkt.UniqueId).FirstOrDefault();
                if (findAcc != null)
                {
                    // AccDbId 메모리에 기억
                    AccountDbId = findAcc.AccountDbId;
                    S_Login loginOkPkt = new S_Login() { LoginOk = 1 };
                    foreach (PlayerDb playerDb in findAcc.Players)
                    {
                        LobbyPlayerInfo lobbyPlayerPkt = new LobbyPlayerInfo()
                        {
                            PlayerDbId = playerDb.PlayerDbId,
                            Name = playerDb.PlayerName,

                            StatInfo = new StatInfo()
                            {
                                Level = playerDb.Level,
                                Hp = playerDb.Hp,
                                MaxHp = playerDb.MaxHp,
                                Attack = playerDb.Attack,
                                Speed = playerDb.Speed,
                                TotalExp = playerDb.TotalExp
                            }
                        };
                        // 메모리에도 들고 있게 해준다.
                        LobbyPlayers.Add(lobbyPlayerPkt);

                        // 패킷에 넣어준다.
                        loginOkPkt.Players.Add(lobbyPlayerPkt);
                    }

                    Send(loginOkPkt);

                    // 로비로 이동
                    ServerState = PlayerServerState.ServerStateLobby;
                }
                else
                {
                    AccountDb newAcc = new AccountDb() { AccountName = loginPkt.UniqueId };
                    db.Accounts.Add(newAcc);
                    // AccDbId 메모리에 기억
                    AccountDbId = newAcc.AccountDbId;
                    bool isSaveSuccess = db.SaveChangesEx();
                    if (!isSaveSuccess)
                    {
                        Debug.Assert(false, "Save Failed!!");
                        return;
                    }
                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);

                    // 로비로 이동
                    ServerState = PlayerServerState.ServerStateLobby;
                }
            }
        }
        public void HandleEnterGame(C_EnterGame enterPkt)
        {
            if (ServerState != PlayerServerState.ServerStateLobby)
            {
                Debug.Assert(false, "-------------ClientSession.HandleEnterGame() ServerState != PlayerServerState.ServerStateLobby-----------------");
                return;
            }

            LobbyPlayerInfo playerInfo = LobbyPlayers.Find(player => player.Name == enterPkt.Name);
            if (playerInfo == null)
            {
                Debug.Assert(false);
                return;
            }

            OwnerPlayer = ObjectManager.Instance.Add<Player>();
            {
                OwnerPlayer.PlayerDbId = playerInfo.PlayerDbId;
                OwnerPlayer.Info.Name = playerInfo.Name;
                OwnerPlayer.Info.PosInfo.State = CharacterState.Idle;
                OwnerPlayer.Info.PosInfo.MoveDir = MoveDir.Right;
                OwnerPlayer.Info.PosInfo.PosX = 0;
                OwnerPlayer.Info.PosInfo.PosY = 0;
                OwnerPlayer.StatInfo.MergeFrom(playerInfo.StatInfo);
                OwnerPlayer.Session = this;

                S_ItemList itemListPkt = new S_ItemList();
                // Player가 들고 있는 아이템 목록을 가지고 온다.
                using (GameDbContext db = new GameDbContext())
                {
                    List<ItemDb> items = db.Items.Where(item => item.OwnerDbId == playerInfo.PlayerDbId).ToList();

                    foreach (ItemDb itemDb in items)
                    {
                        Item item = Item.MakeItem(itemDb);
                        if (item != null)
                        {
                            OwnerPlayer.Inven.Add(item);
                            ItemInfo info = new ItemInfo();
                            info.MergeFrom(item.Info);
                            itemListPkt.Items.Add(info);
                        }
                    }
                }
                Send(itemListPkt);
            }
            ServerState = PlayerServerState.ServerStateGame;
            GameRoom room = RoomManager.Instance.FindOrNUll(1);
            room.Push(room.EnterGame, OwnerPlayer);
        }
        public void HandleCreatePlayer(C_CreatePlayer createPkt)
        {
            if (ServerState != PlayerServerState.ServerStateLobby)
                return;

            using (GameDbContext db = new GameDbContext())
            {
                PlayerDb findPlayer = db.Players
                    .Where(player => player.PlayerName == createPkt.Name)
                    .FirstOrDefault();

                if (findPlayer != null)
                {
                    // 이름이 겹친다. 이름이 선점되어 있음.
                    // 그냥 빈 패킷을 보내준다.
                    Send(new S_CreatePlayer());
                }
                else
                {
                    // 1레벨 스탯 정보 추출
                    StatInfo stat = null;
                    if (!DataManager.StatMap.TryGetValue(1, out stat))
                    {
                        Debug.Assert(false);
                        return;
                    }

                    // DB에 플레이어 만들어 줘야함
                    PlayerDb newPlayer = new PlayerDb()
                    {
                        PlayerName = createPkt.Name,
                        Level = stat.Level,
                        Hp = stat.Hp,
                        MaxHp = stat.MaxHp,
                        Attack = stat.Attack,
                        Speed = stat.Speed,
                        TotalExp = 0,
                        AccountDbId = AccountDbId
                    };

                    db.Players.Add(newPlayer);
                    bool isSaveSuccess = db.SaveChangesEx();
                    if (!isSaveSuccess)
                    {
                        Debug.Assert(false, "Save Failed!!");
                        return;
                    }

                    // 메모리에 추가
                    LobbyPlayerInfo lobbyPlayerPkt = new LobbyPlayerInfo()
                    {
                        PlayerDbId = newPlayer.PlayerDbId,
                        Name = createPkt.Name,
                        StatInfo = new StatInfo()
                        {
                            Level = stat.Level,
                            Hp = stat.Hp,
                            MaxHp = stat.MaxHp,
                            Attack = stat.Attack,
                            Speed = stat.Speed,
                            TotalExp = 0
                        }
                    };
                    LobbyPlayers.Add(lobbyPlayerPkt);

                    // 클라에 전송
                    S_CreatePlayer newPlayerPkt = new S_CreatePlayer()
                    {
                        Player = new LobbyPlayerInfo()
                    };
                    newPlayerPkt.Player.MergeFrom(lobbyPlayerPkt);
                    Send(newPlayerPkt);
                }
            }
        }
    }
}
