namespace TeraDataExtractor
{
    public class Monster
    {
        public Monster(int id, string name, string hp, bool isboss)
        {
            Id = id;
            Name = name;
            IsBoss = isboss;
            Hp = hp;
        }

        public int Id { get; }

        public string Name { get; }

        public string Hp { get; set; }
        public bool IsBoss { get; set; }
    }
}