using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TeraDataExtractor
{
    public class ItemsParser
    {

        private string _region;

        private readonly Dictionary<int, Zone> _zones = new Dictionary<int, Zone>();
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "items");


        public static Dictionary<uint, Item> Items;
        public static Dictionary<uint, List<Tuple<int, int>>> ExpData;
        private List<XDocument> StrSheet_ItemDocs;
        private List<XDocument> ItemDataDocs;
        public void Load(string region)
        {
            StrSheet_ItemDocs = new List<XDocument>();
            ItemDataDocs = new List<XDocument>();

            foreach (var f in Directory.EnumerateFiles(RootFolder + region + "/StrSheet_Item"))
            {
                var d = XDocument.Load(f);
                StrSheet_ItemDocs.Add(d);
            }

            foreach (var f in Directory.EnumerateFiles(RootFolder + region + "/ItemData"))
            {
                var d = XDocument.Load(f);
                ItemDataDocs.Add(d);
            }
        }
        void ParseDocs()
        {
            foreach (var item in ItemDataDocs)
            {
                ParseItemDataDoc(item);
            }
            foreach (var item in StrSheet_ItemDocs)
            {
                ParseStrSheetDoc(item);
            }
        }

        void ParseItemDataDoc(XDocument doc)
        {
            foreach (var item in doc.Descendants().Where(x => x.Name == "Item"))
            {
                //if (item.Attribute("obtainable").Value == "False") continue;
                var id = UInt32.Parse(item.Attribute("id").Value);
                var grade = UInt32.Parse(item.Attribute("rareGrade").Value);
                uint expId = 0;
                if (item.Attribute("linkEquipmentExpId") != null)
                {
                    expId = Convert.ToUInt32(item.Attribute("linkEquipmentExpId").Value);
                }
                uint cd = 0;
                if (item.Attribute("coolTime") != null)
                {
                    cd = Convert.ToUInt32(item.Attribute("coolTime").Value);
                }
                string icon = "";
                if (item.Attribute("icon") != null)
                {
                    icon = item.Attribute("icon").Value.ToLower();
                }
                //var bind = item.Attribute("boundType").Value;

                Items.Add(id, new Item(id, "", grade, expId, cd, icon));
            }
        }
        void ParseStrSheetDoc(XDocument doc)
        {
            foreach (var item in doc.Descendants().Where(x => x.Name == "String"))
            {
                var id = UInt32.Parse(item.Attribute("id").Value);
                var name = item.Attribute("string").Value;
                name = name.Replace("\n", "");
                try
                {
                    Items[id].Name = name;
                }
                catch (Exception)
                {

                    //Console.WriteLine("Skipped {0}", name);
                }
            }
        }
        void Dump(string region)
        {
            List<string> lines = new List<string>();

            foreach (var item in Items)
            {
                if (item.Value.Name == "" || item.Value.Id == 0) continue;

                //if (item.Value.Name.Contains("TBU")) continue;
                //if (item.Value.Name.Contains("PC Cafe")) continue;
                //if (item.Value.Name.Any(x => (ushort)x > 0xAC00 && (ushort)x < 0xD7A3)) continue;
                var sb = new StringBuilder();
                sb.Append(item.Value.Id);
                sb.Append('\t');
                sb.Append(item.Value.RareGrade);
                //sb.Append('\t');
                //sb.Append(item.Value.BoundType);
                sb.Append('\t');
                sb.Append(item.Value.Name);
                sb.Append('\t');
                sb.Append(item.Value.ExpId);
                sb.Append('\t');
                sb.Append(item.Value.CoolTime);
                sb.Append('\t');
                sb.Append(item.Value.Icon);

                lines.Add(sb.ToString());
            }

            File.WriteAllLines(Path.Combine(OutFolder,"items-" + region + ".tsv"), lines);
        }
        public ItemsParser(string region)
        {
            Items = new Dictionary<uint, Item>();
            Load(region);
            ParseDocs();
            Dump(region);
        }
    }

    public class Item
    {
        public uint Id;
        public string Name;
        public string BoundType;
        public uint RareGrade;
        public uint ExpId;
        public uint CoolTime;
        public string Icon;

        public Item(uint id, string name, uint g, uint expId = 0, uint cd = 0, string icon = "")
        {
            Id = id;
            Name = name;
            //BoundType = b;
            RareGrade = g;
            ExpId = expId;
            CoolTime = cd;
            Icon = icon;
        }
    }
}
