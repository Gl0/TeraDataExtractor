using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    /// <summary>
    /// Extracts system messages
    /// Structure: channel, sysOpcodeName, message
    /// </summary>
    class SystemMessagesExtractor
    {
        private string OutFolder = Path.Combine(Program.OutputPath, "sys_msg");

        public SystemMessagesExtractor(string region, DataCenter dc)
        {
            Directory.CreateDirectory(OutFolder);
            var lines = new List<string>();
            dc.Root.Child("StrSheet_SystemMessage").Children("String").ToList().ForEach(str =>
            {
                // Parse message
                var s = str["string", ""].AsString;
                if (s == "") return;
                var d = str["displayChat", false].ToBoolean();
                if (!d) return;
                var id = str["readableId",""].AsString;
                var ch = str["chatChannel",302].ToInt32();
                // Manual fixes, thanks BHS
                s = CaseOverride(s);
                if (id == "SMT_GQUEST_URGENT_APPEAR") s = AppearMsgOverride(s);
                if (id == "SMT_FIELDNAMED_DIE") s = RallyDieOverride(s);
                if (id == "SMT_FIELDNAMED_RANK" && (region == "EU-EN" || region == "NA" || region == "THA" || region == "SE"))
                    s = "<font color='#1DDB16'>" + s;
                if (id == "SMT_GUILD_WAR_WITHDRAW_GUILDMONEY") s = s.Replace("</font></font>", "</font>");
                if (id == "SMT_MEDIATE_REG_WARNING_UI_MESSAGE" && region == "RU") s = "<font>" + s;
                if ((id == "SMT_INGAMESTORE_BUYALERT" || id == "SMT_INGAMESTORE_TCAT_BUYALERT") && region == "TW") s = s + "</font>";

                // Check <font/> tags consistency
                if (s != "" && id != "SMT_BOOSTERENCHANT_GUIDE" && id != "SMT_CITYWAR_DEAD_MESSAGE")
                {
                    var x = Regex.Escape(s);
                    var sCount = Regex.Matches(x, "<font").Count;
                    var eCount = Regex.Matches(x, "/font>").Count;
                    if (eCount != sCount)
                    {
                        Console.WriteLine(region + "   " + id);
                    }
                }

                lines.Add(ch + "\t" + id + "\t" + s.Replace("\n", "&#xA;"));
            });
            File.WriteAllLines(Path.Combine(OutFolder, $"sys_msg-{region}.tsv"), lines);
        }

        #region Thanks BHS
        private static string RallyDieOverride(string s)
        {
            return s.Replace("npcname", "npcName");
        }
        private static string CaseOverride(string s)
        {
            s = s.Replace("NPCNAME", "npcName");
            s = s.Replace("QUESTNAME", "questName");
            s = s.Replace("ZONENAME", "zoneName");

            return s;
        }
        private static string AppearMsgOverride(string s)
        {
            return s.Replace("appeared", " appeared");
        }
        #endregion
    }
}
