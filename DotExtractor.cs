﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    public class DotExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "hotdot");
        private List<HotDot> Dotlist = new List<HotDot>();
        private string[] _glyph = new string[]{ "Glyph", "Символ", "の紋章", "문장", "紋章" };
        public struct formatter { public int abnormalid; public int index; public string tick; public string change; public string time; };

        public DotExtractor(string region, DataCenter dc = null, List<Skill> skilllist=null, List<Template> templates=null)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            RawExtract(dc, skilllist, templates);
//            if (region.Contains("C")) AddCharms(); //charms on classic only
            var outputFile = new StreamWriter(Path.Combine(OutFolder, $"hotdot-{_region}.tsv"));
            foreach (HotDot line in Dotlist)
            {
                outputFile.WriteLine(line.ToString().Replace('\r',' ').Replace('\n',' '));
                Program.Copytexture(line.IconName, line.AbnormalId);
                Program.Copytexture(line.EffectIcon, line.AbnormalId);
            }
            outputFile.Flush();
            outputFile.Close();
        }

        //private void AddCharms()
        //{
        //    var xml = XDocument.Load(RootFolder + _region + "/StrSheet_Charm.xml");
        //    var xml1 = XDocument.Load(RootFolder + _region + "/CharmIconData.xml");
        //    var charmList = (from item in xml.Root.Elements("String")
        //                     join icon in xml1.Root.Elements("Icon") on item.Attribute("id").Value equals icon.Attribute("charmId").Value
        //                     let id = item.Attribute("id").Value
        //                     let name = item.Attribute("string").Value
        //                     let tooltip = item.Attribute("tooltip").Value
        //                     let iconName = icon.Attribute("iconName").Value
        //                     select new HotDot(int.Parse(id), "Charm", 0, "0", 0, 0, name, "", "",tooltip, iconName, "Buff", true, true, iconName)).ToList();
        //    Dotlist = Dotlist.Union(charmList).ToList();
        //}
        private void RawExtract(DataCenter dc, List<Skill>skilllist, List<Template> templates) {
            var abnormalityDC = dc.Root.Children("Abnormality").SelectMany(x => x.Children("Abnormal"));
            //var interesting = new string[] {"1", "3", "4", "5", "6", "7", "8", "9", "18", "19","36", "20", "22", "24", "25", "27", "28", "30", "103", "104", "105", "108", "162", "167", "168", "203", "207", "210", "208", "221", "227", "229", "231", "235", "236", "237" , "249", "255","260", "283", "316" };
            //var notinteresting = new string[] {"8", "9", "18", "20", "27", "28", "103", "105", "108", "168", "221", "227" };
            var redirects_to_ignore = new int[] { 161, 182, 252, 264};
            var redirects_to_follow = new int[] { 64, 161, 182, 223, 248, 252, 264, 271 };
            var Dots = (from item in abnormalityDC
                               let abnormalid = item["id",0].ToInt32()
                               let isShow = item["isShow","False"].AsString != "False"
                               let property = item["property",0].ToString()
                               let isBuff = item["isBuff",true].ToBoolean()
                               let time = item["infinity",false].ToBoolean() ? "0" : item["time","0"].AsString
                               from eff in item.Children("AbnormalityEffect").DefaultIfEmpty()
                               let type = eff?["type",0].ToInt32()??0
                               let method = eff?["method",0].ToInt32()??0
                               let num = item.Children("AbnormalityEffect").TakeWhile(x => x != eff).Count() + 1
                               let amount = eff?["value","0"].AsString??"0"
                               let tick = eff?["tickInterval", 0].ToString()??"0"
                               where (((type == 51 || type == 52) && tick != "0") || (!redirects_to_ignore.Contains(type)))// && amount != "" && method != -1
//                    where (((type == "51" || type == "52") && tick != "0") || (!redirects_to_ignore.Contains(type) && (isShow != "False" || abnormalid == "201" || abnormalid == "202" || abnormalid == "10152050"))) && amount != "" && method != ""
                               select new { abnormalid, type, amount = amount.Contains(',')?amount.Split(',').Last() : amount, method, time, tick, num, property, isBuff, isShow }).ToList();                                  //// 201 202 - marked as not shown, but needed
            var parser = (from item in abnormalityDC
                              let abnormalid = item["id",0].ToInt32()
                              let isShow = item["isShow", "False"].AsString != "False"
                              from eff in item.Children("AbnormalityEffect")
                              let type = eff["type", 0].ToInt32()
                              where redirects_to_follow.Contains(type)
                              let num = item.Children("AbnormalityEffect").TakeWhile(x => x != eff).Count() + 1
                              let redirected = eff["value", 0].ToInt32()
                              select new { abnormalid, isShow, num, redirected }).ToList();
            for (var i = 1; i <= 4; i++) //5 lvl of redirection
            {
                    parser = (from item in parser join item1 in parser on item.redirected equals item1.abnormalid into wrapped
                              from wrap in wrapped.DefaultIfEmpty()
                              select new { item.abnormalid, item.isShow, item.num, redirected = wrap == null ? item.redirected : wrap.redirected }).ToList();
                    parser = parser.Distinct((x, y) => x.abnormalid == y.abnormalid && x.redirected == y.redirected, x => x.abnormalid.GetHashCode()).ToList();//don't eat all RAM in redirect loops
            }
            parser = parser.Where(x => x.isShow ).ToList();
            var Dotdata = (from item in abnormalityDC
                               join item1 in parser on item["id",0].ToInt32() equals item1.redirected into parsed
                               from par in parsed
                               let abnormalid = par.abnormalid
                               let isShow = par.isShow
                               let isBuff = item["isBuff", true].ToBoolean()
                               let time = item["infinity", false].ToBoolean() ? "0" : item["time", "0"].AsString
                               let property = item["property", 0].ToString()
                               from eff in item.Children("AbnormalityEffect")
                               let type = eff["type", -1].ToInt32()
                               let method = eff["method", -1].ToInt32()
                               let num = par.num
                               let amount = eff["value", ""].AsString
                               let tick = eff["tickInterval", 0].ToString()
                               where (((type == 51 || type == 52) && tick != "0") || ((isShow || abnormalid == 201 || abnormalid == 202))) && amount != "" && method != -1
                               select new { abnormalid, type, amount, method, time, tick, num, property,isBuff,isShow }).ToList();
            Dots = Dots.Union(Dotdata).ToList();
            var Missing = parser.Distinct((x, y) => x.redirected.Equals(y.redirected), x => x.redirected.GetHashCode()).ToList();
            Dots = Dots.Distinct((x, y) => x.abnormalid.Equals(y.abnormalid) && x.num == y.num, x=> (x.abnormalid, x.num).GetHashCode()).ToList();
            var subs = (from dot in Dots
                        let change = ((dot.method == 3 || dot.method == 4 || dot.method == 0) && dot.type!=227 && dot.type != 228)
                            ? (dot.type == 51 || dot.type == 52)
                                ? Math.Abs(Math.Round(double.Parse(dot.amount, CultureInfo.InvariantCulture) * 100,2)).ToString("r",CultureInfo.InvariantCulture) + "%"
                                : Math.Abs(Math.Round((1 - double.Parse(dot.amount, CultureInfo.InvariantCulture)) * 100,2)).ToString("r",CultureInfo.InvariantCulture) + "%"
                            : dot.amount
                        select new formatter { abnormalid = dot.abnormalid, index = dot.num, tick = dot.tick, change= change, time=dot.time })
                .GroupBy(x=>x.abnormalid).ToDictionary(g => g.Key, g => g.ToDictionary(h=>h.index));
            Dots = Dots.Distinct((x, y) => x.abnormalid.Equals(y.abnormalid) && x.type == y.type, x => (x.abnormalid, x.type).GetHashCode()).ToList();

            var Names = (from item in dc.Root.Children("StrSheet_Abnormality").SelectMany(x=>x.Children("String"))
                            let abnormalid = item["id", 0].ToInt32()
                            let name = item["name",""].AsString
                            where abnormalid != 0 && name != "" && !name.Contains("BTS")
                            let tooltip = item["tooltip"].IsNull ? "" : SubValues(item["tooltip",""].AsString
                                //                                    .Replace("$H_W_GOOD","").Replace("H_W_GOOD", "").Replace("$COLOR_END","").Replace("$H_W_BAD","").Replace("$H_W_Bad","").Replace("H_W_BAD","").Replace("$BR"," ").Replace("<br>", " ")
                                .Replace("\n","$BR").Replace("\r", "$BR "), abnormalid, subs.GetValueOrDefault(abnormalid))
                            where !tooltip.Contains("BTS")
                            select new { abnormalid, name, tooltip }).Distinct((x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            Missing.ForEach(x =>
            {
                var found = Names.FirstOrDefault(z => x.abnormalid == z.abnormalid);
                if (found!=null)
                {
                    if(!Names.Any(z=>z.abnormalid==x.redirected))
                        Names.Add(new {abnormalid=x.redirected, name=found.name, tooltip=found.tooltip});
                }
            });

            var Icons = (from item in dc.Root.Children("AbnormalityIconData").SelectMany(x=>x.Children("Icon"))
                                let abnormalid = item["abnormalityId",0].ToInt32()
                                let iconName = item["iconName",""].AsString
                                where abnormalid != 0 && iconName!=""
                                select new { abnormalid, iconName }).Distinct((x, y) => x.abnormalid == y.abnormalid, x => x.abnormalid.GetHashCode()).ToList();
            Missing.ForEach(x =>
            {
                var found = Icons.FirstOrDefault(z => x.abnormalid == z.abnormalid);
                if (found != null)
                {
                    if (!Icons.Any(z => z.abnormalid == x.redirected))
                        Icons.Add(new { abnormalid = x.redirected, iconName = found.iconName});
                }
            });

            var SkillToName = (from item in dc.Root.Children("ItemData").SelectMany(x=>x.Children("Item"))
                let comb = item["category","no"].AsString
                let skillid = item["linkSkillId",0].ToInt32()
                let nameid = item["id",0].ToInt32()
                where ((comb == "combat") || (comb == "brooch") || (comb == "charm") || (comb == "magical")) && skillid != 0 && nameid != 0 select new { skillid, nameid });
                // filter only combat items, we don't need box openings etc.
            var ItemNames =  (from item in dc.Root.Children("StrSheet_Item").SelectMany(x=>x.Children("String"))
                    let nameid = item["id",0].ToInt32()
                    let name = item["string",""].AsString
                    where nameid != 0 && name != "" && name != "[TBU]" && name != "TBU_new_in_V24" select new { nameid, name }).ToList();
            var Items = (from item in SkillToName join nam in ItemNames on item.nameid equals nam.nameid orderby item.skillid where nam.name != ""
                         select new { item.skillid, item.nameid, nam.name }).ToList();

            string[] abnormalFilter = {"AbnormalityOnPvp", "AbnormalityOnCommon"};
            var ItemSkills  = (from skill in dc.Root.Children("SkillData").SelectMany(x => x.Children("Skill"))
                                let template = skill["templateId",0].ToInt32()
                                let skillid = skill["id",0].ToInt32()
                                where skill.Parent["huntingZoneId", 0].ToInt32() == 0 && template != 0 && skillid != 0 
                                from TargetingList in skill.Children("TargetingList")
                                from Targeting in TargetingList.Children("Targeting")
                                from AreaList in Targeting.Children("AreaList")
                                from Area in AreaList.Children("Area")
                                from Effect in Area.Children("Effect")
                                from Abnormal in Effect.Children(abnormalFilter)
                                let abid = Abnormal["id", 0].ToInt32()
                                where abid != 0
                               select new { skillid, abid, template}).ToList();
            var ItemAbnormals = (from skills in ItemSkills
                                  join names in Items on skills.skillid equals names.skillid
                                  orderby names.nameid
                                  select new { abid = skills.abid, nameid = names.nameid, names.name }).ToList();
            ItemAbnormals=ItemAbnormals.Distinct((x,y)=>x.abid==y.abid,x=>x.abid.GetHashCode()).ToList();

            if (_region != "KR") skilllist.Where(x=>x.Name.Contains(":")).ToList().ForEach(x=>x.Name=x.Name.Replace(":",""));
            var directSKills = (from iskills in ItemSkills
                                join temp in templates on iskills.template equals temp.templateId
                                join names in skilllist on new { temp.PClass, iskills.skillid } equals new { names.PClass, skillid=names.Id }
                                where iskills.abid != 902 //noctineum, bugged skills abnormals
                                orderby iskills.abid
                                select new { abnormalid = iskills.abid, name = names.Name, iconName = names.IconName }).ToList();

            var Passives = directSKills.GroupBy(x => x.abnormalid).Where(x => x.Key != 400500 && x.Key != 400501 && x.Key != 400508).Select(x1 => new {
                    abnormalid = x1.Key,
                    name = x1.Count() > 1 ? SkillExtractor.RemoveLvl(x1.First().name) : x1.First().name,
                    iconName = x1.First().iconName
                }).ToList(); 

            var Glyphs = (from item in dc.Root.FirstChild("StrSheet_Crest").Children("String")
                          join crestItem in dc.Root.FirstChild("CrestData").Children("CrestItem") on item["id",0].ToInt32() equals crestItem["id",0].ToInt32()
                          let passiveid = item["id",0].ToInt32()
                          let name = item["name",""].AsString
                          let pclass = SkillExtractor.ClassConv(crestItem["class",""].AsString)
                          let skillname = item["skillName",""].AsString
                          let searchname = _region=="RU"? skillname.Replace("Всплеск ярости", "Сила гиганта").Replace("Разряд бумеранга", "Возвратный разряд").Replace("Фронтальная защита", "Сзывающий клич").Replace(":", "") : 
                                            _region !="KR"?skillname.Replace(":",""): skillname
                          let iconName1= skilllist.Find(x => x.Name.Contains(searchname) && x.PClass==pclass)?.IconName ?? ""
                          let skillId1= skilllist.Find(x => x.Name.Contains(searchname) && x.PClass == pclass)?.Id ?? 0
                          let iconName = _region == "KR"||iconName1 !="" || !name.Contains(" ") ? iconName1 : skilllist.Find(x => x.Name.ToLowerInvariant().Contains(
                              _region == "EU-FR" ? name.ToLowerInvariant().Remove(name.LastIndexOf(' ')) : name.ToLowerInvariant().Substring(name.IndexOf(' ')+1)
                              ))?.IconName ?? ""
                          let skillId = _region == "KR"||skillId1 !=0 || !name.Contains(" ") ? skillId1 : skilllist.Find(x => x.Name.ToLowerInvariant().Contains(
                              _region == "EU-FR" ? name.ToLowerInvariant().Remove(name.LastIndexOf(' ')) : name.ToLowerInvariant().Substring(name.IndexOf(' ') + 1)
                              ))?.Id ?? 0
                          let tooltip = item["tooltip",""].AsString
                          select new { passiveid, name, skillname, skillId, iconName, tooltip});
            //dont parse CrestData.xml for passiveid<=>crestid, since they are identical now
            var PassiveData = (from item in dc.Root.Children("Passivity").SelectMany(x => x.Children("Passive"))
                               let passiveid = item["id", 0].ToInt32()
                               join glyph in Glyphs on passiveid equals glyph.passiveid
                               let type = item["type", 0].ToInt32()
                               let method = item["method", 0].ToInt32()
                               where (type ==209 || (type == 210 && method == 4) || type==156 || type == 157 || type == 80 || type == 232 || type == 106)
                               let abnormalid = item["value", 0].ToInt32()
                               where abnormalid != 0 && abnormalid != 500100
                               select new { abnormalid, name=(isGlyph(glyph.name))?$"{glyph.skillname}({glyph.name})": glyph.name, glyph.iconName }).ToList();
            Passives = Passives.Union(PassiveData, (x, y) => (x.abnormalid == y.abnormalid), x => x.abnormalid.GetHashCode()).ToList();

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
                       orderby dot.abnormalid, dot.type
                       select new HotDot(dot.abnormalid, dot.type, double.Parse(dot.amount, CultureInfo.InvariantCulture), dot.method, long.Parse(dot.time), (int)Math.Floor(double.Parse(dot.tick, CultureInfo.InvariantCulture)), gskill==null?iname?.name??iskill.name:gskill.name, iskill==null?"":iskill.nameid.ToString(), iskill == null ? "" : iskill.name,iname?.tooltip??"",gskill==null?iicon?.iconName??"":gskill.iconName,dot.property, dot.isBuff, dot.isShow, iicon?.iconName ?? "")).ToList();

            var Crests = (from item in dc.Root.Children("Passivity").SelectMany(x => x.Children("Passive"))
                                   let value = item["value",0f].ToSingle()
                                   let prob = item["prob",0f].ToSingle() * 100
                                   let type = item["type",0].ToInt32()
                                   let passiveid = item["id",0].ToInt32()
                                   join glyph in Glyphs on passiveid equals glyph.passiveid
                                   join icon in dc.Root.FirstChild("CrestIconData").Children("Icon") on passiveid equals icon["crestId",0].ToInt32()
                                   let glyphIcon = icon["iconName",""].AsString
                                   let tooltip = glyph.tooltip == null ? "" : glyph.tooltip.Replace("$BR", " ").Replace("\n", " ").Replace("$value",
                                   type==72?(-value).ToString():type==171||type==26||type== 175 ? value.ToString():Math.Round(Math.Abs(value*100-100),2)+"%"
                                   ).Replace("$prob", prob+"%")
                                  select new { passiveid, glyph.skillname, glyph.skillId, glyph.iconName, glyph.name, glyphIcon, tooltip})
                    .Distinct((x, y) => (x.passiveid == y.passiveid), x => x.passiveid.GetHashCode()).ToList();

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

        public string SubValues(string text, int abid, Dictionary<int, formatter> sub) {
            if (sub == null) return text;
            string result = text;
            formatter changer;
            for (int i = 2; i <= sub.Count; i++)
            {
                if (sub.TryGetValue(i, out changer))
                    result = result.Replace("$value" + i, changer.change).Replace("$tickInterval"+i, changer.tick+"s").Replace("$time", long.Parse(changer.time) / 1000 + "s");
                else
                if (sub.TryGetValue(1, out changer))
                    result = result.Replace("$value", changer.change + " (unk" + i + ")").Replace("$tickInterval", changer.tick+"s");
                else
                    result = result.Replace("$value" + i, "unk" + i).Replace("$tickInterval" + i + "s", "unk" + i);
            }
            if (sub.TryGetValue(1, out changer))
                result = result.Replace("$value", changer.change).Replace("$tickInterval", changer.tick + "s").Replace("$time", long.Parse(changer.time) / 1000 + "s"); ;
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
