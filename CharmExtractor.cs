using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TeraDataExtractor
{
    class CharmExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "hotdot");

        public CharmExtractor(string region)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            var xml = XDocument.Load(RootFolder + _region + "/StrSheet_Charm.xml");
            var xml1 = XDocument.Load(RootFolder + _region + "/CharmIconData.xml");
            var charmList = (from item in xml.Root.Elements("String") join icon in xml1.Root.Elements("Icon") on item.Attribute("id").Value equals icon.Attribute("charmId").Value
                             let id = item.Attribute("id").Value
                             let name = item.Attribute("string").Value
                             let iconName = icon.Attribute("iconName").Value
                             select new { id, name , iconName }).ToList();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"charms-{_region}.tsv"));
            foreach (var line in charmList)
            {
                outputTFile.WriteLine($"{line.id}\t{line.name}\t{line.iconName.ToLowerInvariant()}");
                Program.Copytexture(line.iconName);
            }
            outputTFile.Close();
        }
    }
}
