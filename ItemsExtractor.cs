using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    public class ItemsExtractor
    {
        private string _region;

        private readonly Dictionary<int, Zone> _zones = new Dictionary<int, Zone>();
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "items");

        public ItemsExtractor(string region, DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            _region = region;
            //Battlegrounds();
            //Items();
            //FullItems();
            TCCItems(dc);
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
                              let name = str.Attribute("string")?.Value??""
                              let category= item.Attribute("category")?.Value??""
                              where (category=="fiber"|| category == "metal" || category == "alchemy" || category == "")&& name!=""
                            select new { id, name }).ToList();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"items-{_region}.tsv"));
            foreach (var line in itemList)
            {
                outputTFile.WriteLine($"{line.id}\t{line.name}");
            }
            outputTFile.Close();
        }

        public void FullItems()
        {
            var strings = "".Select(t => new { id = string.Empty, name = string.Empty, toolTip = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_Item/"))
            {
                var xml = XDocument.Load(file);
                var stringlist= (from str in xml.Root.Elements("String")
                                 let id = str.Attribute("id").Value ?? ""
                                 let name = str.Attribute("string")?.Value ?? ""
                                 let toolTip = str.Attribute("toolTip")?.Value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ') ?? ""
                                 select new { id, name, toolTip }).ToList();
                strings = strings.Union(stringlist, (x, y) => x.id == y.id, x => x.id.GetHashCode()).ToList();

            }
            var items = "".Select(t => new { id = string.Empty, name = string.Empty, category = string.Empty, icon = string.Empty, requiredGender= string.Empty, requiredRace=string.Empty, toolTip = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/ItemData/"))
            {
                var xml = XDocument.Load(file);
                var itemList = (from item in xml.Root.Elements("Item")
                    join str in strings on item.Attribute("id").Value equals str.id
                    let id = item.Attribute("id").Value
                    let category = item.Attribute("category")?.Value ?? ""
                    let requiredGender = item.Attribute("requiredGender")?.Value ?? ""
                    let requiredRace = item.Attribute("requiredRace")?.Value ?? ""
                    let icon = item.Attribute("icon")?.Value ?? ""
                    let tradable = item.Attribute("tradable")?.Value ?? "False"
                    where tradable == "True"
                //                               where str.toolTip.Contains("$value")
                                                        //                            where (category == "fiber" || category == "metal" || category == "alchemy" || category == "") && name != ""
                                select new {id, str.name, category, icon, requiredGender, requiredRace, str.toolTip}).ToList();
                items = items.Union(itemList, (x,y) => x.id==y.id, x=>x.id.GetHashCode()).ToList();
            }
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"fullitems-{_region}.tsv"));
            foreach (var line in items)
            {
                outputTFile.WriteLine($"{line.id}\t{line.name}\t{line.category}\t{line.icon}\t{line.toolTip}\t{line.requiredGender}\t{line.requiredRace}");
            }
            outputTFile.Close();
        }

        private void TCCItems(DataCenter dc)
        {
            var strings = (from str in dc.Root.Children("StrSheet_Item").SelectMany(x=>x.Children("String"))
                                  let id = str["id", 0].ToInt32()
                                  let name = str["string",""].AsString.Replace('\n', ' ') ?? ""
                                  where name != "" && id != 0
                                  select new { id, name }).ToList();
            var items = (from item in dc.Root.Children("ItemData").SelectMany(x => x.Children("Item"))
                                join str in strings on item["id", 0].ToInt32() equals str.id
                                let id = item["id",0].ToInt32()
                                let rareGrade = item["rareGrade",0].ToInt32()
                                let linkEquipmentExpId = item["linkEquipmentExpId",0].ToInt32()
                                let coolTime = item["coolTime",0].ToInt32()
                                let icon = item["icon",""].AsString
                                select new { id, rareGrade, str.name, linkEquipmentExpId, coolTime, icon }).ToList();
            //File.WriteAllLines(Path.Combine(OutFolder, $"items-{_region}.tsv"), items.Select(x => $"{x.id}\t{x.rareGrade}\t{x.name}\t{x.linkEquipmentExpId}\t{x.coolTime}\t{x.icon.ToLowerInvariant()}"));
            File.WriteAllLines(Path.Combine(OutFolder, $"items-{_region}.tsv"), items.OrderBy(x=>x.id).Select(x => new StringBuilder().Append(x.id).Append("\t")
             .Append(x.rareGrade).Append("\t").Append(x.name).Append("\t").Append(x.linkEquipmentExpId).Append("\t").Append(x.coolTime).Append("\t").Append(x.icon.ToLowerInvariant()).ToString().Replace("\n", "&#xA;")));
        }
    }
}