﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace TeraDataExtractor
{
    public class DotExtractor
    {
        private readonly string _region;
        private string OutFolder = Path.Combine(Program.OutputPath, "hotdot");
        private List<HotDot> Dotlist = new List<HotDot>();
        private string[] _glyph = new string[]{ "Glyph", "Символ", "の紋章", "문장", "紋章" };
        public struct formatter { public string abnormalid; public string index; public string tick; public string change; public string time; };

        public DotExtractor(string region)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            RawExtract();
            //AddCharms(); //no more charms
            var outputFile = new StreamWriter(Path.Combine(OutFolder, $"hotdot-{_region}.tsv"));
            foreach (HotDot line in Dotlist)
            {
                outputFile.WriteLine(line.ToString());
                Program.Copytexture(line.IconName,line.AbnormalId);
            }
            outputFile.Flush();
            outputFile.Close();
        }

        private void AddCharms()
        {
            var xml = XDocument.Load(Program.SourcePath + _region + "/StrSheet_Charm.xml");
            var xml1 = XDocument.Load(Program.SourcePath + _region + "/CharmIconData.xml");
            var charmList = (from item in xml.Root.Elements("String")
                             join icon in xml1.Root.Elements("Icon") on item.Attribute("id").Value equals icon.Attribute("charmId").Value
                             let id = item.Attribute("id").Value
                             let name = item.Attribute("string").Value
                             let tooltip = item.Attribute("tooltip").Value
                             let iconName = icon.Attribute("iconName").Value
                             select new HotDot(int.Parse(id), "Charm", 0, "0", 0, 0, name, "", "",tooltip, iconName, "Buff", true, true, iconName)).ToList();
            Dotlist = Dotlist.Union(charmList).ToList();
        }
        private void RawExtract()
        {
            var Missing = "".Select(t => new { abnormalid = string.Empty, isShow = string.Empty, num = 0, redirected = string.Empty }).ToList();
            var Dots = "".Select(t => new { abnormalid = string.Empty, type = string.Empty, amount = string.Empty, method = string.Empty, time = string.Empty, tick = string.Empty, num = 0, property=string.Empty, isBuff=string.Empty, isShow=string.Empty }).ToList();
            var interesting = new string[] {"1", "3", "4", "5", "6", "7", "8", "9", "18", "19","36", "20", "22", "24", "25", "27", "28", "30", "103", "104", "105", "108", "162", "167", "168", "203", "207", "210", "208", "221", "227", "229", "231", "235", "236", "237" , "249", "255","260", "283", "316" };
            var notinteresting = new string[] {"8", "9", "18", "20", "27", "28", "103", "105", "108", "168", "221", "227" };
            var redirects_to_ignore = new string[] { "161", "182", "252", "264"};
            var redirects_to_follow = new string[] { "64", "161", "182", "223", "248", "252", "264", "271" };
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/Abnormality/"))
            {
                var xml = XDocument.Load(file);
                var Dotdata = (from item in xml.Root.Elements("Abnormal")
                               let abnormalid = item.Attribute("id").Value
                               let isShow = item.Attribute("isShow") == null ? "False" : item.Attribute("isShow").Value
                               let property = item.Attribute("property") == null ? "0" : item.Attribute("property").Value
                               let isBuff = item.Attribute("isBuff") == null ? "True" : item.Attribute("isBuff").Value
                               let time = item.Attribute("infinity").Value == "True" ? "0" : item.Attribute("time").Value
                               from eff in item.Elements("AbnormalityEffect")
                               let type = eff.Attribute("type") == null ? "0" : eff.Attribute("type").Value
                               let method = eff.Attribute("method") == null ? "" : eff.Attribute("method").Value
                               let num = item.Elements("AbnormalityEffect").TakeWhile(x => x != eff).Count() + 1
                               let amount = eff.Attribute("value") == null ? "" : eff.Attribute("value").Value
                               let tick = eff.Attribute("tickInterval") == null ? "0" : eff.Attribute("tickInterval").Value
                               where (((type == "51" || type == "52") && tick != "0") || (!redirects_to_ignore.Contains(type) && (isShow != "False" || abnormalid=="201" || abnormalid == "202" || abnormalid== "10152050"))) && amount != "" && method != "" 
                               select new { abnormalid, type, amount, method, time, tick, num, property, isBuff, isShow }).ToList();                                  //// 201 202 - marked as not shown, but needed
                Dots = Dots.Union(Dotdata).ToList();
                var parser = (from item in xml.Root.Elements("Abnormal")
                              let abnormalid = item.Attribute("id").Value
                              let isShow = item.Attribute("isShow") == null ? "False" : item.Attribute("isShow").Value
                              from eff in item.Elements("AbnormalityEffect")
                              let type = eff.Attribute("type") == null ? "0" : eff.Attribute("type").Value
                              let num = item.Elements("AbnormalityEffect").TakeWhile(x => x != eff).Count() + 1
                              let redirected = eff.Attribute("value") == null ? "" : eff.Attribute("value").Value
                              where redirects_to_follow.Contains(type)
                              select new { abnormalid, isShow, num, redirected }).ToList();
                for (var i = 1; i <= 4; i++) //5 lvl of redirection
                {
                    parser = (from item in parser join item1 in parser on item.redirected equals item1.abnormalid into wrapped
                              from wrap in wrapped.DefaultIfEmpty()
                              select new { item.abnormalid, item.isShow, item.num, redirected = wrap == null ? item.redirected : wrap.redirected }).ToList();
                    parser = parser.Distinct((x, y) => x.abnormalid == y.abnormalid && x.redirected == y.redirected, x => x.abnormalid.GetHashCode()).ToList();//don't eat all RAM in redirect loops
                }
                parser = parser.Where(x => x.isShow != "False").ToList();
                Dotdata = (from item in xml.Root.Elements("Abnormal")
                               join item1 in parser on item.Attribute("id").Value equals item1.redirected into parsed
                               from par in parsed
                               let abnormalid = par.abnormalid
                               let isShow = par.isShow
                               let isBuff = item.Attribute("isBuff") == null ? "True" : item.Attribute("isBuff").Value
                               let time = item.Attribute("infinity").Value == "True" ? "0" : item.Attribute("time").Value
                               let property = item.Attribute("property") == null ? "0" : item.Attribute("property").Value
                               from eff in item.Elements("AbnormalityEffect")
                               let type = eff.Attribute("type") == null ? "0" : eff.Attribute("type").Value
                               let method = eff.Attribute("method") == null ? "" : eff.Attribute("method").Value
                               let num = par.num
                               let amount = eff.Attribute("value") == null ? "" : eff.Attribute("value").Value
                               let tick = eff.Attribute("tickInterval") == null ? "0" : eff.Attribute("tickInterval").Value
                               where (((type == "51" || type == "52") && tick != "0") || ((isShow != "False" || abnormalid == "201" || abnormalid == "202"))) && amount != "" && method != ""
                               select new { abnormalid, type, amount, method, time, tick, num, property,isBuff,isShow }).ToList();
                Dots = Dots.Union(Dotdata).ToList();
                parser = parser.Distinct((x, y) => x.redirected.Equals(y.redirected), x => x.redirected.GetHashCode()).ToList();
                Missing = Missing.Union(parser).ToList();
            }
            Dots =Dots.Distinct((x, y) => x.abnormalid.Equals(y.abnormalid) && x.num == y.num, x=> (x.abnormalid+"l"+x.num).GetHashCode()).ToList();
            var subs = (from dot in Dots
                        let index = "value" + (dot.num == 1 ? "" : dot.num.ToString())
                        let change = ((dot.method == "3" || dot.method == "4" || dot.method == "0") && dot.type!="227" && dot.type != "228")
                            ? (dot.type == "51" || dot.type == "52")
                                ? Math.Abs(Math.Round(double.Parse(dot.amount, CultureInfo.InvariantCulture) * 100,2)).ToString("r",CultureInfo.InvariantCulture) + "%"
                                : Math.Abs(Math.Round((1 - double.Parse(dot.amount, CultureInfo.InvariantCulture)) * 100,2)).ToString("r",CultureInfo.InvariantCulture) + "%"
                            : dot.amount
                        select new formatter { abnormalid = dot.abnormalid, index = index, tick = dot.tick, change= change, time=dot.time }).ToDictionary(x => Tuple.Create(x.abnormalid, x.index));

            Dots = Dots.Distinct((x, y) => x.abnormalid.Equals(y.abnormalid) && x.type == y.type, x => (x.abnormalid + "l" + x.type).GetHashCode()).ToList();
            var Names = "".Select(t => new { abnormalid = string.Empty, name = string.Empty, tooltip=string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/StrSheet_Abnormality/"))
            {
                var xml = XDocument.Load(file);
                var Namedata = (from item in xml.Root.Elements("String")
                                let abnormalid = item.Attribute("id").Value
                                let name = item.Attribute("name") == null ? "" : item.Attribute("name").Value
                                let tooltip = item.Attribute("tooltip") == null ? "" : SubValues(item.Attribute("tooltip").Value
//                                    .Replace("$H_W_GOOD","").Replace("H_W_GOOD", "").Replace("$COLOR_END","").Replace("$H_W_BAD","").Replace("$H_W_Bad","").Replace("H_W_BAD","").Replace("$BR"," ").Replace("<br>", " ")
                                    .Replace("\n","$BR").Replace("\r", "$BR "), abnormalid,subs)
                                where abnormalid != "" && name!="" && !name.Contains("BTS") && !tooltip.Contains("BTS")
                                select new { abnormalid, name, tooltip }).ToList();
                Names = Names.Union(Namedata, (x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            }
            Missing.ForEach(x =>
            {
                var found = Names.FirstOrDefault(z => x.abnormalid == z.abnormalid);
                if (found!=null)
                {
                    if(!Names.Any(z=>z.abnormalid==x.redirected))
                        Names.Add(new {abnormalid=x.redirected, name=found.name, tooltip=found.tooltip});
                }
            });

            var Icons = "".Select(t => new { abnormalid = string.Empty, iconName = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/AbnormalityIconData/"))
            {
                var xml = XDocument.Load(file);
                var IconData = (from item in xml.Root.Elements("Icon")
                                let abnormalid = item.Attribute("abnormalityId").Value
                                let iconName = item.Attribute("iconName") == null ? "" : item.Attribute("iconName").Value
                                where abnormalid != "" && iconName!=""
                                select new { abnormalid, iconName }).ToList();
                Icons = Icons.Union(IconData, (x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            }
            Missing.ForEach(x =>
            {
                var found = Icons.FirstOrDefault(z => x.abnormalid == z.abnormalid);
                if (found != null)
                {
                    if (!Icons.Any(z => z.abnormalid == x.redirected))
                        Icons.Add(new { abnormalid = x.redirected, iconName = found.iconName});
                }
            });

            var SkillToName = "".Select(t => new { skillid = string.Empty, nameid = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/ItemData/"))
            {
                var xml = XDocument.Load(file);
                var itemdata = (from item in xml.Root.Elements("Item") let comb = (item.Attribute("category") == null) ? "no" : item.Attribute("category").Value let skillid = (item.Attribute("linkSkillId") == null) ? "0" : item.Attribute("linkSkillId").Value let nameid = item.Attribute("id").Value where ((comb == "combat") || (comb == "brooch") || (comb == "charm") || (comb == "magical")) && skillid != "0" && skillid != "" && nameid != "" select new { skillid, nameid });
                // filter only combat items, we don't need box openings etc.
                SkillToName.AddRange(itemdata);
            }
            var ItemNames = "".Select(t => new { nameid = string.Empty, name = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/StrSheet_Item/"))
            {
                var xml = XDocument.Load(file);
                var namedata = (from item in xml.Root.Elements("String") let nameid = item.Attribute("id").Value let name = item.Attribute("string").Value where nameid != "" && name != "" && name != "[TBU]" && name != "TBU_new_in_V24" select new { nameid, name }).ToList();
                ItemNames.AddRange(namedata);
            }
            var Items = (from item in SkillToName join nam in ItemNames on item.nameid equals nam.nameid orderby item.skillid where nam.name != ""
                         select new { item.skillid, item.nameid, nam.name }).ToList();

            var ItemSkills = "".Select(t => new { skillid = string.Empty, abid=string.Empty, template = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/SkillData/"))
            {
                var xml = XDocument.Load(file);
                if (xml.Root.Attribute("huntingZoneId")?.Value != "0") continue;
                var itemdata = (from skills in xml.Root.Elements("Skill")
                                 let template = skills.Attribute("templateId").Value 
                                 let skillid = skills.Attribute("id").Value
                                 from abns in skills.Descendants().Where(x=> x.Name=="AbnormalityOnPvp"||x.Name == "AbnormalityOnCommon")
                                 let abid = abns.Attribute("id")?.Value??""
                                 where template != "" && 
                                    skillid != "" && abid != ""
                                 select new { skillid, abid, template});
                ItemSkills = ItemSkills.Union(itemdata).ToList();
            }
            var ItemAbnormals = (from skills in ItemSkills
                                  join names in Items on skills.skillid equals names.skillid
                                  orderby int.Parse(names.nameid)
                                  select new { abid = skills.abid, nameid = names.nameid, names.name }).ToList();
            ItemAbnormals=ItemAbnormals.Distinct((x,y)=>x.abid==y.abid,x=>x.abid.GetHashCode()).ToList();

            List<Skill> skilllist;
            new SkillExtractor(_region, out skilllist);
            if (_region!="KR") skilllist.Where(x=>x.Name.Contains(":")).ToList().ForEach(x=>x.Name=x.Name.Replace(":",""));
            var xml1 = XDocument.Load(Program.SourcePath + _region + "/LobbyShape.xml");
            var templates = (from races in xml1.Root.Elements("SelectRace") let race = races.Attribute("race").Value.Cap() let gender = races.Attribute("gender").Value.Cap() from temp in races.Elements("SelectClass") let PClass = SkillExtractor.ClassConv(temp.Attribute("class").Value) let templateId = temp.Attribute("templateId").Value where temp.Attribute("available").Value == "True" select new { race, gender, PClass, templateId });
            //assume skills for different races and genders are the same per class 
            templates = templates.Distinct((x, y) => x.PClass == y.PClass, x => x.PClass.GetHashCode()).ToList();
            var directSKills = (from iskills in ItemSkills
                                join temp in templates on iskills.template equals temp.templateId
                                join names in skilllist on new { temp.PClass, iskills.skillid } equals new { names.PClass, skillid=names.Id }
                                where iskills.abid != "902" //noctineum, bugged skills abnormals
                                orderby int.Parse(iskills.abid)
                                select new { abnormalid = iskills.abid, name = names.Name, iconName = names.IconName }).ToList();
            var Passives = "".Select(t => new { abnormalid = string.Empty, name = string.Empty, iconName = string.Empty }).ToList();

            foreach (var x1 in directSKills.GroupBy(x=>x.abnormalid))
            {
                Passives.Add(new { abnormalid = x1.Key, name = x1.Count()>1?SkillExtractor.RemoveLvl(x1.First().name): x1.First().name, iconName = x1.First().iconName });
            }

            xml1 = XDocument.Load(Program.SourcePath + _region + "/StrSheet_Crest.xml");
            var xml2 = XDocument.Load(Program.SourcePath + _region + "/CrestData.xml");
            var Glyphs = (from item in xml1.Root.Elements("String") join crestItem in xml2.Root.Elements("CrestItem") on item.Attribute("id").Value equals crestItem.Attribute("id").Value
                          let passiveid = item.Attribute("id").Value
                          let name = item.Attribute("name").Value
                          let pclass = SkillExtractor.ClassConv(crestItem.Attribute("class").Value)
                          let skillname = item.Attribute("skillName").Value
                          let searchname = _region=="RU"? skillname.Replace("Всплеск ярости", "Сила гиганта").Replace("Разряд бумеранга", "Возвратный разряд").Replace("Фронтальная защита", "Сзывающий клич").Replace(":", "") : 
                                            _region !="KR"?skillname.Replace(":",""): skillname
                          let iconName1= skilllist.Find(x => x.Name.Contains(searchname) && x.PClass==pclass)?.IconName ?? ""
                          let skillId1= skilllist.Find(x => x.Name.Contains(searchname) && x.PClass == pclass)?.Id ?? ""
                          let iconName = _region == "KR"||iconName1 !="" || !name.Contains(" ") ? iconName1 : skilllist.Find(x => x.Name.ToLowerInvariant().Contains(
                              _region == "EU-FR" ? name.ToLowerInvariant().Remove(name.LastIndexOf(' ')) : name.ToLowerInvariant().Substring(name.IndexOf(' ')+1)
                              ))?.IconName ?? ""
                          let skillId = _region == "KR"||skillId1 !="" || !name.Contains(" ") ? skillId1 : skilllist.Find(x => x.Name.ToLowerInvariant().Contains(
                              _region == "EU-FR" ? name.ToLowerInvariant().Remove(name.LastIndexOf(' ')) : name.ToLowerInvariant().Substring(name.IndexOf(' ') + 1)
                              ))?.Id ?? ""
                          let tooltip = item.Attribute("tooltip").Value
                          select new { passiveid, name, skillname, skillId, iconName, tooltip});
            //dont parse CrestData.xml for passiveid<=>crestid, since they are identical now
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/Passivity/"))
            {
                var xml = XDocument.Load(file);
                var PassiveData = (from item in xml.Root.Elements("Passive")
                                   let abnormalid = item.Attribute("value").Value
                                   let passiveid = item.Attribute("id").Value
                                   let type = item.Attribute("type").Value
                                   join glyph in Glyphs on passiveid equals glyph.passiveid
                                   where abnormalid != "" && (type =="209" || type == "210" || type=="156" || type == "157" || type == "80" || type == "232" || type == "106")
                                   && abnormalid != "500100"
                                   select new { abnormalid, name=(isGlyph(glyph.name))?$"{glyph.skillname}({glyph.name})": glyph.name, glyph.iconName }).ToList();
                Passives = Passives.Union(PassiveData, (x, y) => (x.abnormalid == y.abnormalid), x => x.abnormalid.GetHashCode()).ToList();
            }

            Dotlist = (from dot in Dots
                       join nam in Names on dot.abnormalid equals nam.abnormalid into inames
                       join icon in Icons on dot.abnormalid equals icon.abnormalid into iicons
                       join skills in ItemAbnormals on dot.abnormalid equals skills.abid into iskills
                       join glyph in Passives on dot.abnormalid equals glyph.abnormalid into gskills
                       from iname in inames.DefaultIfEmpty()
                       from iicon in iicons.DefaultIfEmpty()
                       from iskill in iskills.DefaultIfEmpty() 
                       from gskill in gskills.DefaultIfEmpty()

                       where (iname!=null || iskill != null || gskill!=null)
                       orderby int.Parse(dot.abnormalid),int.Parse(dot.type)
                       select new HotDot(int.Parse(dot.abnormalid), dot.type, double.Parse(dot.amount, CultureInfo.InvariantCulture), dot.method, long.Parse(dot.time), int.Parse(dot.tick), gskill==null?iname?.name??iskill.name:gskill.name, iskill==null?"":iskill.nameid, iskill == null ? "" : iskill.name,iname?.tooltip??"",gskill==null?iicon?.iconName??"":gskill.iconName,dot.property, bool.Parse(dot.isBuff), dot.isShow!="False", iicon?.iconName ?? "")).ToList();

            var Crests = "".Select(t => new { passiveid = string.Empty, skillname=string.Empty, skillId=string.Empty, iconName = string.Empty, name = string.Empty, glyphIcon=string.Empty,tooltip=string.Empty}).ToList();
            xml1 = XDocument.Load(Program.SourcePath + _region + "/CrestIconData.xml");
            foreach (
                var file in
                    Directory.EnumerateFiles(Program.SourcePath + _region + "/Passivity/"))
            {
                var xml = XDocument.Load(file);
                var CrestsData = (from item in xml.Root.Elements("Passive")
                                   let value = double.Parse(item.Attribute("value")?.Value??"0", CultureInfo.InvariantCulture)
                                   let prob = double.Parse(item.Attribute("prob").Value??"0", CultureInfo.InvariantCulture) *100
                                   let type = item.Attribute("type").Value
                                   let passiveid = item.Attribute("id").Value
                                   join glyph in Glyphs on passiveid equals glyph.passiveid
                                   join icon in xml1.Root.Elements("Icon") on passiveid equals icon.Attribute("crestId").Value
                                   let glyphIcon = icon.Attribute("iconName").Value
                                   let tooltip = glyph.tooltip == null ? "" : glyph.tooltip.Replace("$BR", " ").Replace("\n", " ").Replace("$value",
                                   type=="72"?(-value).ToString():type=="171"||type=="26"||type== "175"? value.ToString():Math.Round(Math.Abs(value*100-100),2)+"%"
                                   ).Replace("$prob", prob+"%")
                                  select new { passiveid, glyph.skillname, glyph.skillId, glyph.iconName, glyph.name, glyphIcon, tooltip}).ToList();
                Crests = Crests.Union(CrestsData, (x, y) => (x.passiveid == y.passiveid), x => x.passiveid.GetHashCode()).ToList();
            }

            var outputFile = new StreamWriter(Path.Combine(OutFolder, $"glyph-{_region}.tsv"));
            foreach (var glyph in Crests)
            {
                outputFile.WriteLine(glyph.passiveid + "\t" + glyph.skillname + "\t" + glyph.skillId + "\t" + glyph.iconName.ToLowerInvariant() + "\t" +glyph.name + "\t" + glyph.glyphIcon.ToLowerInvariant() + "\t" + glyph.tooltip );
                Program.Copytexture(glyph.glyphIcon);
                Program.Copytexture(glyph.iconName);
            }
            outputFile.Flush();
            outputFile.Close();

        }

        public string SubValues(string text, string abid, Dictionary<Tuple<string,string>,formatter> subs)
        {
            string result = text;
            formatter changer;
            for (int i = 2; i <= 7; i++)
            {
                if (subs.TryGetValue(Tuple.Create(abid, "value"), out changer))
                    result = result.Replace("$time", long.Parse(changer.time) / 1000 + "s");
                if (subs.TryGetValue(Tuple.Create(abid, "value" + i), out changer))
                    result = result.Replace("$value" + i, changer.change).Replace("$tickInterval" + i, changer.tick).Replace("$time", long.Parse(changer.time) / 1000 + "s");
                else
                    if (subs.TryGetValue(Tuple.Create(abid, "value"), out changer))
                        result = result.Replace("$value" + i, changer.change+" (unk"+i+")").Replace("$tickInterval" + i, changer.tick);
                    else
                        result = result.Replace("$value" + i, "unk"+i).Replace("$tickInterval" + i, "unk"+i);
            }
            if (subs.TryGetValue(Tuple.Create(abid, "value"), out changer))
                result = result.Replace("$value", changer.change).Replace("$tickInterval", changer.tick + "s");
            return result;
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
