using System.Collections.Generic;

namespace TeraDataExtractor
{
    public class Program
    {

        private static void Main(string[] args)
        {
            new MonsterExtractor("RU");
         //   new MonsterExtractor("EU-EN");
         //   new MonsterExtractor("EU-FR");
         //   new MonsterExtractor("EU-GER");

            new SkillExtractor("RU");
         //   new SkillExtractor("EU-EN");
         //   new SkillExtractor("EU-FR");
         //   new SkillExtractor("EU-GER");

            new DotExtractor("RU");
         //   new DotExtractor("EU-EN");
         //   new DotExtractor("EU-FR");
         //   new DotExtractor("EU-GER");

        }
    }
}