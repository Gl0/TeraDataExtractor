using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Extracts region names (includes all items from DungeonExtractor?)
    /// Structrue: id, name
    /// </summary>
    public class RegionExtractor
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "regions");

        public RegionExtractor(string region,DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Child("StrSheet_Region").Children("String")
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id != 0
                select new { id, name }).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"regions-{region}.tsv"), strings.OrderBy(x => x.id).Select(x => x.id.ToString() + "\t" + x.name));
        }
    }
}
