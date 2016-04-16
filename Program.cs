using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;

namespace TeraDataExtractor
{
    public class Program
    {
        public static string SourcePath = "j:/c/Extract/";
        public static string OutputPath = "data";
        public static string IconFolder = Path.Combine(OutputPath, "icons");
        public static List<string> Copied = new List<string>();
        public static SortedDictionary<int,string> abnormals = new SortedDictionary<int, string>();
        private static void Main(string[] args)
        {
            Directory.CreateDirectory(OutputPath);//create output directory if not exist
            Directory.CreateDirectory(IconFolder);//create output directory if not exist

            //new MonsterExtractor("RU");
            //new MonsterExtractor("EU-EN");
            //new MonsterExtractor("EU-FR");
            //new MonsterExtractor("EU-GER");
            //new MonsterExtractor("NA");
            //new MonsterExtractor("TW");
            //new MonsterExtractor("JP");
            //new MonsterExtractor("KR");

            //new SkillExtractor("RU");
            //new SkillExtractor("EU-EN");
            //new SkillExtractor("EU-FR");
            //new SkillExtractor("EU-GER");
            //new SkillExtractor("NA");
            //new SkillExtractor("TW");
            //new SkillExtractor("JP");
            //new SkillExtractor("KR");

            new DotExtractor("RU");
            new DotExtractor("EU-EN");
            new DotExtractor("EU-FR");
            new DotExtractor("EU-GER");
            new DotExtractor("NA");
            new DotExtractor("TW");
            new DotExtractor("JP");
            new DotExtractor("KR");

            //new CharmExtractor("RU");
            //new CharmExtractor("EU-EN");
            //new CharmExtractor("EU-FR");
            //new CharmExtractor("EU-GER");
            //new CharmExtractor("NA");
            //new CharmExtractor("TW");
            //new CharmExtractor("JP");
            //new CharmExtractor("KR");

            //PackIcons();
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