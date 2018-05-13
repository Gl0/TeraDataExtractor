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
    /// Extracts quests names
    /// Structure: id, name
    /// </summary>
    public class QuestExtractor
    {
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "quests");

        public QuestExtractor(string region)
        {
            Directory.CreateDirectory(OutFolder);
            var lines = new List<string>();
            Directory.EnumerateFiles(Path.Combine(RootFolder, region, "StrSheet_Quest")).ToList().ForEach(file =>
            {
                if (file.EndsWith("-0.xml")) return;
                var xdoc = XDocument.Load(Path.Combine(RootFolder, region, "StrSheet_Quest", file));
                var item = xdoc.Descendants().Where(x => x.Name == "String").First();
                var id = uint.Parse(item.Attribute("id").Value);
                var name = item.Attribute("string").Value;

                if (!string.IsNullOrEmpty(name)) lines.Add(id + "\t" + name.Replace("\n", "&#xA;"));
            });
            File.WriteAllLines(Path.Combine(OutFolder, $"quests-{region}.tsv"), lines);


        }
    }
}
