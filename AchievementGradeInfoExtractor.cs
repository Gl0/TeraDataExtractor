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
    /// Just a grade to string conversion, could even hardcode them in TCC
    /// Structure: id, name
    /// </summary>
    public class AchievementGradeInfoExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "achi_grade");

        public AchievementGradeInfoExtractor(string region)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            var xdoc = XDocument.Load(Path.Combine(RootFolder, _region, "StrSheet_AchievementGradeInfo.xml"));

            var lines = new List<string>();
            foreach (var item in xdoc.Descendants().Where(x => x.Name == "String"))
            {
                var id = uint.Parse(item.Attribute("id").Value);
                var name = item.Attribute("string").Value;
                if (id > 105 || id < 101) continue;

                lines.Add(id + "\t" + name.Replace("\n", "&#xA;"));
            }
            File.WriteAllLines(Path.Combine(OutFolder, $"achi_grade-{_region}.tsv"), lines);
        }
    }
}
