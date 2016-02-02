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

            var xml1 = XDocument.Load(RootFolder + _region + "/LobbyShape.xml");
            var templates = (from races in xml1.Root.Elements("SelectRace") let race = races.Attribute("race").Value.Cap() let gender = races.Attribute("gender").Value.Cap() from temp in races.Elements("SelectClass") let PClass = SkillExtractor.ClassConv(temp.Attribute("class").Value) let templateId = temp.Attribute("templateId").Value where temp.Attribute("available").Value == "True" select new { race, gender, PClass, templateId });
            //assume skills for different races and genders are the same per class 
            templates = templates.Distinct((x, y) => x.PClass == y.PClass, x => x.PClass.GetHashCode()).ToList();
            var ChainSkills = "".Select(t => new { PClass = string.Empty, skillid = string.Empty, p_skill = new ParsedSkill(string.Empty, string.Empty, string.Empty),abid=string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/SkillData/"))
            {
                var xml = XDocument.Load(file);
                var chaindata = (from temp in templates
                                 join skills in xml.Root.Elements("Skill") on temp.templateId equals skills.Attribute("templateId").Value into Pskills
                                 from Pskill in Pskills
                                 let PClass = temp.PClass
                                 let skillid = Pskill.Attribute("id").Value
                                 let p_skill = new ParsedSkill(Pskill.Attribute("name").Value, skillid, (Pskill.Attribute("connectNextSkill") == null) ? Pskill.Attribute("type").Value : Pskill.Attribute("type").Value + "_combo")
                                 from abns in Pskill.Descendants().Where(x=> x.Name=="AbnormalityOnPvp"||x.Name == "AbnormalityOnCommon")
                                 let abid = abns.Attribute("id")==null? "": abns.Attribute("id").Value
                                 where PClass != "" && skillid != "" && abid != ""
                                 select new { PClass, skillid, p_skill,abid });
                ChainSkills = ChainSkills.Union(chaindata, (x, y) => (x.skillid == y.skillid) && (x.PClass == y.PClass)&&(x.abid==y.abid), x => (x.PClass + x.skillid + x.abid).GetHashCode()).ToList();
            }
            
            Dotlist = (from dot in Dots
                       join nam in Names on dot.abnormalid equals nam.abnormalid
                       join skills in ChainSkills on dot.abnormalid equals skills.abid into pskills
                       from pskill in pskills.DefaultIfEmpty()
                       select new HotDot(int.Parse(dot.abnormalid), dot.type, double.Parse(dot.amount, CultureInfo.InvariantCulture), dot.method, int.Parse(dot.time), int.Parse(dot.tick), nam.name, pskill==null?"":pskill.skillid, pskill == null ? "" : pskill.PClass)).ToList();
        }
    }
}
