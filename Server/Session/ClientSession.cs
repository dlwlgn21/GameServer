using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game.Room;
using Server.Game.Object;
using Server.Data;
using System.Diagnostics;

namespace Server
{
	public class ClientSession : PacketSession
	{
		public Player OwnerPlayer { get; set; }
		public int SessionId { get; set; }

		public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);

			MsgId eMsgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
			
            ushort size = (ushort)packet.CalculateSize();
            byte[] sendBuffer = new byte[size + 4];
            Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)eMsgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
            Send(new ArraySegment<byte>(sendBuffer));
        }
		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			// PROTO Test
			OwnerPlayer = ObjectManager.Instance.Add<Player>();
			{
				OwnerPlayer.Info.Name = $"Player_{OwnerPlayer.Info.ObjectId}";
				OwnerPlayer.Info.PosInfo.State = CharacterState.Idle;
				OwnerPlayer.Info.PosInfo.MoveDir = MoveDir.Right;
				OwnerPlayer.Info.PosInfo.PosX = 0;
				OwnerPlayer.Info.PosInfo.PosY = 0;
				StatInfo stat = null;
				if (DataManager.StatMap.TryGetValue(1, out stat) == false)
				{
					Debug.Assert(false);
					return;
				}
				OwnerPlayer.StatInfo.MergeFrom(stat);
				OwnerPlayer.Session = this;
            }
			GameRoom room = RoomManager.Instance.FindOrNUll(1);
            room.Push(room.EnterGame,OwnerPlayer);
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			GameRoom room = RoomManager.Instance.FindOrNUll(1);
			room.Push(room.LeaveGame, OwnerPlayer.Info.ObjectId);
			SessionManager.Instance.Remove(this);
			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
	}
}
