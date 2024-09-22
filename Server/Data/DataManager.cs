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

        public static Dictionary<int, ItemData> ItemDataMap { get; private set; } = new Dictionary<int, ItemData>();
        public static Dictionary<int, MonsterData> MonsterDataMap { get; private set; } = new Dictionary<int, MonsterData>();
        public static void LoadData()
        {
            StatMap = LoadJson<Data.StatLoader, int, StatInfo>("StatData").MakeDict();
            SkillMap = LoadJson<Data.SkillLoader, int, Data.Skill>("SkillData").MakeDict();
            ItemDataMap = LoadJson<Data.ItemLoader, int, Data.ItemData>("ItemData").MakeDict();
            MonsterDataMap = LoadJson<Data.MonsterLoader, int, Data.MonsterData>("MonsterData").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string fileName) where Loader : ILoader<Key, Value>
        {
           
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{fileName}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
