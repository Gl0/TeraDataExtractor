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
        private string OutFolder = Path.Combine(Program.OutputPath, "acc_benefits");

        public AccountBenefitExtractor(string region, DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            var strings = (from str in dc.Root.Child("StrSheet_AccountBenefit").Children("String")
                let id = str["id", 0].ToInt32()
                let name = str["string", ""].AsString.Replace("\n", "&#xA;") ?? ""
                where name != "" && id != 0
                select new { id , name }).Distinct((x,y)=>x.id==y.id,x=>x.id.GetHashCode()).ToList();

            File.WriteAllLines(Path.Combine(OutFolder, $"acc_benefits-{region}.tsv"), strings.OrderBy(x=>x.id).Select(x=>x.id.ToString()+"\t"+x.name));
        }
    }
}
