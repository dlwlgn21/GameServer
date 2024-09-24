using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server.Game.Object
{
    public class Monster : GameObject
    {
        public int TemplateId { get; private set; }
        Player _target;
        long _nextSearchTick = 0;
        long _nextMoveTick = 0;
        long _coolTick = 0;
        int _serachCellDist = 10;
        int _chaseCellDist = 20;
        int _skillRange = 1;
        public Monster()
        {
            ObjectType = GameObjectType.Monster;
        }

        public void Init(int templateId)
        {
            TemplateId = templateId;
            MonsterData monData;
            DataManager.MonsterDataMap.TryGetValue(TemplateId, out monData);
            Debug.Assert(monData != null);
            StatInfo.MergeFrom(monData.stat);
            StatInfo.Hp = monData.stat.MaxHp;
            ECurrState = CharacterState.Idle;
        }

        // FSM (Finite State Machine)
        public override void Update()
        {
            switch (ECurrState)
            {
                case CharacterState.Idle:
                    UpdateIdle();
                    break;
                case CharacterState.Move:
                    UpdateMove();
                    break;
                case CharacterState.Skill:
                    UpdateSkill();
                    break;
                case CharacterState.Die:
                    UpdateDie();
                    break;
                default:
                    break;
            }
        }

        /*
         * 매 1초마다 일정 범위내에 player가 있는지 체크해서 있으면 moveState로 바꿔주는 함수.
         * **/
        protected virtual void UpdateIdle()
        {
            if (_nextSearchTick > Environment.TickCount64)
                return;
            _nextSearchTick = Environment.TickCount64 + 1000;

            // 범위안에 있으면 그 범위내의 player 뱉어주겠지.
            Player target = Room.FindPlayerByConditionOrNull(p =>
            {
                Vector2Int dir = p.CellPos - CellPos;
                return dir.cellDistFromZeroPos < _serachCellDist;
            });
            if (target == null)
                return;
            _target = target;
            ECurrState = CharacterState.Move;
        }
        protected virtual void UpdateMove()
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;
            // Speed가 1초 동안 몇칸을 움직이냐?의 개념이었다.
            // 1초는 1000 밀리초고, 1000 밀리초를 Speed로 나눈 결과가 바로 moveTick이 된다.
            int moveTick = (int)(1000 / Speed);
            _nextMoveTick = Environment.TickCount64 + moveTick;

            if (_target == null || _target.Room != Room)
            {
                _target = null;
                ECurrState = CharacterState.Idle;
                BroadcastMove();
                return;
            }
            Vector2Int dir = _target.CellPos - CellPos;
            int dist = dir.cellDistFromZeroPos;
            
            // dist == 0 즉, 같은 위치에 있거나, target이 너무 멀리 있다면!
            if (dist == 0 || dist > _chaseCellDist)
            {
                _target = null;
                ECurrState = CharacterState.Idle;
                BroadcastMove();
                return;
            }

            List<Vector2Int> paths = Room.Map.FindPath(CellPos, _target.CellPos, isCheckObjectCollision: false);
            if (paths.Count < 2 || paths.Count > _chaseCellDist)
            {
                _target = null;
                ECurrState = CharacterState.Idle;
                BroadcastMove();
                return;
            }

            // 스킬로 넘어갈지 체크 (dir.x == 0 || dir.y == 0)는 대각선으로 스킬쓰는거 방지
            if (dist <= _skillRange && (dir.x == 0 || dir.y == 0))
            {
                _coolTick = 0;
                ECurrState = CharacterState.Skill;
                return;
            }


            // 실제 이동은 이곳에서 일어나지!!
            EMoveDir = GetEDirectionFromVector(paths[1] - CellPos);
            Room.Map.ApplyMove(this, paths[1]);
            BroadcastMove();
        }


        protected virtual void UpdateSkill()
        {
            // 바로 공격가능
            if (_coolTick == 0)
            {
                // 유효한 타겟인지
                if (_target == null || _target.Room != Room || _target.Hp <= 0)
                {
                    _target = null;
                    ECurrState = CharacterState.Move;
                    BroadcastMove();
                    return;
                }

                // 스킬이 아직 사용 가능한지
                Vector2Int dir = (_target.CellPos - CellPos);
                int dist = dir.cellDistFromZeroPos;
                bool isCanUseSkill = (dist <= _skillRange && (dir.x == 0 || dir.y == 0));
                if (!isCanUseSkill)
                {
                    ECurrState = CharacterState.Move;
                    BroadcastMove();
                    return;
                }

                // 타겟팅 방향 주시하도록 만들어주자!
                MoveDir lookDir = GetEDirectionFromVector(dir);
                if (EMoveDir != lookDir)
                {
                    EMoveDir = lookDir;
                    BroadcastMove();
                }

                Skill skillData = null;
                if (DataManager.SkillMap.TryGetValue((int)SkillType.SkillAuto, out skillData) == false)
                {
                    Debug.Assert(false);
                    return;
                }

                // 데미지 판정
                _target.OnDamaged(this, skillData.damage + TotalAttack);

                // 스킬 사용 Broadcast
                S_Skill skillPkt = new S_Skill() { Info = new SkillInfo() };
                skillPkt.ObjectId = Id;
                skillPkt.Info.SkillId = skillData.id;
                Room.BroadcastToAllPlayer(skillPkt);

                // 스킬 쿨타임 적용
                int coolTick = (int)(1000 * skillData.cooldown);
                _coolTick = Environment.TickCount64 + coolTick;
            }

            // 다음에 내가 스킬을 사용할 수 있는 시간까지 왔는지 아닌지
            if (_coolTick > Environment.TickCount64)
                return;
            _coolTick = 0;
        }
        protected virtual void UpdateDie()
        {

        }

        public override void OnDie(GameObject attacker)
        {
            base.OnDie(attacker);

            GameObject owner = attacker.GetOwner();
            if (owner.ObjectType == GameObjectType.Player)
            {
                RewardData rewardData = GetRewardOrNull();
                if (rewardData != null)
                {
                    Player player = (Player)owner;
                    DbTransaction.RewardToPlayer(player, rewardData, Room);
                }
            }
        }


        void BroadcastMove()
        {
            S_Move movePkt = new S_Move();
            movePkt.ObjectId = Id;
            movePkt.PosInfo = PosInfo;
            Room.BroadcastToAllPlayer(movePkt);
        }

        RewardData GetRewardOrNull()
        {
            MonsterData monData;
            DataManager.MonsterDataMap.TryGetValue(TemplateId, out monData);
            Debug.Assert(monData != null);

            int rand = new Random().Next(0, 101);
            int sum = 0;
            foreach (RewardData reward in monData.rewards)
            {
                sum += reward.probability;
                if (rand <= sum)
                {
                    return reward;
                }
            }

            return null;
        }
    }
}
