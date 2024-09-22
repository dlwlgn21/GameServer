using Server.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Utills
{
    public static class Extentions
    {
        public static bool SaveChangesEx(this GameDbContext db)
        {
            try
            {
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
