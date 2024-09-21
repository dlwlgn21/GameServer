using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Data
{

    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        public static Dictionary<int, StatInfo> StatMap { get; private set; } = new Dictionary<int, StatInfo>();
        public static Dictionary<int, Data.Skill> SkillMap { get; private set; } = new Dictionary<int, Data.Skill>();

        public static void LoadData()
        {
            StatMap = LoadJson<Data.StatData, int, StatInfo>("StatData").MakeDict();
            SkillMap = LoadJson<Data.SkillData, int, Data.Skill>("SkillData").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string fileName) where Loader : ILoader<Key, Value>
        {
           
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{fileName}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
