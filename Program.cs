using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    public class Program
    {
        public static string SourcePath = "j:/c/Extract/";
        public static string OutputPath = "data";
        public static string IconFolder = Path.Combine(OutputPath, "icons");
        public static List<string> Copied = new List<string>();
        public static SortedDictionary<int, string> abnormals = new SortedDictionary<int, string>();
        private static void Main(string[] args) {
            GCSettings.LatencyMode = GCLatencyMode.Batch;
            Directory.CreateDirectory(OutputPath);//create output directory if not exist
            Directory.CreateDirectory(IconFolder);//create output directory if not exist
            if (args.Length == 0) { new OpcodeDownloader(); return;}
//            var region = "RU";
            Parallel.ForEach(args.ToList(), region => {
                var dirInfo = new DirectoryInfo(SourcePath + region);
                var fileName = dirInfo.EnumerateFiles("Datacenter_Fina*").OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault()?.FullName;
                if (string.IsNullOrWhiteSpace(fileName)) {Console.WriteLine("Missing "+fileName);return;}
                using var dc = new DataCenter(File.OpenRead(fileName), true);
                Console.WriteLine(region+": "+dc.Root.Child("BuildVersion")["version",0].ToString());
                new SkillExtractor(region, dc, out var skills, out var templates);
                new MonsterExtractor(region, dc, skills, templates);
                new DotExtractor(region, dc, skills, templates);
                TCCStuff(region, dc);
            });

            PackIcons();
        }


        public static void TCCStuff(string region, DataCenter dc=null)
        {
            new ItemsExtractor(region, dc);
            new AccountBenefitExtractor(region, dc);
            new NewWorldMapDataExtractor(region, dc);
            new EquipmentExpDataExtractor(region, dc); //
            new AchievementGradeInfoExtractor(region, dc); //
            new AchievementsExtractor(region, dc);
            new DungeonsExtractor(region, dc);
            new QuestExtractor(region, dc);
            new RegionExtractor(region, dc);
            new SocialExtractor(region, dc);
            new SystemMessagesExtractor(region, dc);
            new GuildQuestsExtractor(region, dc); //
        }

        public static void Copytexture(string name, int id = 0)
        {
            name = name.ToLowerInvariant();
            lock (Copied)
            {
                if (!string.IsNullOrEmpty(name) && !Copied.Contains(name))
                {
                    var filename = SourcePath + "Icons\\" + name.Replace(".", "\\Texture2D\\") + ".png";
                    var outfilename = Path.Combine(IconFolder, name + ".png");
                    if (File.Exists(filename))
                    {
                        if (!File.Exists(outfilename))
                            File.Copy(filename, Path.Combine(outfilename), true);
                        Copied.Add(name);
                    }
                    else Console.WriteLine("Not found texture: " + name);
                }
            }
            if (!string.IsNullOrEmpty(name) && id != 0 && !abnormals.ContainsKey(id)) abnormals.Add(id, name);
        }

        public static void PackIcons()
        {
            if (File.Exists(IconFolder + ".zip"))
                File.Delete(IconFolder + ".zip");

            Package zip = Package.Open(IconFolder + ".zip", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            foreach (var file in Directory.EnumerateFiles(IconFolder))
            {
                PackagePart part = zip.CreatePart(new Uri("/" + Path.GetFileName(file), UriKind.Relative), "image/png", CompressionOption.Maximum);
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