using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraDataExtractor.data
{
    public class GuildQuest
    {
        public uint Id { get; set; }
        public string Title { get; set; }
        public uint ZoneId { get; set; }
        public uint BattleFieldId { get; set; }

        public GuildQuest(uint id, string s)
        {
            Id = id;
            Title = s;
        }
    }
}
