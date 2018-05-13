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
    /// Needed in TCC chat for "-TYPE- account." messages
    /// Structure: id, name
    /// </summary>
    public class AccountBenefitExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "acc_benefits");
        private Dictionary<uint, string> _benefits;

        public AccountBenefitExtractor(string region)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            _benefits = new Dictionary<uint, string>();
            var xdoc = XDocument.Load(Path.Combine(RootFolder,_region, "StrSheet_AccountBenefit.xml"));
            xdoc.Descendants().Where(x => x.Name == "String").ToList().ForEach(s =>
            {
                _benefits.Add(Convert.ToUInt32(s.Attribute("id").Value),
                               s.Attribute("string").Value);
            });

            var lines = new List<string>();
            _benefits.ToList().ForEach(b =>
            {
                if (!string.IsNullOrEmpty(b.Value))
                {
                    var line = b.Key + "\t" + b.Value.Replace("\n", "&#xA;");
                    lines.Add(line);
                }
            });
            File.WriteAllLines(Path.Combine(OutFolder, $"acc_benefits-{_region}.tsv"), lines);
        }
    }
}
