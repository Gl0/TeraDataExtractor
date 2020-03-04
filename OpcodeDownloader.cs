using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace TeraDataExtractor
{
    public class OpcodeDownloader {
        private string OutFolder = Path.Combine(Program.OutputPath, "opcodes");

        public OpcodeDownloader() {
            Directory.CreateDirectory(OutFolder);
            try { ToolboxOpcodes("https://raw.githubusercontent.com/tera-toolbox/tera-toolbox/master/data/data.json", OutFolder); }
            catch { }
            try { ToolboxOpcodes("https://raw.githubusercontent.com/tera-toolbox/tera-toolbox/beta/data/data.json", OutFolder); }
            catch { }
        }

        public static void ToolboxOpcodes(string url, string directory) {
            using WebClient client = new WebClient();
            string json = client.DownloadString(url);
            var parsed = JsonConvert.DeserializeObject<ToolboxTeraData>(json);
            foreach (var map in parsed.maps) {
                var fname = Path.Combine(directory, $"protocol.{map.Key}.map");
                if (!File.Exists(fname)) File.WriteAllText(fname, string.Join("\n", map.Value.Select(x => x.Key + " " + x.Value)));
            }
        }

        public class ToolboxTeraData {
            public Dictionary<string, Dictionary<string, int>> maps { get; set; }
            public Dictionary<string, string> protocol { get; set; }
            public dynamic deprecated { get; set; }
        }
    }
}
