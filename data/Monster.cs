using System;
using System.Globalization;

namespace TeraDataExtractor
{
    public class Monster
    {
        public Monster(int id, string name, string hp, bool isboss)
        {
            Id = id;
            Name = name;
            IsBoss = isboss;
            var dot = hp.IndexOf(".");
            Hp = long.Parse(string.IsNullOrWhiteSpace(hp)?"0":dot>0?hp.Substring(0,dot):hp);
        }

        public int Id { get; }

        public string Name { get; }

        public long Hp { get; set; }
        public bool IsBoss { get; set; }
    }
}