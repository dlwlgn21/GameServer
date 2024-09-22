using Google.Protobuf.Protocol;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace Server.Game.Object
{
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }

        long _nextMoveTick = 0;
        public override void Update()
        {
            if (OwnerData == null || OwnerData.projectileInfo == null  ||Owner == null || Room == null )
            {
                Debug.Assert(false);
                return;
            }

            if (_nextMoveTick >= Environment.TickCount64)
                return;
            // 1초를 Speed로 나눈게 내가 기다려야 하는 틱이 되는 것.
            long tick = (long)(1000 / Speed); 
            _nextMoveTick = Environment.TickCount64 + tick;

            Vector2Int destPos = GetFrontCellPosFromCurrDir();
            if (Room.Map.IsCanGo(destPos))
            {
                CellPos = destPos;
                S_Move movePkt = new S_Move();
                movePkt.ObjectId = Id;
                movePkt.PosInfo = PosInfo;
                Room.BroadcastToAllPlayer(movePkt);
                Console.WriteLine("Move Arrow!");
            }
            else
            {
                GameObject target = Room.Map.GetGameObjectFromSpecifiedPositionOrNull(destPos);
                if (target != null)
                {
                    target.OnDamaged(this, OwnerData.damage + Owner.StatInfo.Attack);
                }

                // 소멸
                Room.Push(Room.LeaveGame, Id);
                Console.WriteLine("LeaveRoom Arrow!");
            }

        }
        public override GameObject GetOwner()
        {
            return Owner;
        }
    }
}
