using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TeraDataExtractor.data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Not really needed, just strip unused data to save 5x space
    /// </summary>
    public class NewWorldMapDataExtractor
    {
        private readonly string _region;
        private string RootFolder = Program.SourcePath;
        private string OutFolder = Path.Combine(Program.OutputPath, "world_map");
        private List<MapElement> _worlds;

        public NewWorldMapDataExtractor(string region)
        {
            _region = region;
            Directory.CreateDirectory(OutFolder);
            _worlds = new List<MapElement>();
            var xdoc = XDocument.Load(Path.Combine(RootFolder, _region, "NewWorldMapData.xml"));
            xdoc.Descendants().Where(x => x.Name == "World").ToList().ForEach(worldEl =>
            {
                var wId = Convert.ToUInt32(worldEl.Attribute("id").Value);
                var wNameId = worldEl.Attribute("nameId") != null ? Convert.ToUInt32(worldEl.Attribute("nameId").Value) : 0;
                var wMapId = worldEl.Attribute("mapId") != null ? worldEl.Attribute("mapId").Value : "";
                var world = new MapElement(wId, wNameId, wMapId);

                worldEl.Descendants().Where(x => x.Name == "Guard").ToList().ForEach(guardEl =>
                {
                    var gId = Convert.ToUInt32(guardEl.Attribute("id").Value);
                    var gNameId = guardEl.Attribute("nameId") != null ? Convert.ToUInt32(guardEl.Attribute("nameId").Value) : 0;
                    var gMapId = guardEl.Attribute("mapId") != null ? guardEl.Attribute("mapId").Value : "";

                    var guard = new MapElement(gId, gNameId, gMapId);

                    guardEl.Descendants().Where(x => x.Name == "Section").ToList().ForEach(sectionEl =>
                    {
                        var sId = Convert.ToUInt32(sectionEl.Attribute("id").Value);
                        var sNameId = Convert.ToUInt32(sectionEl.Attribute("nameId").Value);
                        var sMapId = sectionEl.Attribute("mapId")?.Value??"";
                        var section = new MapElement(sId, sNameId, sMapId);

                        guard.Children.Add(section);
                    });

                    world.Children.Add(guard);
                });

                _worlds.Add(world);
            });

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
                                                    new XAttribute("nameId", g.NameId));
                    g.Children.ForEach(s =>
                    {

                        var sEl = new XElement("Section", new XAttribute("id", s.Id),
                                                        new XAttribute("mapId", s.MapId),
                                                        new XAttribute("nameId", s.NameId));
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
