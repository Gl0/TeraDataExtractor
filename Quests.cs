using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TeraDataExtractor
{
    public class Quests
    {
        private string _region;

        private readonly Dictionary<int, Zone> _zones = new Dictionary<int, Zone>();
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "quests");

        public Quests(string region)
        {
            Directory.CreateDirectory(OutFolder);
            _region = region;
            Battlegrounds();
            Items();
        }

        public void Battlegrounds()
        {
            var xml = XDocument.Load(RootFolder + _region + "/BattleFieldData.xml");
            var xml1 = XDocument.Load(RootFolder + _region + "/StrSheet_BattleField.xml");
            var battleList = (from item in xml.Root.Elements("BattleField")
                join str in xml1.Root.Elements("String") on item.Attribute("name").Value equals str.Attribute("id").Value
                let id = item.Attribute("id").Value
                let name = str.Attribute("string").Value
                select new {id, name}).ToList();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"battle-{_region}.tsv"));
            foreach (var line in battleList)
            {
                outputTFile.WriteLine($"{line.id}\t{line.name}");
            }
            outputTFile.Close();
        }

        public void Items()
        {
            var xml = XDocument.Load(RootFolder + _region + "/ItemData/ItemData-0.xml");
            var xml1 = XDocument.Load(RootFolder + _region + "/StrSheet_Item/StrSheet_Item-0.xml");
            var itemList = (from item in xml.Root.Elements("Item")
                              join str in xml1.Root.Elements("String") on item.Attribute("id").Value equals str.Attribute("id").Value
                              let id = item.Attribute("id").Value
                              let name = str.Attribute("string").Value
                              let category= item.Attribute("category")?.Value??""
                              where category=="fiber"|| category == "metal" || category == "alchemy"
                              select new { id, name }).ToList();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"items-{_region}.tsv"));
            foreach (var line in itemList)
            {
                outputTFile.WriteLine($"{line.id}\t{line.name}");
            }
            outputTFile.Close();
        }

    }
}