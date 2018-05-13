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
    /// Extracts emote strings
    /// Structure: id, string
    /// </summary>
    public class SocialExtractor
    {
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "social");

        public SocialExtractor(string region)
        {
            Directory.CreateDirectory(OutFolder);
            var lines = new List<string>();
            XDocument.Load(Path.Combine(RootFolder, region, "StrSheet_Social.xml")).
            Descendants().Where(x => x.Name == "String").ToList().ForEach(s =>
            {
                var id = s.Attribute("id").Value;
                var name = s.Attribute("string").Value;
                if (!name.StartsWith("{")) return; //make this better if some change to original file ever happens
                lines.Add(id + "\t" + name.Replace("\n", "&#xA;"));
            });
            File.WriteAllLines(Path.Combine(OutFolder, $"social-{region}.tsv"), lines);

        }
    }
}
