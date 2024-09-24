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

        public void HandleMovePkt(Player player, C_Move movePkt)
        {
            if (player == null)
                return;

            // TODO : 클라의 data를 Validation 해주어야 함. 
            PositionInfo movePosInfo = movePkt.PosInfo;
            ObjectInfo info = player.Info;

            // 다른 좌표로 이동할 경우, 갈 수 있는지 체크
            if (movePosInfo.PosX != info.PosInfo.PosX ||
                movePosInfo.PosY != info.PosInfo.PosY)
            {
                if (Map.IsCanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                    return;
            }

            info.PosInfo.State = movePosInfo.State;
            info.PosInfo.MoveDir = movePosInfo.MoveDir;
            // 실제 Pos 값 대입은 ApplyMove 함수에서!
            Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

            // 다른 플레이어들한테도 뿌려준다.
            S_Move resMovePkt = new S_Move();
            resMovePkt.ObjectId = player.Info.ObjectId;
            resMovePkt.PosInfo = movePkt.PosInfo;

            BroadcastToAllPlayer(resMovePkt);
        }

        public void HandleSkillPkt(Player player, C_Skill skillPkt)
        {
            if (player == null)
                return;

            ObjectInfo info = player.Info;
            if (info.PosInfo.State != CharacterState.Idle)
                return;

            // TODO : 스킬 사용 가능여부 체크

            // 통과
            info.PosInfo.State = CharacterState.Skill;

            S_Skill sPkt = new S_Skill() { Info = new SkillInfo() };
            sPkt.ObjectId = player.Info.ObjectId;
            sPkt.Info.SkillId = skillPkt.Info.SkillId;
            BroadcastToAllPlayer(sPkt);
            Console.WriteLine($"Player{player.Info.ObjectId} Request Skill{skillPkt.Info.SkillId} Dir {player.Info.PosInfo.MoveDir}");

            Data.Skill skillData = null;
            if (DataManager.SkillMap.TryGetValue(skillPkt.Info.SkillId, out skillData) == false)
            {
                Debug.Assert(false);
                return;
            }
            switch (skillData.skillType)
            {
                case SkillType.SkillNone:
                    break;
                case SkillType.SkillAuto:
                    {
                        // TODO : 데미지 판정
                        Vector2Int skiillPos = player.GetFrontCellPosPassedDir(info.PosInfo.MoveDir);
                        GameObject target = Map.GetGameObjectFromSpecifiedPositionOrNull(skiillPos);
                        if (target != null)
                        {
                            Console.WriteLine($"{player.Info.ObjectId} is attack to GameObject {target.Info.ObjectId}");
                            target.OnDamaged(player, skillData.damage + player.TotalAttack);
                        }
                    }
                    break;
                case SkillType.SkillProjectile:
                    {
                        // TODO : Arrow
                        Arrow arrow = ObjectManager.Instance.Add<Arrow>();
                        Debug.Assert(arrow != null);
                        if (arrow == null)
                            return;

                        arrow.Owner = player;
                        arrow.OwnerData = skillData;
                        arrow.PosInfo.State = CharacterState.Move;
                        arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
                        arrow.PosInfo.PosX = player.PosInfo.PosX;
                        arrow.PosInfo.PosY = player.PosInfo.PosY;
                        arrow.Speed = skillData.projectileInfo.speed;
                        Console.WriteLine($"Arrow Speed : {arrow.Speed}");
                        Push(EnterGame, arrow);
                    }
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }
}
