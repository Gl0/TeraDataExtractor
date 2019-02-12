using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TeraDataExtractor.data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Parses guild quests names
    /// Structure: id, name
    /// </summary>
    public class GuildQuestsExtractor //copied from the other tool coz too lazy
    {
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "guild_quests");
        
        private XDocument Data;
        private XDocument StrSheet;
        private XDocument BattleFieldData;
        private XDocument StrSheet_BattleField;
        private XDocument StrSheet_ZoneName;
        private string _region;
        private Dictionary<uint, GuildQuest> GuildQuests;

        private void Load(string region)
        {
            Data = XDocument.Load(Path.Combine(RootFolder, region,"GuildQuest.xml"));
            StrSheet = XDocument.Load(Path.Combine(RootFolder, region,"StrSheet_GuildQuest.xml"));
            BattleFieldData = XDocument.Load(Path.Combine(RootFolder, region,"BattleFieldData.xml"));
            StrSheet_BattleField = XDocument.Load(Path.Combine(RootFolder, region,"StrSheet_BattleField.xml"));
            StrSheet_ZoneName = XDocument.Load(Path.Combine(RootFolder, region,"StrSheet_ZoneName.xml"));
        }
        private void ParseDataDoc()
        {
            foreach (var questElement in Data.Descendants().Where(x => x.Name == "Quest"))
            {
                var id = uint.Parse(questElement.Attribute("id").Value);
                var type = id / 1000;
                if (type == 2)
                {
                    //bg
                    var stringId = uint.Parse(questElement.Attribute("title").Value.Replace("@GuildQuest:", ""));
                    uint battleFieldId = uint.Parse(questElement.Descendants().FirstOrDefault(x => x.Name == "Field").Attribute("battleFieldId").Value);
                    var bgNameId = BattleFieldData.Descendants().FirstOrDefault(x => x.Name == "BattleField" && x.Attribute("id").Value == battleFieldId.ToString()).Attribute("name").Value;
                    var bgName = StrSheet_BattleField.Descendants().FirstOrDefault(x => x.Name == "String" && x.Attribute("id").Value == bgNameId).Attribute("string").Value;
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
                    var stringId = uint.Parse(questElement.Attribute("title").Value.Replace("@GuildQuest:", ""));
                    uint zoneId = uint.Parse(questElement.Descendants().FirstOrDefault(x => x.Name == "Npc").Attribute("huntingZoneId").Value);
                    var zoneName = StrSheet_ZoneName.Descendants().FirstOrDefault(x => x.Name == "String" && x.Attribute("id").Value == zoneId.ToString()).Attribute("string").Value;
                    GuildQuests[stringId].Title = GuildQuests[stringId].Title.Replace(@"{HuntingZone1}", zoneName);
                }

            }
        }
        private void ParseStrSheetDoc()
        {
            foreach (var stringElement in StrSheet.Descendants().Where(x => x.Name == "String"))
            {
                var id = uint.Parse(stringElement.Attribute("id").Value);
                var str = stringElement.Attribute("string").Value;

                GuildQuests.Add(id, new GuildQuest(id, str));
            }
        }
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
        public GuildQuestsExtractor(string region)
        {
            _region = region;
            if (region.Contains("C")) return;
            GuildQuests = new Dictionary<uint, GuildQuest>();
            Load(region);
            ParseStrSheetDoc();
            ParseDataDoc();
            Dump();
        }
    }
}
