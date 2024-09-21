﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.DB;
using Server.Game.Job;
using Server.Game.Room;
using ServerCore;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();
		static List<System.Timers.Timer> _timers = new List<System.Timers.Timer>();
		static void TickRoom(GameRoom room, int tickInMilleSec = 100)
		{
			var timer = new System.Timers.Timer();
			timer.Interval = tickInMilleSec;
			timer.Elapsed += ((s, e)=> { room.Update(); });
			timer.AutoReset = true;
			timer.Enabled = true;

			_timers.Add(timer);
        }

		static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();

			// DBTest
			using (var db = new GameDbContext())
			{
				db.Accounts.Add(new AccountDb() { AccountName = "TestAccount"});
				db.SaveChanges();
			}
			int a = 0;

			GameRoom room = RoomManager.Instance.Add(1);
			TickRoom(room, 50);



			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			while (true)
			{
				Thread.Sleep(100);
			}
		}
	}
}
