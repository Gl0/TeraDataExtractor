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
            var charmList = (from item in xml.Root.Elements("String")
                             let id = item.Attribute("id").Value
                             let name = item.Attribute("string").Value
                             select new { id, name }).ToList();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"charms-{_region}.tsv"));
            foreach (var line in charmList)
            {
                outputTFile.WriteLine($"{line.id}\t{line.name}");
            }
        }
    }
}
