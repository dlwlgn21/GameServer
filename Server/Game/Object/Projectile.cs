using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object
{
    public class Projectile : GameObject
    {
        public Data.Skill OwnerData { get; set; }
        public Projectile()
        {
            ObjectType = GameObjectType.Projectile;
        }
    }
}
