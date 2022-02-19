using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{

    /**
    WARNING: Pets are not reconized
    */
    public class Template {
        public string race { get; }
        public string gender { get; }
        public string PClass { get; }
        public int templateId { get; }

        public int tclass { get; }

        public Template(string race, string gender, string pClass, int templateId) {
            this.race = race;
            this.gender = gender;
            PClass = pClass;
            this.templateId = templateId;
            tclass = templateId % 100;
            if (tclass >= 9) tclass = tclass + 81;
        }
    }

    public class SkillExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "skills");
        private List<Skill> skilllist;
        private static Regex regex = new Regex("^[a-zA-Z0-9а-яА-Я\\%\\#\\'\\[\\]\\(\\)_\\:\\;\\.\\,\\- ]+$");

        public SkillExtractor(string region, DataCenter dc, out List<Skill> list, out List<Template> templates)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            skilllist = new List<Skill>();
            //if (region == "JP-C") RawExtractOld(dc);
            //else if (region.Contains("C")) RawExtractEUC(dc);
            //else
            RawExtract(dc);
            templates = chained_skills(dc);
            list = skilllist.ToList();
            item_Skills(dc);
            loadoverride();
            skilllist.Sort();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"skills-{_region}.tsv"));
            foreach (Skill line in skilllist)
            {
                outputTFile.WriteLine(line.toTSV());
                Program.Copytexture(line.IconName);
            }
            outputTFile.Flush();
            outputTFile.Close();
            VehicleSkills(dc);
        }

        private void VehicleSkills(DataCenter dc) {
            var mobNames = (from hunting in dc.Root.Children("StrSheet_Creature").SelectMany(x=>x.Children("HuntingZone"))
               let idzone = hunting["id",0].ToInt32() from entity in hunting.Children("String") let template = entity["templateId",0].ToInt32() let name = entity["name",""].AsString where name != "" && template != 0 && idzone != 0 select new { idzone, template, name }).ToList();
            var petskills = (from vs in dc.Root.FirstChild("StrSheet_VehicleSkill").Children("String")
                             let idzone = vs["huntingZoneId",0].ToInt32()
                             let template = vs["templateId",0].ToInt32()
                             let id = vs["id",0].ToInt32()
                             let name = vs["name",""].AsString
                             join mobName in mobNames on new { idzone, template } equals new { mobName.idzone, mobName.template } into vsms
                             from vsm in vsms.DefaultIfEmpty()
                             let petname = vsm?.name ?? ""
                             join si in dc.Root.FirstChild("VehicleSkillIconData").Children("Icon") on new { idzone, template, id } equals
                                                  new
                                                  {
                                                      idzone = si["huntingZoneId",0].ToInt32(),
                                                      template = si["templateId",0].ToInt32(),
                                                      id = si["skillId",0].ToInt32()
                                                  }
                             let icon = si["iconName",""].AsString
                             select new PetSkill(idzone, template, petname, id, name, icon)
                ).ToList();
            var ChainSkills = (from pet in petskills
                                 join skill in dc.Root.Children("SkillData").SelectMany(x => x.Children("Skill")) on new { hz = pet.HZ, template = pet.Template } equals new { hz = skill.Parent["huntingZoneId",0].ToInt32(), template = skill["templateId",0].ToInt32() }
                                 let parent = skill["id",0].ToInt32()
                                 let next = skill["nextSkill",0].ToInt32()
                                 from targetingList in skill.Children("TargetingList")
                                 from targeting in targetingList.Children("Targeting")
                                 from projectileSkillList in targeting.Children("ProjectileSkillList")
                                 from projectileSkill in projectileSkillList.Children("ProjectileSkill").DefaultIfEmpty()
                                 let id = projectileSkill?["id",0].ToInt32()??0
                                 where parent != 0 && (next != 0 || id != 0)
                                 select new { hz = pet.HZ, template = pet.Template, skillId = id == 0 ? next : id, parent }).ToList();
                            ChainSkills = ChainSkills.Distinct( (x, y) => x.hz == y.hz && x.template == y.template && x.skillId == y.skillId && x.parent == y.parent,
                                                            x => (x.hz, x.template,x.parent,x.skillId).GetHashCode()).ToList();
            ChainSkills.ForEach(x =>
            {
                var found = petskills.FirstOrDefault(z => x.hz == z.HZ && x.template == z.Template && x.parent == z.Id);
                if (found != null && !petskills.Any(z => x.hz == z.HZ && x.template == z.Template && x.skillId == z.Id))
                    petskills.Add(new PetSkill(x.hz, x.template, found.PetName, x.skillId, found.Name, found.IconName));
            });
            petskills.Sort();
            var outputTFile = new StreamWriter(Path.Combine(OutFolder, $"pets-skills-{_region}.tsv"));
            foreach (PetSkill line in petskills)
            {
                outputTFile.WriteLine(line.toTSV());
                Program.Copytexture(line.IconName);
            }
            outputTFile.Flush();
            outputTFile.Close();
        }


        private void loadoverride()
        {
            if (!File.Exists(Path.Combine(RootFolder,$"override/skills-override-{_region}.tsv")))
                return;
            var reader = new StreamReader(File.OpenRead(Path.Combine(RootFolder,$"override/skills-override-{_region}.tsv")));
            List<Skill> skilllist1 = new List<Skill>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split('\t');
                var id = int.Parse(values[0]);
                var race = values[1];
                var gender = values[2];
                var PClass = values[3];
                var skillName = values[4];
                var chained = values[5];
                var skillDetail = values[6];
                var skillIcon = values[7];
                var skill = new Skill(id, race, gender, PClass, skillName, chained, skillDetail, skillIcon);
                skilllist1.Add(skill);
            }
            skilllist=skilllist1.Union(skilllist).ToList();
        }

        private List<Template> chained_skills(DataCenter dc)
        {
            var templates = (from races in dc.Root.FirstChild("LobbyShape").Children("SelectRace")
                    let race = races["race",""].AsString.Cap() let gender = races["gender",""].AsString.Cap()
                from temp in races.Children("SelectClass")
                    let PClass = ClassConv(temp["class",""].AsString) let templateId = temp["templateId",0].ToInt32()
                    where temp["available",false].AsBoolean select new Template(race, gender, PClass, templateId))
                .Distinct((x, y) => x.PClass == y.PClass, x => x.PClass.GetHashCode()).ToList();
            //assume skills for different races and genders are the same per class 

            //not found Thrall & RP/turrel skills - need manual override files, same as for boss monsters, but localizable

            //1st way: parse chains based on "type" attribute
            //connect = connectNextSkill=id (multihit),<AddAbnormalityConnectSkill/AddConnectSkill redirectSkill=id>(first need to parse multihit to count then rest of added chains)
            //normal(connected from connect) = connectNextSkill=id (multihit)
            //dash = <Dash dashRedirectSkill=id/>
            //switch=on/off via nextSkill
            //combo =multihit via nextSkill
            //combo_instance=multihit via nextSkill
            //change= ?connectNextSkill=id (Explosion). <ConnectSkill redirectSkill=id>(immediate_cancel), <AddAbnormalityConnectSkill redirectSkill=id />(rearcancel)
            //movingSkill=lockon/groundplaced via nextSkill (sometime standalone)
            //movingCharge=<ChargeStage shotSkillId=id>
            //any type of skill can do <ProjectileSkill id=id>
            //drain=chargecancell via nextSkill+<Drain backSkillId=id>
            //counter= counterattack
            //evade = connectNextSkill=id evade multihit or nextSkill to movingcharge
            //pressHit = chained, connectNextSkil back to first hit

            //2nd way: parse internal skill name
            //name Race_Gender_Class_SkillLvl_InternalSkillName_modifiers (with "_" may be " ", modifiers may be absent)
            //internalSkillName can also contain "_" or " "
            //sometimes contains combo hitnumber (" 01" " 02" etc) at the end (type="combo_instance","combo") or useless " 01" - so we cuold not say whether it 1st hit in a row or standalone
            //sometimes hitnumber "01" "02" is not separated from skillname (gunner)
            // modifier (case insensitive) -> ParsedSkill.cs

            //trying 2nd way:
            //create dictionary of all modifiers and cut them from internalname to get relation between publicname and internalname from known skill names
            //then parse this modifiers to fill hit numbers and chained properties
            var ChainSkills =(from temp in templates
                                 join skills in dc.Root.Children("SkillData").Where(x => x["huntingZoneId", 0].ToInt32() == 0).SelectMany(x=>x.Children("Skill")) on temp.templateId equals skills["templateId",0].ToInt32() into Pskills
                                 from Pskill in Pskills
                                 let PClass = temp.PClass
                                 let skillid = Pskill["id",0].ToInt32()
                                 where PClass != "" && skillid != 0
                                 let cat = Pskill["category",""].AsString
                                 let p_skill = new ParsedSkill(Pskill["name",""].AsString, skillid, (Pskill["connectNextSkill",0].ToInt32() == 0) ? Pskill["type",""].AsString : Pskill["type",""].AsString + "_combo", cat, temp.tclass)
                                 select new { PClass, skillid, p_skill })
                .Distinct((x, y) => (x.skillid == y.skillid) && (x.PClass == y.PClass), x => (x.PClass, x.skillid).GetHashCode()).ToList();
            var IntToPub = (from cs in ChainSkills
                            join sl in skilllist on new { cs.PClass, cs.skillid } equals new { sl.PClass, skillid = sl.Id } into itps
                            from itp in itps
                            select new { cs.p_skill.BaseName, cs.p_skill.Category, itp.Name, itp.IconName }).ToList();
            IntToPub.Distinct((x, y) => x.BaseName == y.BaseName, x => x.BaseName.GetHashCode());
            var projectileSkills = (from temp in templates /// just for one missing projectile skillid...
                    join skills in dc.Root.Children("SkillData").Where(x => x["huntingZoneId", 0].ToInt32() == 0).SelectMany(x => x.Children("Skill")) on temp.templateId
                        equals skills["templateId", 0].ToInt32() into Pskills
                    from Pskill in Pskills
                    from ProjectileId in Pskill.Descendants("ProjectileSkill").Select(x => x["id", 0].ToInt32()).DefaultIfEmpty(0)
                    let PClass = temp.PClass
                    let skillid = Pskill["id", 0].ToInt32()
                    where PClass != "" && skillid != 0 && ProjectileId != 0
                    join sl in skilllist on new { PClass, skillid } equals new { sl.PClass, skillid = sl.Id } into ps
                    from ips in ps
                    select new { PClass, ProjectileId, Name=ips.Name, IconName=ips.IconName }
                ).Distinct((x, y) => x.PClass == y.PClass && x.ProjectileId == y.ProjectileId, x => (x.PClass, x.ProjectileId).GetHashCode()).ToList();
            var chainedlist = (from cs in ChainSkills
                               from itpc in IntToPub.Where(x => x.Category != 0 && x.Category == cs.p_skill.Category && x.BaseName != cs.p_skill.BaseName).DefaultIfEmpty(new { BaseName = "", Category = 0, Name = "", IconName = "" })
                               from itp in IntToPub.Where(x => x.BaseName == cs.p_skill.BaseName).DefaultIfEmpty(new { BaseName = "", Category = 0, Name = "", IconName = "" })
                               from proj in projectileSkills.Where(x=>x.PClass==cs.PClass && x.ProjectileId==cs.skillid).DefaultIfEmpty( new{PClass="",ProjectileId=0,Name="",IconName="" })
                               //join itp in IntToPub on cs.p_skill.BaseName equals itp.BaseName
                               join sl in skilllist on new { cs.skillid, cs.PClass } equals new { skillid = sl.Id, sl.PClass } into uskills
                               from uskill in uskills.DefaultIfEmpty(new Skill(0, "", "", "", ""))
                               where itp.Name != "" || itpc.Name!="" || uskill.Name != "" || proj.Name !=""
                               select new Skill(cs.skillid, "Common", "Common", cs.PClass, uskill.Name == "" ? ChangeLvl(itp.Name==""?itpc.Name==""?proj.Name:itpc.Name:itp.Name, cs.p_skill.Lvl) : uskill.Name, cs.p_skill.Chained, cs.p_skill.Detail, uskill.IconName == "" ? itp.IconName=="" ? itpc.IconName =="" ? proj.IconName : itpc.IconName : itp.IconName  : uskill.IconName)).ToList();
            skilllist = chainedlist.Union(skilllist).ToList();
            templates.Add(new Template("Common", "Common", "Common", 9999)); ///add summoning skills for equipment
            return templates;
        }

        private void item_Skills(DataCenter dc)
        {
            var ItemSkills = (from item in dc.Root.Children("ItemData").SelectMany(x => x.Children("Item"))
                    
                                let skillid = item["linkSkillId",0].ToInt32()
                                let nameid = item["id",0].ToInt32()
                                where skillid != 0 && nameid != 0
                                let comb = item["category", "no"].AsString
                                where ((comb == "combat") || (comb == "brooch") || (comb == "charm") || (comb == "magical"))
                                let itemicon = item["icon", ""].AsString
                                select new { skillid, nameid, itemicon });
                // filter only combat items, we don't need box openings etc.
            
            var Items = (from item in ItemSkills join 
                nam in dc.Root.Children("StrSheet_Item").SelectMany(x => x.Children("String")) on item.nameid equals nam["id",0].ToInt32() orderby item.skillid
                let name = nam["string",""].AsString
                where name != "" && name != "[TBU]" && name != "TBU_new_in_V24"
                select new Skill(item.skillid, "Common", "Common", "Common", name, item.itemicon)).ToList();
            if (_region == "RU") // shortest names only for RU region since other have non localized names or less descriptive names.
                Items.Sort((x, y) => CompareItems(x.Id, y.Id, x.Name, y.Name));
            Items = Items.Distinct((x, y) => x.Id == y.Id, x => x.Id.GetHashCode()).ToList();
            skilllist = Items.Union(skilllist).ToList();
        }

        private void RawExtract(DataCenter dc)
        {

            var skilldata = (from item in dc.Root.Children("StrSheet_UserSkill").SelectMany(x=>x.Children("String"))
                                 let id = item["id",0].ToInt32()
                                 let race = item["race",""].AsString
                                 let gender = item["gender",""].AsString
                                 let PClass = ClassConv(item["class", ""].AsString)
                                 let name = item["name",""].AsString
                                 where id != 0 && race != "" && gender != "" && name != "" && PClass != ""
                                 select new Skill(id, race, gender, PClass, name)).ToList();
            skilllist = skilllist.Union(skilldata).ToList();
            
            var icondata = (from item in dc.Root.Children("SkillIconData").SelectMany(x => x.Children("Icon"))
                                 let id = item["skillId",0].ToInt32()
                                 let race = item["race",""].AsString
                                 let gender = item["gender",""].AsString
                                 let PClass = ClassConv(item["class", ""].AsString)
                                 let iconName = item["iconName", ""].AsString
                                 where id != 0 && race != "" && gender != "" && PClass != ""
                                 select new Skill(id, race, gender, PClass, "", iconName)).ToList();
            skilllist = (from sl in skilllist
                         join ic in icondata on new { sl.Race, sl.Gender, sl.PClass, sl.Id } equals new { ic.Race, ic.Gender, ic.PClass, ic.Id } into skills
                         from skill in skills.DefaultIfEmpty()
                         select new Skill(sl.Id, sl.Race, sl.Gender, sl.PClass, sl.Name, skill?.IconName ?? "")).ToList();
        }


        //private void RawExtractOld()
        //{
        //    var xml = XDocument.Load(RootFolder + _region + "/LobbyShape.xml");
        //    var templates = (from races in xml.Root.Elements("SelectRace") let race = races.Attribute("race").Value.Cap() let gender = races.Attribute("gender").Value.Cap() from temp in races.Elements("SelectClass") let PClass = ClassConv(temp.Attribute("class").Value) let templateId = temp.Attribute("templateId").Value where temp.Attribute("available").Value == "True" select new Template(race, gender, PClass, templateId));
        //    templates = templates.Distinct((x, y) => x.PClass == y.PClass, x => x.PClass.GetHashCode()).ToList();

        //    foreach (
        //        var file in
        //        Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_UserSkill/"))
        //    {
        //        var xml1 = XDocument.Load(file);
        //        var skilldata = (from item in xml1.Root.Elements("String") join temp in templates on item.Attribute("templateId").Value equals temp.templateId
        //            let id = item.Attribute("id").Value
        //            let race = temp.race
        //            let gender = temp.gender
        //            let PClass = temp.PClass
        //            let name = (item.Attribute("name") == null) ? "" : item.Attribute("name").Value
        //            where id != "" && race != "" && gender != "" && name != "" && PClass != ""
        //            select new Skill(id, "Common", "Common", PClass, name)).ToList();
        //        skilllist = skilllist.Union(skilldata).ToList();
        //    }
        //    var icondata = new List<Skill>();
        //    foreach (
        //        var file in
        //        Directory.EnumerateFiles(RootFolder + _region + "/SkillIconData/"))
        //    {
        //        var xml1 = XDocument.Load(file);
        //        var skilldata = (from item in xml1.Root.Elements("Icon") join temp in templates on item.Attribute("templateId").Value equals temp.templateId
        //            let id = item.Attribute("skillId").Value
        //            let race = temp.race
        //            let gender = temp.gender
        //            let PClass = temp.PClass
        //            let iconName = (item.Attribute("iconName") == null) ? "" : item.Attribute("iconName").Value
        //            where id != "" && race != "" && gender != "" && PClass != ""
        //            select new Skill(id, "Common", "Common", PClass, "", iconName)).ToList();
        //        icondata = icondata.Union(skilldata).ToList();
        //    }
        //    skilllist = (from sl in skilllist
        //        join ic in icondata on new { sl.Race, sl.Gender, sl.PClass, sl.Id } equals new { ic.Race, ic.Gender, ic.PClass, ic.Id } into skills
        //        from skill in skills.DefaultIfEmpty()
        //        select new Skill(sl.Id, sl.Race, sl.Gender, sl.PClass, sl.Name, skill?.IconName ?? "")).ToList();
        //}

        //private void RawExtractEUC()
        //{
        //    var xml = XDocument.Load(RootFolder + _region + "/StrSheet_UserSkill.xml");
        //    skilllist = (from item in xml.Root.Elements("String")
        //        let id = item.Attribute("id").Value
        //        let race = item.Attribute("race").Value
        //        let gender = item.Attribute("gender").Value
        //        let PClass = (item.Attribute("class") == null) ? "" : ClassConv(item.Attribute("class").Value)
        //        let name = (item.Attribute("name") == null) ? "" : item.Attribute("name").Value
        //        where id != "" && race != "" && gender != "" && name != "" && PClass != ""
        //        select new Skill(id, race, gender, PClass, name)).ToList();
        //    var xml1 = XDocument.Load(RootFolder + _region + "/SkillIconData.xml");
        //    var icondata = (from item in xml1.Root.Elements("Icon")
        //        let id = item.Attribute("skillId").Value
        //        let race = item.Attribute("race").Value
        //        let gender = item.Attribute("gender").Value
        //        let PClass = (item.Attribute("class") == null) ? "" : ClassConv(item.Attribute("class").Value)
        //        let iconName = (item.Attribute("iconName") == null) ? "" : item.Attribute("iconName").Value
        //        where id != "" && race != "" && gender != "" && PClass != ""
        //        select new Skill(id, race, gender, PClass, "", iconName)).ToList();
        //    skilllist = (from sl in skilllist
        //                 join ic in icondata on new { sl.Race, sl.Gender, sl.PClass, sl.Id } equals new { ic.Race, ic.Gender, ic.PClass, ic.Id } into skills
        //                 from skill in skills.DefaultIfEmpty()
        //                 select new Skill(sl.Id, sl.Race, sl.Gender, sl.PClass, sl.Name, skill?.IconName ?? "")).ToList();
        //}


        private static int CompareItems(int idx, int idy, string x, string y)
        {
            if (idx == idy)
            {
                if (x == null)
                {
                    if (y == null) return 0;
                    else return -1;
                }
                else
                {
                    if (y == null)
                        return 1;
                    else
                    {
                        if (regex.Match(x).Value != x) { return 1; } ///Non RU & EN characters
                        if (regex.Match(y).Value != y) { return -1; } ///Non RU & EN characters
                        if (idx/1000 == 60246) { string t = x; x = y; y = t; } ///Greater charms
                        int retval = x.Length.CompareTo(y.Length);

                        if (retval != 0)
                            return retval;
                        else
                            return x.CompareTo(y);
                    }
                }
            }
            else return idx.CompareTo(idy);
        }

        private string ChangeLvl(string Name,string Lvl)
        {
            string res = Name;
            if (res.EndsWith("-1"))
            {
                int a = 0;
                for (int i = 0; i < Lvl.Length; i++)
                {
                    char ch = Lvl[i];
                    if (ch == 'V')
                        a += 5;
                    else if (ch == 'I')
                    {
                        if ((i + 1 < Lvl.Length) && (Lvl[i + 1] == 'V'))
                            a -= 1;
                        else a += 1;
                    }
                }
                res = res.Substring(0, res.Length - 1)+ a.ToString();
                return res;
            }

            foreach (string lvl in lvls)
            {
                if (res.EndsWith(lvl))
                {
                    res = res.Substring(0, res.Length - lvl.Length) + Lvl;
                    break;
                }
                else if (res.Contains(lvl+" "))
                {
                    res = res.Replace(lvl+" ",Lvl+" ");
                    break;
                }
            }
            return res;
        }
        private static List<string> lvls = new List<string> { " I", " II", " III", " IV", " V", " VI", " VII", " VIII", " IX", " X",
            " XI", " XII", " XIII", " XIV", " XV", " XVI", " XVII", " XVIII", " XIX", " XX"};

        public static string RemoveLvl(string name)
        {
            foreach (var lvl in lvls)
            {
                if (name.EndsWith(lvl))
                {
                    name = name.Substring(0, name.Length - lvl.Length);
                    break;
                }
                else if (name.Contains(lvl + " "))
                {
                    name = name.Replace(lvl, string.Empty);
                    break;
                }
            }
            return name;
        }

        public static string ClassConv(string PClass)
        {
            string Title = PClass.Cap();
            if (Title == "Elementalist")
            {
                Title = "Mystic";
            }
            else if (Title == "Engineer")
            {
                Title = "Gunner";
            }
            else if (Title == "Soulless")
            {
                Title = "Reaper";
            }
            else if (Title == "Fighter")
            {
                Title = "Brawler";
            }
            else if (Title == "Assassin")
            {
                Title = "Ninja";
            }
            else if (Title == "Glaiver")
            {
                Title = "Valkyrie";
            }
            return Title;
        }
    }
}