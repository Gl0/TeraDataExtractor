using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Extracts emote strings
    /// Structure: id, string
    /// </summary>
    public class SocialExtractor
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "social");

        public SocialExtractor(string region, DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Child("StrSheet_Social").Children("String")
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name.StartsWith("{") && id != 0
                select new { id, name }).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"social-{region}.tsv"), strings.OrderBy(x => x.id).Select(x => x.id.ToString() + "\t" + x.name));
        }


    }
}
