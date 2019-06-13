using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Extracts all achievement names, could include actual achievement data if 
    /// achievement chat link is implemented in TCC
    /// Structure: id, name
    /// </summary>
    public class AchievementsExtractor
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "achievements");
        public AchievementsExtractor(string region, DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Children("StrSheet_Achievement").SelectMany(x => x.Children("String"))
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id != 0
                select new { id, name }).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"achievements-{region}.tsv"), strings.OrderBy(x => x.id).Select(x => x.id.ToString() + "\t" + x.name));
        }

    }
}
