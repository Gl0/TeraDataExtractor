using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Alkahest.Core.Data;
using TeraDataExtractor.data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Not really needed, just strip unused data to save 5x space
    /// </summary>
    public class NewWorldMapDataExtractor
    {
        private readonly string _region;
        private string OutFolder = Path.Combine(Program.OutputPath, "world_map");
        private List<MapElement> _worlds;

        public NewWorldMapDataExtractor(string region, DataCenter dc)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            _worlds = new List<MapElement>();
            dc.Root.Child("NewWorldMapData").Children("World").ToList().ForEach(worldEl => {
                int wId = worldEl["id", 0].ToInt32();
                int wNameId = worldEl["nameId",0].ToInt32();
                string wMapId = worldEl["mapId",""].AsString;
                var world = new MapElement(wId , wNameId, wMapId);

                worldEl.Children("Guard").ToList().ForEach(guardEl =>
                {
                    var gId = guardEl["id",0].ToInt32();
                    var gNameId = guardEl["nameId",0].ToInt32();
                    var gMapId = guardEl["mapId",""].AsString;
                    var guard = new MapElement(gId, gNameId, gMapId);

                    guardEl.Children("Section").ToList().ForEach(sectionEl =>
                    {
                        var sId = sectionEl["id",0].ToInt32();
                        var sNameId = sectionEl["nameId",0].ToInt32();
                        var sMapId = sectionEl["mapId",""].AsString;
                        var dg = sectionEl["type",""].AsString == "dungeon" ? true : false;
                        var cId = sectionEl.Children("Npc").FirstOrDefault() ? ["continentId", 0].ToInt32() ?? 0;
                        var section = new MapElement(sId, sNameId, sMapId, dg);
                        if (guard.ContinentId == 0) guard.ContinentId = cId;
                        guard.Children.Add(section);
                    });
                    world.Children.Add(guard);
                });
                _worlds.Add(world);
            });
            SaveMap();
        }

        private void SaveMap() {
            var root = new XElement("WorldMap");
            _worlds.ToList().ForEach(w =>
            {
                var wEl = new XElement("World", new XAttribute("id", w.Id),
                    new XAttribute("mapId", w.MapId),
                    new XAttribute("nameId", w.NameId));
                w.Children.ForEach(g =>
                {
                    var gEl = new XElement("Guard", new XAttribute("id", g.Id),
                        new XAttribute("mapId", g.MapId),
                        new XAttribute("nameId", g.NameId),
                        new XAttribute("continentId", g.ContinentId)); g.Children.ForEach(s =>
                    {

                        var sEl = new XElement("Section", new XAttribute("id", s.Id),
                            new XAttribute("mapId", s.MapId),
                            new XAttribute("nameId", s.NameId));
                        if (s.IsDungeon) sEl.Add(new XAttribute("type", "dungeon"));
                        gEl.Add(sEl);
                    });
                    wEl.Add(gEl);
                });
                root.Add(wEl);
            });
            root.Save(Path.Combine(OutFolder, $"world_map-{_region}.xml"));
        }
    }
}
