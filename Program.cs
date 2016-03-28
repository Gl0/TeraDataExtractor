using System.Collections.Generic;
using System.IO;

namespace TeraDataExtractor
{
    public class Program
    {
        public static string SourcePath = "j:/c/Extract/";
        public static string OutputPath = "data";
        private static void Main(string[] args)
        {
            Directory.CreateDirectory(OutputPath);//create output directory if not exist
            //new MonsterExtractor("RU");
            //new MonsterExtractor("EU-EN");
            //new MonsterExtractor("EU-FR");
            //new MonsterExtractor("EU-GER");
            //new MonsterExtractor("NA");
            //new MonsterExtractor("TW");
            //new MonsterExtractor("JP");
            //new MonsterExtractor("KR");

            new SkillExtractor("RU");
            //new SkillExtractor("EU-EN");
            //new SkillExtractor("EU-FR");
            //new SkillExtractor("EU-GER");
            //new SkillExtractor("NA");
            //new SkillExtractor("TW");
            //new SkillExtractor("JP");
            //new SkillExtractor("KR");

            //new DotExtractor("RU");
            //new DotExtractor("EU-EN");
            //new DotExtractor("EU-FR");
            //new DotExtractor("EU-GER");
            //new DotExtractor("NA");
            //new DotExtractor("TW");
            //new DotExtractor("JP");
            //new DotExtractor("KR");

            //new CharmExtractor("RU");
            //new CharmExtractor("EU-EN");
            //new CharmExtractor("EU-FR");
            //new CharmExtractor("EU-GER");
            //new CharmExtractor("NA");
            //new CharmExtractor("TW");
            //new CharmExtractor("JP");
            //new CharmExtractor("KR");

        }
    }
}