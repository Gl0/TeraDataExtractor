using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TeraDataExtractor
{
    /// <summary>
    /// Extracts region names (includes all items from DungeonExtractor?)
    /// Structrue: id, name
    /// </summary>
    public class RegionExtractor
    {
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "regions");

        public RegionExtractor(string region)
        {
            Directory.CreateDirectory(OutFolder);
            var lines = new List<string>();
            XDocument.Load(Path.Combine(RootFolder, region, "StrSheet_Region.xml")).
            Descendants().Where(x => x.Name == "String").ToList().ForEach(s =>
            {
                var id = s.Attribute("id").Value;
                var name = s.Attribute("string").Value;
                lines.Add(id + "\t" + name.Replace("\n", "&#xA;"));
            });
            File.WriteAllLines(Path.Combine(OutFolder, $"regions-{region}.tsv"), lines);

        }
    }
}
