using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;

namespace TeraDataExtractor
{
    public class Program
    {
        public static string SourcePath = @"D:\Vincenzo\Downloads\TeraDataCenterTools-0.2";
        public static string OutputPath = "data";
        public static string IconFolder = Path.Combine(OutputPath, "icons");
        public static List<string> Copied = new List<string>();
        public static SortedDictionary<int,string> abnormals = new SortedDictionary<int, string>();
        private static void Main(string[] args)
        {
            Directory.CreateDirectory(OutputPath);//create output directory if not exist
            Directory.CreateDirectory(IconFolder);//create output directory if not exist

            new MonsterExtractor("RU");
            new MonsterExtractor("EU-EN");
            new MonsterExtractor("EU-FR");
            new MonsterExtractor("EU-GER");
            new MonsterExtractor("NA");
            new MonsterExtractor("TW");
            new MonsterExtractor("JP");
            new MonsterExtractor("KR");

            new SkillExtractor("RU");
            new SkillExtractor("EU-EN");
            new SkillExtractor("EU-FR");
            new SkillExtractor("EU-GER");
            new SkillExtractor("NA");
            new SkillExtractor("TW");
            new SkillExtractor("JP");
            new SkillExtractor("KR");

            new DotExtractor("RU");
            new DotExtractor("EU-EN");
            new DotExtractor("EU-FR");
            new DotExtractor("EU-GER");
            new DotExtractor("NA");
            new DotExtractor("TW");
            new DotExtractor("JP");
            new DotExtractor("KR");

            new Quests("RU");
            new Quests("EU-EN");
            new Quests("EU-FR");
            new Quests("EU-GER");
            new Quests("NA");
            new Quests("TW");
            new Quests("JP");
            new Quests("KR");

            #region TCC stuff
            new AccountBenefitExtractor("RU");
            new AccountBenefitExtractor("EU-EN");
            new AccountBenefitExtractor("EU-FR");
            new AccountBenefitExtractor("EU-GER");
            new AccountBenefitExtractor("NA");
            new AccountBenefitExtractor("TW");
            new AccountBenefitExtractor("JP");
            new AccountBenefitExtractor("KR");

            new NewWorldMapDataExtractor("RU");
            new NewWorldMapDataExtractor("EU-EN");
            new NewWorldMapDataExtractor("EU-FR");
            new NewWorldMapDataExtractor("EU-GER");
            new NewWorldMapDataExtractor("NA");
            new NewWorldMapDataExtractor("TW");
            new NewWorldMapDataExtractor("JP");
            new NewWorldMapDataExtractor("KR");

            new EquipmentExpDataExtractor("RU");
            new EquipmentExpDataExtractor("EU-EN");
            new EquipmentExpDataExtractor("EU-FR");
            new EquipmentExpDataExtractor("EU-GER");
            new EquipmentExpDataExtractor("NA");
            new EquipmentExpDataExtractor("TW");
            new EquipmentExpDataExtractor("JP");
            new EquipmentExpDataExtractor("KR");

            new AchievementGradeInfoExtractor("RU");
            new AchievementGradeInfoExtractor("EU-EN");
            new AchievementGradeInfoExtractor("EU-FR");
            new AchievementGradeInfoExtractor("EU-GER");
            new AchievementGradeInfoExtractor("NA");
            new AchievementGradeInfoExtractor("TW");
            new AchievementGradeInfoExtractor("JP");
            new AchievementGradeInfoExtractor("KR");

            new AchievementsExtractor("RU");
            new AchievementsExtractor("EU-EN");
            new AchievementsExtractor("EU-FR");
            new AchievementsExtractor("EU-GER");
            new AchievementsExtractor("NA");
            new AchievementsExtractor("TW");
            new AchievementsExtractor("JP");
            new AchievementsExtractor("KR");

            new DungeonsExtractor("RU");
            new DungeonsExtractor("EU-EN");
            new DungeonsExtractor("EU-FR");
            new DungeonsExtractor("EU-GER");
            new DungeonsExtractor("NA");
            new DungeonsExtractor("TW");
            new DungeonsExtractor("JP");
            new DungeonsExtractor("KR");

            new QuestExtractor("RU");
            new QuestExtractor("EU-EN");
            new QuestExtractor("EU-FR");
            new QuestExtractor("EU-GER");
            new QuestExtractor("NA");
            new QuestExtractor("TW");
            new QuestExtractor("JP");
            new QuestExtractor("KR");

            new RegionExtractor("RU");
            new RegionExtractor("EU-EN");
            new RegionExtractor("EU-FR");
            new RegionExtractor("EU-GER");
            new RegionExtractor("NA");
            new RegionExtractor("TW");
            new RegionExtractor("JP");
            new RegionExtractor("KR");

            new SocialExtractor("RU");
            new SocialExtractor("EU-EN");
            new SocialExtractor("EU-FR");
            new SocialExtractor("EU-GER");
            new SocialExtractor("NA");
            new SocialExtractor("TW");
            new SocialExtractor("JP");
            new SocialExtractor("KR");

            new SystemMessagesExtractor("RU");
            new SystemMessagesExtractor("EU-EN");
            new SystemMessagesExtractor("EU-FR");
            new SystemMessagesExtractor("EU-GER");
            new SystemMessagesExtractor("NA");
            new SystemMessagesExtractor("TW");
            new SystemMessagesExtractor("JP");
            new SystemMessagesExtractor("KR");
            #endregion

            PackIcons();
        }
        public static void Copytexture(string name,int id=0)
        {
            name = name.ToLowerInvariant();
            if (!string.IsNullOrEmpty(name)&&!Copied.Contains(name))
            {
                var filename = SourcePath + "Icons\\" + name.Replace(".", "\\Texture2D\\") + ".png";
                if (File.Exists(filename))
                {
                    File.Copy(filename, Path.Combine(IconFolder, name + ".png"), true);
                    Copied.Add(name);
                }
                else Console.WriteLine("Not found texture: " + name);
            }
            if (!string.IsNullOrEmpty(name) && id!=0 && !abnormals.ContainsKey(id)) abnormals.Add(id, name);
        }

        public static void PackIcons()
        {
            if (File.Exists(IconFolder + ".zip"))
                File.Delete(IconFolder + ".zip");

            Package zip = Package.Open(IconFolder + ".zip", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            foreach (var file in Directory.EnumerateFiles(IconFolder))
            {
                PackagePart part= zip.CreatePart(new Uri("/"+Path.GetFileName(file),UriKind.Relative), "image/png",CompressionOption.Maximum);
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(part.GetStream());
                }
            }
            PackagePart part1 = zip.CreatePart(new Uri("/enraged.png", UriKind.Relative), "image/png", CompressionOption.Maximum);
            using (FileStream fileStream = new FileStream(SourcePath + "Icons\\enraged.png", FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(part1.GetStream());
            }
            part1 = zip.CreatePart(new Uri("/slaying.png", UriKind.Relative), "image/png", CompressionOption.Maximum);
            using (FileStream fileStream = new FileStream(SourcePath + "Icons\\slaying.png", FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(part1.GetStream());
            }
            zip.Close();
            
            if (abnormals.Count > 0)
            {
                var outputFile = new StreamWriter(Path.Combine(OutputPath, $"hotdot/abnormal.tsv"));
                foreach (var i in abnormals)
                {
                    outputFile.WriteLine($"{i.Key}\t{i.Value}");
                }
                outputFile.Close();
            }
        }
    }
}