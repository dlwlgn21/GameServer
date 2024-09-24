using Google.Protobuf.Protocol;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server.Game.Object
{
    public class GameObject
    {
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;

        public int Id
        {
            get { return Info.ObjectId; }
            set { Info.ObjectId = value; }
        }
        public GameRoom Room { get; set; }
        public ObjectInfo Info { get; set; } = new ObjectInfo();

        public PositionInfo PosInfo { get; private set; } = new PositionInfo();
        public StatInfo StatInfo { get; private set; } = new StatInfo();

        public virtual int TotalAttack { get { return StatInfo.Attack; } }
        public virtual int TotalDefence { get { return 0; } }

        public float Speed
        {
            get { return StatInfo.Speed; }
            set { StatInfo.Speed = value; }
        }
        public int Hp
        {
            get { return StatInfo.Hp; }
            set { StatInfo.Hp = Math.Clamp(value, 0, StatInfo.MaxHp); }
        }

        public MoveDir EMoveDir
        {
            get { return PosInfo.MoveDir; }
            set { PosInfo.MoveDir = value; }
        }
        public CharacterState ECurrState
        {
            get { return PosInfo.State; }
            set { PosInfo.State = value; }
        }

        public GameObject()
        {
            Info.PosInfo = PosInfo;
            Info.StatInfo = StatInfo;
        }

        public virtual void Update()
        {

        }
        public Vector2Int CellPos
        {
            get
            {
                return new Vector2Int(PosInfo.PosX, PosInfo.PosY);
            }
            set
            {
                PosInfo.PosX = value.x;
                PosInfo.PosY = value.y;
            }
        }
        public Vector2Int GetFrontCellPosFromCurrDir()
        {
            return GetFrontCellPosPassedDir(PosInfo.MoveDir);
        }

        public static MoveDir GetEDirectionFromVector(Vector2Int dir)
        {
            if (dir.x > 0)
                return MoveDir.Right;
            else if (dir.x < 0)
                return MoveDir.Left;
            else if (dir.y > 0)
                return MoveDir.Up;
            else
                return MoveDir.Down;
        }
        public Vector2Int GetFrontCellPosPassedDir(MoveDir eDir)
        {
            Vector2Int currCellPos = CellPos;
            switch (eDir)
            {
                case MoveDir.Up:
                    currCellPos += Vector2Int.up;
                    break;
                case MoveDir.Down:
                    currCellPos += Vector2Int.down;
                    break;
                case MoveDir.Left:
                    currCellPos += Vector2Int.left;
                    break;
                case MoveDir.Right:
                    currCellPos += Vector2Int.right;
                    break;
                default:
                    break;
            }
            return currCellPos;
        }

        public virtual void OnDamaged(GameObject attacker, int damage)
        {
            if (Room == null)
            {
                Debug.Assert(false);
                return;
            }
            damage = Math.Max(damage - TotalDefence, 1);
            StatInfo.Hp = Math.Max(StatInfo.Hp - damage, 0);
            S_ChangeHp changeHpPket = new S_ChangeHp();
            changeHpPket.ObjectId = Id;
            changeHpPket.Hp = StatInfo.Hp;
            Room.BroadcastToAllPlayer(changeHpPket);

            if (StatInfo.Hp <= 0)
                OnDie(attacker);

        }

        public virtual void OnDie(GameObject attacker)
        {
            if (Room == null)
            {
                Debug.Assert(false);
                return;
            }
            S_Die diePkt = new S_Die();
            diePkt.ObjectId = Id;
            diePkt.AttackerId = attacker.Id;
            Room.BroadcastToAllPlayer(diePkt);

            GameRoom room = Room;
            room.LeaveGame(Id);

            StatInfo.Hp = StatInfo.MaxHp;
            PosInfo.State = CharacterState.Idle;
            PosInfo.MoveDir = MoveDir.Right;
            PosInfo.PosX = 0;
            PosInfo.PosY = 0;

            room.EnterGame(this);
        }

        public virtual GameObject GetOwner()
        {
            return this;
        }
    }
}
