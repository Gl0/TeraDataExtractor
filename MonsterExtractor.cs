﻿using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /*
     If you are here, you will need to read and understand the game database.
     Welcome in hell.
    */


    public class MonsterExtractor
    {
        private string _region;

        private readonly Dictionary<int, Zone> _zones = new Dictionary<int, Zone>();
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "monsters");

        public MonsterExtractor(string region, DataCenter dc = null, List<Skill> skills = null, List<Template> templates = null)
        {
            Directory.CreateDirectory(OutFolder);
            _region = region;
            Monsters(dc, skills, templates);
            WriteXml();
        }

        private void WriteXml()
        {
            using (var outputFile = new StreamWriter(Path.Combine(OutFolder, $"monsters-{_region}.xml")))
            {
                outputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                outputFile.WriteLine("<Zones>");
                foreach (var zone in _zones.Values)
                {
                    if (zone.Monsters.Count == 0)
                    {
                        continue;
                    }
                    outputFile.Write("\t<Zone ");
                    outputFile.Write("id=\"" + zone.Id + "\" ");
                    outputFile.Write("name=\"" + zone.Name.Replace("\"", "'").Replace("&nbsp;", " ").Trim() + "\" ");
                    outputFile.WriteLine(">");
                    foreach (var monster in zone.Monsters)
                    {
                        outputFile.Write("\t\t<Monster ");
                        outputFile.Write("name=\"" + monster.Value.Name.Replace("\"", "'").Replace("&nbsp;", " ").Trim() + "\" ");
                        outputFile.Write("id=\"" + monster.Value.Id + "\" ");
                        outputFile.Write(monster.Value.IsBoss ? "isBoss=\"True\" " : "isBoss=\"False\" ");
                        outputFile.Write("hp=\"" + monster.Value.Hp + "\" ");
                        outputFile.Write("speciesId=\"" + monster.Value.Specie + "\" ");

                        outputFile.WriteLine("/>");
                    }
                    outputFile.WriteLine("\t</Zone>");
                }
                outputFile.WriteLine("</Zones>");
                outputFile.Flush();
                outputFile.Close();
            }
        }

        private void Monsters(DataCenter dc, List<Skill> skilllist, List<Template> templates)
        {
            var contdata = (from cont in dc.Root.FirstChild("ContinentData").Children("Continent") let battle = cont["channelType",""] == "battleField" let idcont = cont["id",0].ToInt32() from hunting in cont.Children("HuntingZone") let idzone = hunting["id",0].ToInt32() where idcont != 0 && idzone != 0 select new { idcont, idzone, battle }).ToList();

            var summons = (from temp in templates
                                  join skills in dc.Root.Children("SkillData").SelectMany(x=>x.Children("Skill")) on temp.templateId equals skills["templateId",0].ToInt32() into Pskills
                                  from Pskill in Pskills
                                  let PClass = temp.PClass
                                  let skillId = Pskill["id",0].ToInt32()
                                  from TargetingList in Pskill.Children("TargetingList")
                                  from Targeting in TargetingList.Children("Targeting")
                                  from AreaList in Targeting.Children("AreaList")
                                  from Area in AreaList.Children("Area")
                                  from Effect in Area.Children("Effect")
                                  from SummonNpc in Effect.Children("SummonNpc")
                                  let id = SummonNpc["templateId",0].ToInt32()
                                  where skillId != 0 && PClass != "" && id != 0
                                  select new { id, skillId, PClass }).Distinct((x, y) => x.PClass == y.PClass && x.id == y.id, x => (x.PClass, x.id).GetHashCode()).ToList();

            var summonNames = (from item in skilllist
                               join summon in summons on new { skillId = item.Id, PClass = item.PClass } equals new { summon.skillId, summon.PClass } into results
                               from res in results
                               let name = item.Name
                               where res.id != 0 && name != ""
                               select new { idzone = 1023, identity = res.id, name }).ToList();

            var regdata = (from str in dc.Root.FirstChild("StrSheet_Region").Children("String") let idname = str["id",0].ToInt32() let regname = str["string",""].AsString where idname != 0 && regname != "" select new { idname, regname }).ToList();

            var contToStr = (from area in dc.Root.Children("Area") let idcont = area["continentId",0].ToInt32() let nameid = area["nameId",0].ToInt32() where idcont != 0 && nameid != 0 select new { idcont, nameid }).Distinct().ToList();

            var dundata = (from dun in dc.Root.Children("StrSheet_Dungeon").SelectMany(x=>x.Children("String")) let idcont = dun["id",0].ToInt32() let dunname = dun["string",""].AsString where idcont != 0 && dunname != "" where idcont>8888 && idcont<10000 select new { idcont, dunname }).ToList();

            var regdd = (from contn in contToStr
                         join reg in regdata on contn.nameid equals reg.idname into regn
                         from rg in regn
                         join dun in dundata on contn.idcont equals dun.idcont into regdun
                         from rd in regdun.DefaultIfEmpty()
                         select new { idcont = contn.idcont, regname = rd == null ? rg.regname : rd.dunname, nameid = contn.nameid }).ToList();
            var zdata = (from zone in dc.Root.FirstChild("StrSheet_ZoneName").Children("String")
                         let Id = zone["id",0].ToInt32()
                         let Name = zone["string",""].AsString
                         join cont in contdata on Id equals cont.idzone into cb
                         from c in cb.DefaultIfEmpty()
                         select new { Id , Name, battle = c?.battle??false }).ToList();

            var zonedata = (from rd in regdd
                from cont in contdata
                where rd.idcont == cont.idcont
                let idzone = cont.idzone
                let battle = cont.battle
                let prio = (!rd.nameid.ToString().StartsWith(cont.idzone.ToString()))
                orderby idzone, prio
                select new {Id = idzone, Name = rd.regname, battle}).ToList();
            zonedata = zdata.Union(zonedata).ToList();
            zonedata = zonedata.Distinct((x, y) => x.Id == y.Id, (x) => x.Id.GetHashCode()).ToList();

            var mobdata = (from hunting in dc.Root.Children("StrSheet_Creature").SelectMany(x=>x.Children("HuntingZone"))
                           let idzone = hunting["id",0].ToInt32()
                           from entity in hunting.Children("String")
                           join summon in summonNames on new { idzone, identity = entity["templateId",0].ToInt32() } equals new { summon.idzone, summon.identity } into results
                           from res in results.DefaultIfEmpty()
                           let identity = entity["templateId",0].ToInt32()
                           let name = string.IsNullOrWhiteSpace(res?.name) ? entity["name",""].AsString : res.name
                           where name != "" && identity != 0
                           select new { idzone, identity, name }).ToList();
            mobdata = mobdata.Union(summonNames).ToList();

            var mobprop  = (from hunting in dc.Root.Children("NpcData")
                                let idzone = hunting["huntingZoneId",0].ToInt32()
                                from entity in hunting.Children("Template")
                                let id = entity["id",0].ToInt32()
                                let boss = entity["showAggroTarget",false].ToBoolean()
                                let size = entity["size",""].AsString
                                let speciesId = entity["speciesId", 0].AsInt32
                                from stat in entity.Children("Stat")
                                let maxHP = stat["maxHp","0"].AsString
                                where id != 0
                                select new { idzone, id, boss, maxHP, size, speciesId }).ToList();
            var moball = (from mobd in mobdata
                          join mobb in mobprop on new { mobd.idzone, id = mobd.identity } equals new { mobb.idzone, mobb.id } into moba
                          from mobs in moba.DefaultIfEmpty()
                          orderby mobd.idzone, mobd.identity
                          select new { mobd.idzone, mobd.identity, mobd.name, boss = mobs == null ? false : mobs.boss, maxHP = mobs == null ? "0" : mobs.maxHP, size = mobs == null ? "" : mobs.size, speciesId = mobs?.speciesId ?? 0}).ToList();
            var alldata = (from mobs in moball
                           join zoned in zonedata on mobs.idzone equals zoned.Id into zones
                           from zone in zones.DefaultIfEmpty()
                           orderby mobs.idzone, mobs.identity
                           select new { mobs.idzone, regname = zone == null ? "unknown" : zone.Name, mobs.identity, mobs.name, boss = (zone == null) ? false : zone.battle ? false : mobs.boss, mobs.maxHP, mobs.size, mobs.speciesId }).ToList();

            var bossOverride = new Dictionary<int, Dictionary<int, bool>>();
            var nameOverride = new Dictionary<int, Dictionary<int, string>>();

            if (File.Exists(RootFolder + "override/monsters-override.xml"))
            {
                var isBossOverrideXml = XDocument.Load(RootFolder + "override/monsters-override.xml");
                foreach (var zone in isBossOverrideXml.Root.Elements("Zone"))
                {
                    var id = int.Parse(zone.Attribute("id").Value);
                    bossOverride.Add(id, new Dictionary<int, bool>());
                    nameOverride.Add(id, new Dictionary<int, string>());
                    foreach (var monster in zone.Elements("Monster"))
                    {
                        var monsterId = int.Parse(monster.Attribute("id").Value);
                        var isBoss = monster.Attribute("isBoss");
                        if (isBoss != null)
                        {
                            var isBossString = isBoss.Value.ToLower();
                            bossOverride[id].Add(monsterId, isBossString == "true");
                        }
                        var bossName = monster.Attribute("name");
                        if (bossName == null) continue;
                        var nameOverrideString = bossName.Value;
                        nameOverride[id].Add(monsterId, nameOverrideString);
                    }
                }
            }
            foreach (var all in alldata)
            {
                if (!_zones.ContainsKey(all.idzone))
                {
                    _zones.Add(all.idzone, new Zone(all.idzone, all.regname));
                }
                bool isboss = all.boss;
                if (bossOverride.ContainsKey(all.idzone) && bossOverride[all.idzone].ContainsKey(all.identity))
                    isboss = bossOverride[all.idzone][all.identity];
                string name = all.name;
                if (nameOverride.ContainsKey(all.idzone) && nameOverride[all.idzone].ContainsKey(all.identity))
                    name = nameOverride[all.idzone][all.identity];
                if (name.Contains("{@Creature:"))
                {
                    int zone = int.Parse(name.Substring(name.IndexOf("{@Creature:") + 11, name.IndexOf("#") - name.IndexOf("{@Creature:") - 11));
                    int id = int.Parse(name.Substring(name.IndexOf("#") + 1, name.IndexOf("}") - name.IndexOf("#") - 1));
                    var subst = alldata.First(x => (x.idzone == zone && x.identity == id)).name;
                    name = name.Replace(name.Substring(name.IndexOf("{"), name.IndexOf("}") - name.IndexOf("{") + 1), subst);
                }
                if (name.Contains("{@Rgn:"))
                {
                    name = name.Replace("{@Rgn:", "").Replace("}", "");
                }

                if (!_zones[all.idzone].Monsters.ContainsKey(all.identity))
                    _zones[all.idzone].Monsters.Add(all.identity, new Monster(all.identity, name, all.maxHP, isboss, all.speciesId));
            }
        }
    }
}