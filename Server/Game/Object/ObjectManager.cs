using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Server.Game.Object
{
    public class ObjectManager
    {
        public static ObjectManager Instance { get; } = new ObjectManager();
        object _lock = new object();
        Dictionary<int, Player> _playerMap = new Dictionary<int, Player>();

        // [UNUSED(1)][TYPE(7)][ID(24)]
        int _counter = 1; // TODO

        public T Add<T>() where T : GameObject, new()
        {
            T gameObject = new T();

            lock (_lock)
            {
                gameObject.Id = GenerateId(gameObject.ObjectType);
                if (gameObject.ObjectType == GameObjectType.Player)
                {
                    _playerMap.Add(gameObject.Id, gameObject as Player);
                }
            }
            Debug.Assert(gameObject != null);
            return gameObject;
        }

        int GenerateId(GameObjectType eType)
        {
            lock (_lock)
            {
                return ((int)eType << 24 | (_counter++));
            }
        }

        public bool Remove(int objectId)
        {
            GameObjectType objectType = GetOjbectTypeById(objectId);

            lock (_lock)
            {
                if (objectType == GameObjectType.Player)
                    return _playerMap.Remove(objectId);
            }

            return false;
        }

        public static GameObjectType GetOjbectTypeById(int id)
        {
            int type = (id >> 24) & 0x7F;
            return (GameObjectType)type;
        }

        public Player FindOrNUll(int objectId)
        {
            GameObjectType objectType = GetOjbectTypeById(objectId);

            lock (_lock)
            {
                if (objectType == GameObjectType.Player)
                {
                    Player player = null;
                    if (_playerMap.TryGetValue(objectId, out player))
                        return player;
                }
            }
            return null;
        }
    }
}
