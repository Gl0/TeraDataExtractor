using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Text;

namespace TeraDataExtractor
{
    public class DotExtractor
    {
        private readonly string _region;
        private const string RootFolder = "j:/c/Extract/";
        private List<HotDot> Dotlist;

        public DotExtractor(string region)
        {
            _region = region;
            Dotlist = new List<HotDot>();
            RawExtract();
            var outputFile = new StreamWriter("DATA/hotdot-" + _region + ".tsv");
            foreach (HotDot line in Dotlist)
            {
                outputFile.WriteLine(line.ToString());
            }
            outputFile.Flush();
            outputFile.Close();
        }

        private void RawExtract()
        {

            var Dots = "".Select(t => new { abnormalid = string.Empty, type = string.Empty, amount = string.Empty, method = string.Empty, time = string.Empty, tick = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/Abnormality/"))
            {
                var xml = XDocument.Load(file);
                var Dotdata = (from item in xml.Root.Elements("Abnormal")
                               let abnormalid = item.Attribute("id").Value
                               let time = item.Attribute("infinity").Value=="True"?"0":item.Attribute("time").Value
                               from eff in item.Elements("AbnormalityEffect")
                               let type = eff.Attribute("type") == null ? "0" : eff.Attribute("type").Value
                               let method = eff.Attribute("method") == null ? "0" : eff.Attribute("method").Value
                               let amount = eff.Attribute("value") == null ? "0" : eff.Attribute("value").Value
                               let tick = eff.Attribute("tickInterval") == null ? "0" : eff.Attribute("tickInterval").Value
                               where (((type == "51"||type=="52") && tick != "0")||type == "4") && amount != "0" && amount != "0.0" && method != "0"
                               select new { abnormalid, type, amount, method, time, tick }).ToList();
                Dots = Dots.Union(Dotdata).ToList();
            }
            var Names = "".Select(t => new { abnormalid = string.Empty, name = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_Abnormality/"))
            {
                var xml = XDocument.Load(file);
                var Namedata = (from item in xml.Root.Elements("String")
                                let abnormalid = item.Attribute("id").Value
                                let name = item.Attribute("name")==null?"": item.Attribute("name").Value
                                where abnormalid != ""
                                select new { abnormalid, name }).ToList();
                Names = Names.Union(Namedata, (x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            }
            Dotlist = (from dot in Dots
                       join nam in Names on dot.abnormalid equals nam.abnormalid
                       select new HotDot(int.Parse(dot.abnormalid), dot.type, double.Parse(dot.amount, CultureInfo.InvariantCulture), dot.method, int.Parse(dot.time), int.Parse(dot.tick), nam.name)).ToList();
        }
    }
}
