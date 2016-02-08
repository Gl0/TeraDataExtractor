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
        seta = 1, // ?set abs stat value
        abs  = 2, // each tick  HP +=HPChange ; MP += MPChange
        perc = 3, // each tick  HP += MaxHP*HPChange; MP += MaxMP*MPChange
        setp = 4  // ?set % stat value
    }

    public enum types
    {
        Power = 3,
        Endurance = 4,
        MovSpd = 5,
        Crit = 6,
        CritResist = 7,
        Ballance = 9,
        WeakResist = 14,
        DotResist = 15,
        StunResist = 16, //something strange, internal itemname sleep_protect, but user string is stun resist, russian user string is "control effect resist"
        AllResist = 18,
        CritPower = 19,
        Aggro = 20,
        NoMPDecay = 21, //slayer
        Attack = 22, //total damage modificator
        XPBoost = 23,
        ASpd = 24,
        //25,210 = disable evasion and moving skills, not sure who is who
        CraftTime=26,
        OutOfCombatMovSpd = 27,
        //28 = Something comming with MovSpd debuff skills, fxp 32% MovSpd debuff from Lockdown Blow IV, give also 12% of this kind
        //29 = something strange when using Lethal Strike
        Stamina = 30,
        Gathering = 31,
        HPChange = 51,
        MPChange = 52,
        RageChange = 53,
        StaminaDecay = 207 //
        //280 50% rage = value 1.5??
    }
    class HotDot
    {
        public int AbnormalId { get; set; }
        public types _type { get; set; } //seems usless
        public double HPChange { get; set; }
        public double MPChange { get; set; }
        public methods Method { get; set; }
        public int Time { get; set; } //debuff time in msec, 0 = infinite
        public int Tick { get; set; } //tick time in sec
        public string Name { get; set; }
        public double Amount { get; set; }
        public string PClass { get; set; }
        public string Skillid { get; set; }

        public HotDot(int abnormalid, string type, double amount, string method, int time, int tick, string name,string skillid,string pclass)
        {
            AbnormalId = abnormalid;
            _type = (types) Enum.Parse(typeof(types),type);
            HPChange = (type == "51") ? amount : 0;
            MPChange = (type == "52") ? amount : 0;
            Method = (methods) Enum.Parse(typeof(methods), method);
            Time = time;
            Tick = tick;
            Amount = amount;
            Name = name;
            Skillid = skillid;
            PClass = pclass;
        }
        public override string ToString()
        {
            return AbnormalId + "\t" + _type + "\t" + HPChange.ToString("F", CultureInfo.InvariantCulture) + "\t" + MPChange.ToString("F", CultureInfo.InvariantCulture) + "\t" + Method + "\t" + Time + "\t" + Tick + "\t" + Amount.ToString("F", CultureInfo.InvariantCulture)+"\t" + Name + "\t" + Skillid + "\t" + PClass;
        }
    }
}
