using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraDataExtractor
{
    /// <summary>
    /// Just copy file as is. They should be the same for all regions, 
    /// but copy all anyway in case there are any region-specific changes in the future.
    /// </summary>
    public class EquipmentExpDataExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "equip_exp");
        public EquipmentExpDataExtractor(string region)
        {
            _region = region;
            if (region.Contains("C"))return;
            Directory.CreateDirectory(OutFolder);
            File.Copy(Path.Combine(RootFolder,_region, "EquipmentExpData.xml"),
                        Path.Combine(OutFolder, $"equip_exp-{_region}.xml"), true);
        }
    }
}
