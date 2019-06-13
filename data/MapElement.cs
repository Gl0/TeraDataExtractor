using System.Collections.Generic;

namespace TeraDataExtractor.data
{
    public class MapElement
    {
        public int Id { get; set; }
        public int NameId { get; set; }
        public string MapId { get; set; }
        public bool IsDungeon { get; set; }
        public int ContinentId { get; set; }
        public List<MapElement> Children { get; set; }
        public MapElement(int id, int nameId, string mapId, bool dng = false)
        {
            Children = new List<MapElement>();
            Id = id;
            NameId = nameId;
            MapId = mapId;
            IsDungeon = dng;
        }
    }
}