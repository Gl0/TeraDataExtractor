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
        private string OutFolder = Path.Combine(Program.OutputPath, "hotdot");
        private List<HotDot> Dotlist = new List<HotDot>();
        private string[] _glyph = new string[]{ "Glyph", "Символ", "の紋章", "문장", "紋章" };

        public DotExtractor(string region)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            RawExtract();
            var outputFile = new StreamWriter(Path.Combine(OutFolder, $"hotdot-{_region}.tsv"));
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
            var interesting = new string[] { "3", "4", "6", "19", "22", "24", "104" , "162" , "203" , "210", "208" , "283" };
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/Abnormality/"))
            {
                var xml = XDocument.Load(file);
                var Dotdata = (from item in xml.Root.Elements("Abnormal")
                               let abnormalid = item.Attribute("id").Value
                               let isShow = item.Attribute("isShow") == null ? "False" : item.Attribute("isShow").Value
                               let time = item.Attribute("infinity").Value=="True"?"0":item.Attribute("time").Value
                               from eff in item.Elements("AbnormalityEffect")
                               let type = eff.Attribute("type") == null ? "0" : eff.Attribute("type").Value
                               let method = eff.Attribute("method") == null ? "" : eff.Attribute("method").Value
                               let amount = eff.Attribute("value") == null ? "" : eff.Attribute("value").Value
                               let tick = eff.Attribute("tickInterval") == null ? "0" : eff.Attribute("tickInterval").Value
                               where (((type == "51"||type=="52") && tick != "0")|| (interesting.Contains(type)&& isShow !="False")) && amount != "" && method != ""
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

            xml1 = XDocument.Load(RootFolder + _region + "/StrSheet_Crest.xml");
            var Glyphs = (from item in xml1.Root.Elements("String")
                         let passiveid = item.Attribute("id").Value
                         let name = item.Attribute("name").Value
                         let skillname = item.Attribute("skillName").Value
                          select new { passiveid, name, skillname});
            //dont parse CrestData.xml for passiveid<=>crestid, since they are identical now
            var Passives = "".Select(t => new { abnormalid = string.Empty, name = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/Passivity/"))
            {
                var xml = XDocument.Load(file);
                var PassiveData = (from item in xml.Root.Elements("Passive")
                                   let abnormalid = item.Attribute("value").Value
                                   let passiveid = item.Attribute("id").Value
                                   let type = item.Attribute("type").Value
                                   join glyph in Glyphs on passiveid equals glyph.passiveid
                                   where abnormalid != "" && (type =="209" || type == "210" || type=="156" || type == "157" || type == "80" || type == "232" || type == "106")
                                   && abnormalid != "500100"
                                   select new { abnormalid, name=(isGlyph(glyph.name))?$"{glyph.skillname}({glyph.name})": glyph.name }).ToList();
                Passives = Passives.Union(PassiveData, (x, y) => (x.abnormalid == y.abnormalid), x => x.abnormalid.GetHashCode()).ToList();
            }

            Dotlist = (from dot in Dots
                       join nam in Names on dot.abnormalid equals nam.abnormalid
                       join skills in ChainSkills on dot.abnormalid equals skills.abid into pskills
                       join glyph in Passives on dot.abnormalid equals glyph.abnormalid into gskills
                       from pskill in pskills.DefaultIfEmpty() 
                       from gskill in gskills.DefaultIfEmpty()

                       where (nam.name != "" || pskill != null || gskill!=null)
                       orderby int.Parse(dot.abnormalid),int.Parse(dot.type)
                       select new HotDot(int.Parse(dot.abnormalid), dot.type, double.Parse(dot.amount, CultureInfo.InvariantCulture), dot.method, int.Parse(dot.time), int.Parse(dot.tick), gskill==null?nam.name:gskill.name, pskill==null?"":pskill.skillid, pskill == null ? "" : pskill.PClass)).ToList();
        }

        private bool isGlyph(string name)
        {
            foreach (string gl in _glyph)
            {
                if (name.Contains(gl)) return true;
            }
            return false;
        }
    }
}
