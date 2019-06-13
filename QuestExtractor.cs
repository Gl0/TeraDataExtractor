using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Extracts quests names
    /// Structure: id, name
    /// </summary>
    public class QuestExtractor
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "quests");

        public QuestExtractor(string region, DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Children("StrSheet_Quest").Select(x => x.FirstChild("String"))
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id > 9999
                select new { id, name }).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"quests-{region}.tsv"), strings.OrderBy(x => x.id).Select(x => x.id.ToString() + "\t" + x.name));
        }
    }
}
