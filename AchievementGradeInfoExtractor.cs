using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Just a grade to string conversion, could even hardcode them in TCC
    /// Structure: id, name
    /// </summary>
    public class AchievementGradeInfoExtractor
    {
        private readonly string _region;
        private string OutFolder = Path.Combine(Program.OutputPath, "achi_grade");

        public AchievementGradeInfoExtractor(string region,DataCenter dc)
        {
            _region = region;
            if (region.Contains("C")) return;
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Child("StrSheet_AchievementGradeInfo").Children("String")
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id != 0 && id < 106 && id > 100
                select new { id, name }).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"achi_grade-{_region}.tsv"), strings.OrderBy(x => x.id).Select(x => x.id.ToString() + "\t" + x.name));
        }
    }
}
