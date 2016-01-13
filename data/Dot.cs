using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraDataExtractor
{
    public enum methods
    {
        abs = 2, //each tick  HP +=Amount
        perc = 3  // each tick  HP += MaxHP*Amount
    }
    class HotDot
    {
        public int AbnormalId { get; set; }
        public string effectID { get; set; }
        public double Amount { get; set; }
        public methods Method { get; set; }
        public int Time { get; set; } //debuff time in msec
        public int Tick { get; set; } //tick time in sec
        public string Name { get; set; }

        public HotDot(int abnormalid, string effectid, double amount, string method, int time, int tick, string name)
        {
            AbnormalId = abnormalid;
            effectID = effectid;
            Amount = amount;
            Method = (methods) Enum.Parse(typeof(methods), method);
            Time = time;
            Tick = tick;
            Name = name;
        }
        public override string ToString()
        {
            return AbnormalId + "\t" + effectID + "\t" + Amount + "\t" + Method + "\t" + Time + "\t" + Tick + "\t" + Name;
        }
    }
}
