using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraDataExtractor
{
    public enum methods
    {
        abs = 2, //each tick  HP +=HPChange ; MP += MPChange
        perc = 3  // each tick  HP += MaxHP*HPChange; MP += MaxMP*MPChange
    }
    class HotDot
    {
        public int AbnormalId { get; set; }
        public string effectID { get; set; } //seems usless
        public double HPChange { get; set; }
        public double MPChange { get; set; }
        public methods Method { get; set; }
        public int Time { get; set; } //debuff time in msec, 0 = infinite
        public int Tick { get; set; } //tick time in sec
        public string Name { get; set; }

        public HotDot(int abnormalid, string effectid, string type, double amount, string method, int time, int tick, string name)
        {
            AbnormalId = abnormalid;
            effectID = effectid;
            HPChange = (type == "51") ? amount : 0;
            MPChange = (type == "52") ? amount : 0;
            Method = (methods) Enum.Parse(typeof(methods), method);
            Time = time;
            Tick = tick;
            Name = name;
        }
        public override string ToString()
        {
            return AbnormalId + "\t" + effectID + "\t" + HPChange.ToString("F", CultureInfo.InvariantCulture) + "\t" + MPChange.ToString("F", CultureInfo.InvariantCulture) + "\t" + Method + "\t" + Time + "\t" + Tick + "\t" + Name;
        }
    }
}
