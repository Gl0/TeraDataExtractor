using System.IO;
using System.Xml;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Just copy file as is. They should be the same for all regions, 
    /// but copy all anyway in case there are any region-specific changes in the future.
    /// </summary>
    public class EquipmentExpDataExtractor
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "equip_exp");
        public EquipmentExpDataExtractor(string region, DataCenter dc)
        {
            if (region.Contains("C")) return;
            Directory.CreateDirectory(OutFolder);
            using var writer = XmlWriter.Create(Path.Combine(OutFolder, $"equip_exp-{region}.xml"), new XmlWriterSettings{Indent = true});
            dc.Root.Child("EquipmentExpData").WriteElement(writer);
        }
    }
}
