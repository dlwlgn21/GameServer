using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.DB;
using Server.Game.Object;
using Server.Game.Room;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PacketHandler
{

    public static void C_LoginHandler(PacketSession session, IMessage pkt)
    {
        ClientSession cliSession = (ClientSession)session;
        cliSession.HandleLogin((C_Login)pkt);
    }

    // 이쪽은 RedZone!! 여러 Thread에서 동시다발적으로 실행될 수 있기 때문
    public static void C_MoveHandler(PacketSession session, IMessage pkt)
	{

		// 클라쪽에서 나 이동할거야! 하고 패킷을 보냈으니,
		// 서버쪽에서는 모든 플레이어가 이녀석 이동했어! 하고 브로드캐스팅 해주어야 한다.
		C_Move movePkt = (C_Move)pkt;
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

    public static void C_SkillHandler(PacketSession session, IMessage pkt)
    {
        C_Skill skillPkt = (C_Skill)pkt;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.OwnerPlayer;
        if (player == null)
            return;
        GameRoom room = player.Room;
        if (room == null)
            return;


        room.Push(room.HandleSkillPkt, player, skillPkt);

    }


    public static void C_EnterGameHandler(PacketSession session, IMessage pkt)
    {
        ClientSession cliSession = (ClientSession)session;
        cliSession.HandleEnterGame((C_EnterGame)pkt);
    }


    public static void C_CreatePlayerHandler(PacketSession session, IMessage pkt)
    {
        ClientSession cliSession = (ClientSession)session;
        cliSession.HandleCreatePlayer((C_CreatePlayer)pkt);
    }

    public static void C_EquipItemHandler(PacketSession session, IMessage pkt)
    {
        C_EquipItem equipPkt = (C_EquipItem)pkt;
        ClientSession cliSession = (ClientSession)session;


        Player player = cliSession.OwnerPlayer;
        if (player == null)
            return;

        // 이곳도 마찬가지. 다른 쓰레드에서 null 대입 해줄 수 있기 때문에 따로 받아야 한다.
        GameRoom room = player.Room;
        if (room == null)
            return;


        room.Push(room.HadnleEqiupItem, player, equipPkt);
    }


}
