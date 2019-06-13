using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alkahest.Core.Data;
using TeraDataExtractor.data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Parses guild quests names
    /// Structure: id, name
    /// </summary>
    public class GuildQuestsExtractor //copied from the other tool coz too lazy
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "guild_quests");
        private string _region;
        private Dictionary<int, GuildQuest> GuildQuests;
        private void Dump()
        {
            Directory.CreateDirectory(OutFolder);
            List<string> lines = new List<string>();
            foreach (var quest in GuildQuests)
            {
                if (quest.Key % 2 != 1) continue;
                if (quest.Value.Title.Contains("{")) continue;
                //if (quest.Value.ZoneId == 0) continue;
                var sb = new StringBuilder();
                sb.Append(quest.Value.Id);
                sb.Append('\t');
                sb.Append(quest.Value.Title);
                //sb.Append('\t');
                //sb.Append(quest.Value.ZoneId);
                lines.Add(sb.ToString().Replace("\n", "&#xA;"));
            }
            File.WriteAllLines(Path.Combine(OutFolder,$"guild_quests-{_region}.tsv"), lines);
        }
        public GuildQuestsExtractor(string region, DataCenter dc)
        {
            _region = region;
            if (region.Contains("C")) return;
            var battleFieldData = dc.Root.FirstChild("BattleFieldData");
            var strSheet_BattleField = dc.Root.FirstChild("StrSheet_BattleField");
            var strSheet_ZoneName = dc.Root.FirstChild("StrSheet_ZoneName");
            GuildQuests = (from str in dc.Root.Child("StrSheet_GuildQuest").Children("String")
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id != 0
                select new { id, name }).ToDictionary(x=>x.id,x=>new GuildQuest(x.id,x.name));

            foreach (var questElement in dc.Root.Child("GuildQuest").Children("Quest"))
            {
                var id = questElement["id",0].ToInt32();
                var type = id / 1000;
                if (type == 2)
                {
                    //bg
                    var stringId = int.Parse(questElement["title",""].AsString.Replace("@GuildQuest:", ""));
                    int battleFieldId = questElement.FirstChild("ClearCondition").FirstChild("TargetList").FirstChild("Field")?["battleFieldId",0].ToInt32()??0;
                    var bgNameId = battleFieldData.Children("BattleField").FirstOrDefault(x => x["id",-1].ToInt32() == battleFieldId)?["name",0].ToInt32()??0;
                    var bgName = strSheet_BattleField.Children("String").FirstOrDefault(x => x["id",-1].ToInt32() == bgNameId)?["string",""].AsString;
                    GuildQuests[stringId].Title = GuildQuests[stringId].Title.Replace(@"{BattleField1}", bgName);
                }
                else if (type == 3)
                {
                    //gather
                }
                else if (type == 4 || type == 5)
                {
                    //rally
                }
                else if (type >= 11)
                {
                    //dg
                    var stringId = int.Parse(questElement["title", ""].AsString.Replace("@GuildQuest:", ""));
                    int zoneId = questElement.FirstChild("ClearCondition").FirstChild("TargetList").FirstChild("Npc")?["huntingZoneId",0].ToInt32()??0;
                    var zoneName = strSheet_ZoneName.Children("String").FirstOrDefault(x => x["id",-1].ToInt32() == zoneId)?["string",""].AsString;
                    GuildQuests[stringId].Title = GuildQuests[stringId].Title.Replace(@"{HuntingZone1}", zoneName);
                }
            }
            Dump();
        }
    }
}
