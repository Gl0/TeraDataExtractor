namespace TeraDataExtractor.data
{
    public class GuildQuest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ZoneId { get; set; }
        public int BattleFieldId { get; set; }

        public GuildQuest(int id, string s)
        {
            Id = id;
            Title = s;
        }
    }
}
