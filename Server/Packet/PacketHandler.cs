using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game.Object;
using Server.Game.Room;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
	// 이쪽은 RedZone!! 여러 Thread에서 동시다발적으로 실행될 수 있기 때문
	public static void C_MoveHandler(PacketSession session, IMessage packet)
	{

		// 클라쪽에서 나 이동할거야! 하고 패킷을 보냈으니,
		// 서버쪽에서는 모든 플레이어가 이녀석 이동했어! 하고 브로드캐스팅 해주어야 한다.
		C_Move movePkt = (C_Move)packet;
		ClientSession clientSession = (ClientSession)session;

        // 멀티쓰레드 환경에서 안전하게 작업할 수 있게, 따로 받아온다. 다른곳에서 null 대입 했어도 여기에서 하나 들고있으니 괜찮다.
        Player player = clientSession.OwnerPlayer; 
        if (player == null)
			return;
		
		// 이곳도 마찬가지. 다른 쓰레드에서 null 대입 해줄 수 있기 때문에 따로 받아야 한다.
		GameRoom room = player.Room;
		if (room == null)
			return;


		room.Push(room.HandleMovePkt ,player, movePkt);
	}

    public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
        C_Skill skillPkt = (C_Skill)packet;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.OwnerPlayer;
        if (player == null)
            return;
        GameRoom room = player.Room;
        if (room == null)
            return;


        room.Push(room.HandleSkillPkt, player, skillPkt);

    }
}
