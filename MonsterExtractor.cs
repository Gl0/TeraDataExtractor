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
        private const string RootFolder = "j:/c/Extract/";

        public MonsterExtractor(string region)
        {
            _region = region;
		    Monsters();
            WriteXml();
        }

        private void WriteXml()
        {
            using (var outputFile = new StreamWriter("data/monsters-" + _region + ".xml"))
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
                    outputFile.Write("name=\"" + zone.Name.Replace("\"", "'") + "\" ");
                    outputFile.WriteLine(">");
                    foreach (var monster in zone.Monsters)
                    {
                        outputFile.Write("\t\t<Monster ");
                        outputFile.Write("name=\"" + monster.Value.Name.Replace("\"", "'") + "\" ");
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
                contToStr = contToStr.Union(areadata, (x, y) => x.idcont == y.idcont,x => x.idcont.GetHashCode()).ToList();
            }

            xml = XDocument.Load(RootFolder + _region + "/StrSheet_Dungeon/StrSheet_Dungeon-0.xml");
            var dundata = (from dun in xml.Root.Elements("String") let idcont = dun.Attribute("id").Value let dunname = dun.Attribute("string").Value where idcont != "" && dunname != "" select new { idcont, dunname }).ToList();

            var regdd = (from contn in contToStr
                         join reg in regdata on contn.nameid equals reg.idname into regn
                         from rg in regn
                         join dun in dundata on contn.idcont equals dun.idcont into regdun
                         from rd in regdun.DefaultIfEmpty()
                            select new { idcont = contn.idcont, regname = rd == null ? rg.regname : rd.dunname }).ToList();

            var zonedata= ( from rd in regdd
                            from cont in contdata
                            where rd.idcont == cont.idcont 
                            let idzone= Convert.ToInt32(cont.idzone) let battle = cont.battle
                            orderby idzone
                            select new { Id=idzone, Name=rd.regname,battle } ).ToList();
            using (StreamWriter outputFile = new StreamWriter("data/cont.csv"))
            {
                foreach (var line in zonedata)
                {
                    outputFile.WriteLine("{0};{1}", line.Id, line.Name);
                }
            }
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

            foreach (var all in alldata) {
                if (!_zones.ContainsKey(all.idzone))
                {
                    _zones.Add(all.idzone, new Zone(all.idzone, all.regname));
                }
                _zones[all.idzone].Monsters.Add(all.identity, new Monster(all.identity, all.name, all.maxHP, all.boss));
            }

            using (StreamWriter outputFile = new StreamWriter("data/monsters.csv"))
            {
                foreach (var line in alldata)
                {
                    outputFile.WriteLine("{0};{1};{2};{3};{4};{5}", line.idzone, line.regname, line.identity, line.name, line.boss,line.maxHP);
                }
            }
        }

        private void MonsterSeparator()
        {
            var reader = new StreamReader(File.OpenRead("data/monsters.csv"));
            var writers = new Dictionary<int, StreamWriter>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split(';');
                var area = Convert.ToInt32(values[0]);
                var areaname = values[1];
                var id = Convert.ToInt32(values[2]);
                var name = values[3];
                var boss = Boolean.Parse(values[4]);
                var maxHP = Convert.ToInt32(values[5]);
                if (!writers.ContainsKey(area))
                {

                    writers.Add(area, new StreamWriter("data/"+ area + "-" + areaname.Replace("\"", "'").Replace(':', ';').Replace("-","_") + ".tsv"));
                }
                StreamWriter writer;
                writers.TryGetValue(area, out writer);
                writer.WriteLine(id + "\t" + name + "\t" + boss +"\t" + maxHP);
            }

            foreach (var writer in writers)
            {
                writer.Value.Flush();
                writer.Value.Close();
            }
        }
    }
}