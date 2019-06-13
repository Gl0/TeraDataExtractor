using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Needed in TCC chat for "-TYPE- account." messages
    /// Structure: id, name
    /// </summary>
    public class AccountBenefitExtractor
    {
        private readonly string _region;
        private string OutFolder = Path.Combine(Program.OutputPath, "acc_benefits");
        private Dictionary<uint, string> _benefits;

        public AccountBenefitExtractor(string region, DataCenter dc)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Child("StrSheet_AccountBenefit").Children("String")
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id != 0
                select new { id , name }).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"acc_benefits-{_region}.tsv"), strings.OrderBy(x=>x.id).Select(x=>x.id.ToString()+"\t"+x.name));
        }
    }
}
