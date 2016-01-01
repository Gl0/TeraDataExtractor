using System.Collections.Generic;
using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace TeraDataExtractor
{

    /**
    WARNING: Pets are not reconized
    */
    public class SkillExtractor
    {
        private readonly string _region;
        private const string RootFolder = "j:/c/Extract/";
        private List<Skill> skilllist;

        public SkillExtractor(string region)
        {
            _region = region;
            skilllist = new List<Skill>();
            RawExtract();
            chained_skills();
            item_Skills();
            var outputFile = new StreamWriter("DATA/skills-" + _region + ".csv");
            foreach (Skill line in skilllist)
            {
//                outputFile.WriteLine(line.toSSV());
                outputFile.WriteLine(line.toCSV());
            }
            outputFile.Flush();
            outputFile.Close();
            SkillsFormat();
        }

        private void chained_skills()
        {
            var xml = XDocument.Load(RootFolder + _region + "/LobbyShape.xml");
            var templates = (from races in xml.Root.Elements("SelectRace") let race= races.Attribute("race").Value.Cap() let gender= races.Attribute("gender").Value.Cap() from temp in races.Elements("SelectClass") let PClass = ClassConv(temp.Attribute("class").Value) let templateId = temp.Attribute("templateId").Value where temp.Attribute("available").Value== "True" select new {race, gender, PClass ,templateId });
            //assume skills for different races and genders are the same per class 
            templates = templates.Distinct((x, y) => x.PClass == y.PClass, x => x.PClass.GetHashCode()).ToList();

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
            var ChainSkills = "".Select(t => new {PClass=string.Empty, skillid = string.Empty, p_skill = new ParsedSkill(string.Empty,string.Empty,string.Empty) }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/SkillData/"))
            {
                xml = XDocument.Load(file);
                var chaindata = (from temp in templates join skills in xml.Root.Elements("Skill") on temp.templateId equals skills.Attribute("templateId").Value into Pskills
                                 from Pskill in Pskills
                                    let PClass = temp.PClass
                                    let skillid = Pskill.Attribute("id").Value  
                                    let p_skill = new ParsedSkill(Pskill.Attribute("name").Value,skillid, (Pskill.Attribute("connectNextSkill")==null)?Pskill.Attribute("type").Value: Pskill.Attribute("type").Value+"_combo")
                                 where PClass != "" && skillid != "" select new { PClass, skillid, p_skill });
                ChainSkills = ChainSkills.Union(chaindata, (x, y) => (x.skillid == y.skillid)&&(x.PClass==y.PClass), x => (x.PClass+x.skillid).GetHashCode()).ToList();
            }
            var IntToPub = (from cs in ChainSkills join sl in skilllist on new { cs.PClass, cs.skillid } equals new { sl.PClass, skillid = sl.Id } into itps
                            from itp in itps select new { cs.p_skill.BaseName, itp.Name }).ToList();
            IntToPub.Distinct((x,y)=>x.BaseName==y.BaseName,x=>x.BaseName.GetHashCode());
            skilllist = (from cs in ChainSkills
                           join itp in IntToPub on cs.p_skill.BaseName equals itp.BaseName
                           join sl in skilllist on new { cs.skillid, cs.PClass } equals new { skillid = sl.Id, sl.PClass } into uskills
                           from uskill in uskills.DefaultIfEmpty(new Skill("","","","",""))
                           select new Skill(cs.skillid, "Common", "Common", cs.PClass, uskill.Name == "" ? itp.Name : uskill.Name,cs.p_skill.Chained,cs.p_skill.Detail)).ToList();
                         
        }
        private string cut_name(string internalname, out List<string> modifiers)
        {
            modifiers = new List<string> { };
            return internalname;
        }

        private void item_Skills()
        {
            var ItemSkills = "".Select(t => new { skillid = string.Empty, nameid = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/ItemData/"))
            {
                var xml = XDocument.Load(file);
                var itemdata = (from item in xml.Root.Elements("Item") let comb=(item.Attribute("category")== null)?"no": item.Attribute("category").Value let skillid = (item.Attribute("linkSkillId")==null)?"0":item.Attribute("linkSkillId").Value let nameid = item.Attribute("id").Value where ((comb=="combat")||(comb=="brooch") || (comb == "charm")) && skillid!="0" && skillid != "" && nameid != "" select new { skillid, nameid });
                // filter only combat items, we don't need box openings etc.
                ItemSkills = ItemSkills.Union(itemdata, (x, y) => x.skillid == y.skillid, x => x.skillid.GetHashCode()).ToList();
            }
            var ItemNames = "".Select(t => new { nameid = string.Empty, name = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_Item/"))
            {
                var xml = XDocument.Load(file);
                var namedata = (from item in xml.Root.Elements("String") let nameid = item.Attribute("id").Value let name = item.Attribute("string").Value where nameid != "" && name != "" select new { nameid, name });
                ItemNames = ItemNames.Union(namedata, (x, y) => x.nameid == y.nameid, x => x.nameid.GetHashCode()).ToList();
            }
            var Items = (from item in ItemSkills join nam in ItemNames on item.nameid equals nam.nameid orderby item.skillid where nam.name!="" select new Skill(item.skillid,"Common","Common","Common", nam.name)).ToList();
            skilllist=skilllist.Union(Items).ToList();
        }



        private void SkillsFormat()
        {
            var reader = new StreamReader(File.OpenRead("data/skills-" + _region + ".csv"));
            var mystic = new StreamWriter("data/skills/mystic-" + _region + ".tsv");
            var priest = new StreamWriter("data/skills/priest-" + _region + ".tsv");
            var gunner = new StreamWriter("data/skills/gunner-" + _region + ".tsv");
            var reaper = new StreamWriter("data/skills/reaper-" + _region + ".tsv");
            var archer = new StreamWriter("data/skills/archer-" + _region + ".tsv");
            var warrior = new StreamWriter("data/skills/warrior-" + _region + ".tsv");
            var slayer = new StreamWriter("data/skills/slayer-" + _region + ".tsv");
            var berserker = new StreamWriter("data/skills/berserker-" + _region + ".tsv");
            var sorcerer = new StreamWriter("data/skills/sorcerer-" + _region + ".tsv");
            var lancer = new StreamWriter("data/skills/lancer-" + _region + ".tsv");
            var common = new StreamWriter("data/skills/common-" + _region + ".tsv");
            var brawler = new StreamWriter("data/skills/brawler-" + _region + ".tsv");

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split(';');
                var id = values[0];
                var race = values[1];
                var gender = values[2];
                var playerclass = values[3];
                var name = values[4];
                var detail = values[5];
                var chained = values[6];
                var str = id + "\t" + name + "\t" + chained + "\t"+ detail;

                if (race != "Common" || gender != "Common") continue;

                switch (playerclass)
                {
                    case "Mystic":
                        mystic.WriteLine(str);
                        break;
                    case "Priest":
                        priest.WriteLine(str);
                        break;
                    case "Reaper":
                        reaper.WriteLine(str);
                        break;
                    case "Gunner":
                        gunner.WriteLine(str);
                        break;
                    case "Archer":
                        archer.WriteLine(str);
                        break;
                    case "Warrior":
                        warrior.WriteLine(str);
                        break;
                    case "Slayer":
                        slayer.WriteLine(str);
                        break;
                    case "Berserker":
                        berserker.WriteLine(str);
                        break;
                    case "Sorcerer":
                        sorcerer.WriteLine(str);
                        break;
                    case "Lancer":
                        lancer.WriteLine(str);
                        break;
                    case "Brawler":
                        brawler.WriteLine(str);
                        break;
                    case "Common":
                        common.WriteLine(str);
                        break;
                }
            }
            brawler.Flush();
            brawler.Close();
            common.Flush();
            common.Close();
            mystic.Flush();
            mystic.Close();
            warrior.Flush();
            warrior.Close();
            priest.Flush();
            priest.Close();
            lancer.Flush();
            lancer.Close();
            sorcerer.Flush();
            sorcerer.Close();
            berserker.Flush();
            berserker.Close();
            slayer.Flush();
            slayer.Close();
            archer.Flush();
            archer.Close();
            gunner.Flush();
            gunner.Close();
            reaper.Flush();
            reaper.Close();
        }

        private void RawExtract()
        {

            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/StrSheet_UserSkill/"))
            {
                var xml = XDocument.Load(file);
                var skilldata = (from item in xml.Root.Elements("String")
                                 let id = item.Attribute("id").Value
                                 let race = item.Attribute("race").Value
                                 let gender = item.Attribute("gender").Value
                                 let PClass = (item.Attribute("class")==null)?"": ClassConv(item.Attribute("class").Value)
                                 let name = item.Attribute("name").Value
                                 where id != "" && race != "" && gender != "" && name != "" && PClass != ""
                                 select new Skill( id, race, gender, PClass, name )).ToList();
                skilllist = skilllist.Union(skilldata).ToList();
            }
        }

        private string ClassConv(string PClass)
        {
            string Title = PClass.Cap();
            if (Title == "Elementalist")
            {
                Title = "Mystic";
            }
            if (Title == "Engineer")
            {
                Title = "Gunner";
            }
            if (Title == "Soulless")
            {
                Title = "Reaper";
            }
            if (Title == "Fighter")
            {
                Title = "Brawler";
            }
            return Title;
        }
    }
}