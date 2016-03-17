using System.Collections.Generic;
using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;

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

        public MonsterExtractor(string region)
        {
            Directory.CreateDirectory(OutFolder);
            _region = region;
		    Monsters();
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

                        outputFile.WriteLine("/>");
                    }
                    outputFile.WriteLine("\t</Zone>");
                }
                outputFile.WriteLine("</Zones>");
                outputFile.Flush();
                outputFile.Close();
            }
        }

        private void Monsters()
        {
            var xml = XDocument.Load(RootFolder + _region + "/ContinentData.xml");
            var contdata = (from cont in xml.Root.Elements("Continent") let battle = (cont.Attribute("channelType")==null)?false:cont.Attribute("channelType").Value== "battleField" let idcont = cont.Attribute("id").Value from hunting in cont.Elements("HuntingZone") let idzone = hunting.Attribute("id").Value where idcont != "" && idzone != "" select new { idcont, idzone, battle}).ToList();
            xml = XDocument.Load(RootFolder + _region + "/StrSheet_Region.xml");
            var regdata = (from str in xml.Root.Elements("String") let idname = str.Attribute("id").Value let regname = str.Attribute("string").Value where idname != "" && regname != "" select new { idname, regname }).ToList();

            var contToStr = "".Select(t=> new { idcont = string.Empty, nameid = string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/Area/"))
            {
                xml = XDocument.Load(file);
                var areadata = (from area in xml.Root.Document.Elements("Area") let idcont = area.Attribute("continentId").Value let nameid = area.Attribute("nameId").Value where idcont != "" && nameid != "" select new { idcont, nameid });
                contToStr = contToStr.Union(areadata).ToList();
            }

            xml = XDocument.Load(RootFolder + _region + "/StrSheet_Dungeon/StrSheet_Dungeon-0.xml");
            var dundata = (from dun in xml.Root.Elements("String") let idcont = dun.Attribute("id").Value let dunname = dun.Attribute("string").Value where idcont != "" && dunname != "" select new { idcont, dunname }).ToList();

            var regdd = (from contn in contToStr
                         join reg in regdata on contn.nameid equals reg.idname into regn
                         from rg in regn
                         join dun in dundata on contn.idcont equals dun.idcont into regdun
                         from rd in regdun.DefaultIfEmpty()
                            select new { idcont = contn.idcont, regname = rd == null ? rg.regname : rd.dunname ,nameid = contn.nameid}).ToList();

            var zonedata= ( from rd in regdd
                            from cont in contdata
                            where rd.idcont == cont.idcont 
                            let idzone= Convert.ToInt32(cont.idzone) let battle = cont.battle
                            let prio= (!rd.nameid.StartsWith(cont.idzone))
                            orderby idzone,prio
                            select new { Id=idzone, Name=rd.regname,battle} ).ToList();
            zonedata=zonedata.Distinct((x, y) => x.Id == y.Id, (x) => x.Id.GetHashCode()).ToList();
            //using (StreamWriter outputFile = new StreamWriter("data/cont.csv"))
            //{
            //    foreach (var line in zonedata)
            //    {
            //        outputFile.WriteLine("{0};{1}", line.Id, line.Name);
            //    }
            //}
            xml = XDocument.Load(RootFolder + _region + "/StrSheet_Creature.xml");
            var mobdata = (from hunting in xml.Root.Elements("HuntingZone") let idzone = hunting.Attribute("id").Value from entity in hunting.Elements("String") let identity = entity.Attribute("templateId").Value let name = entity.Attribute("name").Value where name != "" && identity != "" && idzone != "" select new { idzone, identity, name }).ToList();
            var mobprop = "".Select(t => new { idzone = string.Empty, id=string.Empty,boss=false,maxHP=string.Empty,size=string.Empty }).ToList();
            foreach (
                var file in
                    Directory.EnumerateFiles(RootFolder + _region + "/NpcData/"))
            {
                xml = XDocument.Load(file);
                var mobpdata = (from hunting in xml.Root.Document.Elements("NpcData") let idzone = hunting.Attribute("huntingZoneId").Value
                                from entity in hunting.Elements("Template") let id = entity.Attribute("id").Value
                                    let boss = (entity.Attribute("showAggroTarget")==null)?false: bool.Parse(entity.Attribute("showAggroTarget").Value)
                                    let size = (entity.Attribute("size") == null) ? "" : entity.Attribute("size").Value
                                from stat in entity.Elements("Stat") let maxHP = stat.Attribute("maxHp").Value
                                where id != "" && idzone != "" select new { idzone, id, boss, maxHP,size }).ToList();
                mobprop = mobprop.Union(mobpdata).ToList();
            }
            var moball = (from mobd in mobdata join mobb in mobprop on new { mobd.idzone, id = mobd.identity } equals new { mobb.idzone, mobb.id } into moba
                          from mobs in moba.DefaultIfEmpty()
                          let idzone=Convert.ToInt32(mobd.idzone) let identity=Convert.ToInt32(mobd.identity)
                          orderby idzone, identity select new { idzone, identity, mobd.name, boss= mobs==null?false:mobs.boss, maxHP = mobs==null?"0":mobs.maxHP, size = mobs==null ? "" : mobs.size }).ToList();
            var alldata = (from mobs in moball join zoned in zonedata on mobs.idzone equals zoned.Id into zones
                           from zone in zones.DefaultIfEmpty()
                           orderby mobs.idzone, mobs.identity select new { mobs.idzone, regname=zone==null?"unknown":zone.Name, mobs.identity, mobs.name, boss=(zone==null)?false:zone.battle?false:mobs.boss, mobs.maxHP, mobs.size }).ToList();

            var bossOverride = new Dictionary<int, Dictionary<int, bool>>();
            var nameOverride = new Dictionary<int, Dictionary<int, string>>();

            if (File.Exists(RootFolder + "override/monsters-override.xml")){
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
            //using (StreamWriter outputFile = new StreamWriter("data/npc.txt"))
            //{
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

                    _zones[all.idzone].Monsters.Add(all.identity, new Monster(all.identity, name, all.maxHP, isboss));

                //    outputFile.WriteLine("{0} {1} {2} {3} {4}", all.idzone, all.identity, isboss, all.maxHP, name);
                }
            //}
        }
    }
}