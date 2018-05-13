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
    /// Extracts all achievement names, could include actual achievement data if 
    /// achievement chat link is implemented in TCC
    /// Structure: id, name
    /// </summary>
    public class AchievementsExtractor
    {
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "achievements");
        public AchievementsExtractor(string region)
        {
            Directory.CreateDirectory(OutFolder);
            var lines = new List<string>();
            Directory.EnumerateFiles(Path.Combine(RootFolder, region, "StrSheet_Achievement")).ToList().ForEach(file =>
            {
                var xdoc = XDocument.Load(Path.Combine(RootFolder, region, "StrSheet_Achievement", file));
                if (xdoc.Descendants().Count() == 0) return;
                foreach (var item in xdoc.Descendants().Where(x => x.Name == "String"))
                {
                    var id = uint.Parse(item.Attribute("id").Value);
                    var name = item.Attribute("string").Value;

                    if(!string.IsNullOrEmpty(name)) lines.Add(id + "\t" + name.Replace("\n", "&#xA;"));
                }
            });
            File.WriteAllLines(Path.Combine(OutFolder, $"achievements-{region}.tsv"), lines);

        }

    }
}
