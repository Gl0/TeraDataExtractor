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
            var interesting = new string[] { "3", "4", "6", "19", "22", "24","30", "104" , "162" , "203" , "207", "210", "208" , "283" };
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
            var Names = "".Select(t => new { abnormalid = string.Empty, name = string.Empty, tooltip=string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_Abnormality/"))
            {
                var xml = XDocument.Load(file);
                var Namedata = (from item in xml.Root.Elements("String")
                                let abnormalid = item.Attribute("id").Value
                                let name = item.Attribute("name") == null ? "" : item.Attribute("name").Value
                                let tooltip = item.Attribute("tooltip") == null ? "" : item.Attribute("tooltip").Value
                                where abnormalid != ""
                                select new { abnormalid, name, tooltip }).ToList();
                Names = Names.Union(Namedata, (x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            }
            var Icons = "".Select(t => new { abnormalid = string.Empty, iconName = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/AbnormalityIconData/"))
            {
                var xml = XDocument.Load(file);
                var IconData = (from item in xml.Root.Elements("Icon")
                                let abnormalid = item.Attribute("abnormalityId").Value
                                let iconName = item.Attribute("iconName") == null ? "" : item.Attribute("iconName").Value
                                where abnormalid != ""
                                select new { abnormalid, iconName }).ToList();
                Icons = Icons.Union(IconData, (x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            }

            var SkillToName = "".Select(t => new { skillid = string.Empty, nameid = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/ItemData/"))
            {
                var xml = XDocument.Load(file);
                var itemdata = (from item in xml.Root.Elements("Item") let comb = (item.Attribute("category") == null) ? "no" : item.Attribute("category").Value let skillid = (item.Attribute("linkSkillId") == null) ? "0" : item.Attribute("linkSkillId").Value let nameid = item.Attribute("id").Value where ((comb == "combat") || (comb == "brooch") || (comb == "charm") || (comb == "magical")) && skillid != "0" && skillid != "" && nameid != "" select new { skillid, nameid });
                // filter only combat items, we don't need box openings etc.
                SkillToName.AddRange(itemdata);
            }
            var ItemNames = "".Select(t => new { nameid = string.Empty, name = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_Item/"))
            {
                var xml = XDocument.Load(file);
                var namedata = (from item in xml.Root.Elements("String") let nameid = item.Attribute("id").Value let name = item.Attribute("string").Value where nameid != "" && name != "" && name != "[TBU]" && name != "TBU_new_in_V24" select new { nameid, name }).ToList();
                ItemNames.AddRange(namedata);
            }
            var Items = (from item in SkillToName join nam in ItemNames on item.nameid equals nam.nameid orderby item.skillid where nam.name != ""
                         select new { item.skillid, item.nameid, nam.name }).ToList();

            var ItemSkills = "".Select(t => new { skillid = string.Empty, abid=string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/SkillData/"))
            {
                var xml = XDocument.Load(file);
                var itemdata = (from skills in xml.Root.Elements("Skill")
                                 let template = skills.Attribute("templateId").Value 
                                 let skillid = skills.Attribute("id").Value
                                 from abns in skills.Descendants().Where(x=> x.Name=="AbnormalityOnPvp"||x.Name == "AbnormalityOnCommon")
                                 let abid = abns.Attribute("id")==null? "": abns.Attribute("id").Value
                                 where template == "9999" && skillid != "" && abid != ""
                                 select new { skillid, abid });
                ItemSkills = ItemSkills.Union(itemdata).ToList();
            }
            var ItemAbnormals = (from skills in ItemSkills
                                  join names in Items on skills.skillid equals names.skillid
                                  orderby int.Parse(names.nameid)
                                  select new { abid = skills.abid, nameid = names.nameid, names.name }).ToList();

            var xml1 = XDocument.Load(RootFolder + _region + "/StrSheet_Crest.xml");
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
                       join icon in Icons on dot.abnormalid equals icon.abnormalid
                       join skills in ItemAbnormals on dot.abnormalid equals skills.abid into iskills
                       join glyph in Passives on dot.abnormalid equals glyph.abnormalid into gskills
                       from iskill in iskills.DefaultIfEmpty() 
                       from gskill in gskills.DefaultIfEmpty()

                       where (nam.name != "" || iskill != null || gskill!=null)
                       orderby int.Parse(dot.abnormalid),int.Parse(dot.type)
                       select new HotDot(int.Parse(dot.abnormalid), dot.type, double.Parse(dot.amount, CultureInfo.InvariantCulture), dot.method, int.Parse(dot.time), int.Parse(dot.tick), gskill==null?nam.name:gskill.name, iskill==null?"":iskill.nameid, iskill == null ? "" : iskill.name,nam.tooltip,icon.iconName)).ToList();
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
