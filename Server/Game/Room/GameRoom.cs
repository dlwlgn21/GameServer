using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Job;
using Server.Game.Object;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        public int RoomId { get; set; }

        Dictionary<int, Player> _playerMap = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsterMap = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectileMap = new Dictionary<int, Projectile>();
        public Map Map { get; private set; } = new Map();

        public void Init(int mapId)
        {
            Map.LoadMap(mapId);
            // TODO : TMP
            Monster monster = ObjectManager.Instance.Add<Monster>();
            monster.Init(1);
            monster.CellPos = new Vector2Int(5, 5);
            Push(EnterGame, monster);
        }
 
        // 누군가가 주기적으로 호출해주어야 한다!
        public void Update()
        {
            foreach (var projectile in _projectileMap.Values)
                projectile.Update();
            foreach (var monster in _monsterMap.Values)
                monster.Update();
            
            Flush();
        }

        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.Assert(false);
                return;
            }
        GameObjectType eType = ObjectManager.GetOjbectTypeById(gameObject.Id);

            if (eType == GameObjectType.Player)
            {
                Player newPlayer = (Player)gameObject;
                _playerMap.Add(newPlayer.Id, newPlayer);
                newPlayer.Room = this;
                Map.ApplyMove(newPlayer, new Vector2Int(newPlayer.CellPos.x, newPlayer.CellPos.y));

                // 본인한테 정보 전송 [나 님 두둥 등장]
                {
                    // Enter
                    S_EnterGame enterPkt = new S_EnterGame();
                    enterPkt.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPkt);

                    // Spawn
                    S_Spawn spawnPkt = new S_Spawn();
                    foreach (Player p in _playerMap.Values)
                    {
                        if (newPlayer != p)
                            spawnPkt.Objects.Add(p.Info);
                    }
                    foreach (Monster m in _monsterMap.Values)
                        spawnPkt.Objects.Add(m.Info);

                    foreach (Projectile p in _projectileMap.Values)
                        spawnPkt.Objects.Add(p.Info);

                    newPlayer.Session.Send(spawnPkt);
                }
            }
            else if (eType == GameObjectType.Monster)
            {
                Monster newMonster = (Monster)gameObject;
                _monsterMap.Add(newMonster.Id, newMonster);
                newMonster.Room = this;
                Map.ApplyMove(newMonster, new Vector2Int(newMonster.CellPos.x, newMonster.CellPos.y));
            }
            else if (eType == GameObjectType.Projectile)
            {
                Projectile newProjectile = (Projectile)gameObject;
                _projectileMap.Add(newProjectile.Id, newProjectile);
                newProjectile.Room = this;
            }
                
            // 타인들에게 정보 전송 [신입 왔어!!]
            {
                S_Spawn spawnPkt = new S_Spawn();
                spawnPkt.Objects.Add(gameObject.Info);
                foreach (Player p in _playerMap.Values)
                {
                    if (p.Id != gameObject.Id)
                        p.Session.Send(spawnPkt);
                }
            }
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType eType = ObjectManager.GetOjbectTypeById(objectId);

            if (eType == GameObjectType.Player)
            {
                Player player = null;
                if (_playerMap.Remove(objectId, out player) == false)
                    return;

                player.OnLeaveGame();
                Map.ApplyLeaveFromGrid(player);
                player.Room = null;
                // 본인에게 정보 전송 [나 님 나가!]
                {
                    S_LeaveGame leavePkt = new S_LeaveGame();
                    player.Session.Send(leavePkt);
                }
            }
            else if (eType == GameObjectType.Monster)
            {
                Monster monster = null;
                if (_monsterMap.Remove(objectId, out monster) == false)
                    return;
                Map.ApplyLeaveFromGrid(monster);
                monster.Room = null;
            }
            else if (eType == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if (_projectileMap.Remove(objectId, out projectile) == false)
                    return;
                projectile.Room = null;
            }

            // 타인들에게 정보 전송 [님들 나 나감요!]
            {
                S_Despawn despawnPkt = new S_Despawn();
                despawnPkt.ObjectIds.Add(objectId);
                foreach (Player p in _playerMap.Values)
                {
                    if (p.Id != objectId)
                        p.Session.Send(despawnPkt);
                }
            }
        }

        public Player FindPlayerByConditionOrNull(Func<GameObject, bool> condition)
        {
            foreach (Player player in _playerMap.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }
            return null;
        }
        public void BroadcastToAllPlayer(IMessage pkt)
        {
            foreach (Player p in _playerMap.Values)
            {
                p.Session.Send(pkt);
            }
        }
    }
}
