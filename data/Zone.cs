using System.Collections.Generic;

namespace TeraDataExtractor
{
    public class Zone
    {
        public Zone(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }

        public Dictionary<int, Monster> Monsters { get; } = new Dictionary<int, Monster>();
    }
}