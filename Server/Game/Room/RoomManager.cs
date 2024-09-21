using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Game.Room
{
    public class RoomManager
    {
        public static RoomManager Instance { get; } = new RoomManager();
        object _lock = new object();
        Dictionary<int, GameRoom> _roomMap = new Dictionary<int, GameRoom>();

        int _roomId = 1;

        public GameRoom Add(int mapId)
        {
            GameRoom gameRoom = new GameRoom();
            gameRoom.Push(gameRoom.Init, mapId);

            lock (_lock)
            {
                gameRoom.RoomId = _roomId;
                _roomMap.Add(_roomId, gameRoom);
                ++_roomId;
            }

            return gameRoom;
        }
        public bool Remove(int roomId)
        {
            lock (_lock)
            {
                return _roomMap.Remove(roomId);
            }
        }
        public GameRoom FindOrNUll(int roomId)
        {
            lock (_lock)
            {
                GameRoom room = null;
                if (_roomMap.TryGetValue(roomId, out room))
                    return room;
                return null;
            }
        }
    }
}
