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
        swch = 0, // switch on for noctineum ? other strange uses.
        seta = 1, // ?set abs stat value
        abs  = 2, // each tick  HP +=HPChange ; MP += MPChange
        perc = 3, // each tick  HP += MaxHP*HPChange; MP += MaxMP*MPChange
        setp = 4  // ?set % stat value
    }

    public enum types
    {
        MaxHP = 1,
        Power = 3,
        Endurance = 4,
        MovSpd = 5,
        Crit = 6,
        CritResist = 7,
        ImpactEffective = 8, 
        Ballance = 9,
        WeakResist = 14,
        DotResist = 15,
        StunResist = 16, //something strange, internal itemname sleep_protect, but user string is stun resist, russian user string is "control effect resist"
        AllResist = 18,
        CritPower = 19,
        CritPower1 = 36,
        Aggro = 20,
        NoMPDecay = 21, //slayer
        Attack = 22, //total damage modificator
        XPBoost = 23,
        ASpd = 24,
        MovSpdInCombat = 25,
        CraftTime=26,
        OutOfCombatMovSpd = 27,
        HPDrain=28, //drain hp on attack
        //28 = Something comming with MovSpd debuff skills, fxp 32% MovSpd debuff from Lockdown Blow IV, give also 12% of this kind
        //29 = something strange when using Lethal Strike
        Stamina = 30,
        Gathering = 31,
        HPChange = 51,
        MPChange = 52,
        RageChange = 53,
        KnockDownChance = 103,
        DefPotion = 104, //or glyph: - incoming damage %
        IncreasedHeal = 105,
        PVPDef = 108,
        AtkPotion = 162, //or glyph: + outgoing damage %
        CritChance = 167,
        PVPAtk = 168,
        Noctenium = 203, //different values for different kinds of Noctenium, not sure what for =)
        StaminaDecay = 207,
        CDR = 208,
        Block = 210, //frontal block ? Not sure, the ability to use block, or blocking stance
        HPLoss = 221, //loss hp at the and of debuff
        Absorb = 227, //or may be I've messed it with 281
        Resurrect = 229,
        Mark = 231, // Velik's Mark/Curse of Kaprima = increase received damage when marked
        CastSpeed = 236,
        CrystalBind = 237,
        CCrystalBind = 249,
        DropUp = 255,
        Range = 259, //increase melee range? method 0 value 0.1= +10%
        //264 = redirect abnormality, value= new abnormality, bugged due to wrong float format in xml.
        Rage = 280, //tick - RageChange, notick (one change) - Rage 
        SuperArmor = 283,
        ForceCrit = 316, //archer's Find Weakness = next hit will trigger critpower crystalls
        Charm = 65535
    }
    class HotDot
    {
        public int AbnormalId { get; set; }
        public types _type { get; set; }
        public double HPChange { get; set; }
        public double MPChange { get; set; }
        public methods Method { get; set; }
        public int Time { get; set; } //debuff time in msec, 0 = infinite
        public int Tick { get; set; } //tick time in sec
        public string Name { get; set; }
        public double Amount { get; set; }
        public string Itemid { get; set; }
        public string ItemName { get; set; }
        public string Tooltip { get; set; }
        public string IconName { get; set; }

        public HotDot(int abnormalid, string type, double amount, string method, int time, int tick, string name,string itemid, string itemName,string tooltip,string iconName)
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
            Itemid = itemid;
            ItemName = itemName;
            Tooltip = tooltip;
            IconName = iconName;
        }
        public override string ToString()
        {
            return AbnormalId + "\t" + _type + "\t" + HPChange.ToString("R", CultureInfo.InvariantCulture) + "\t" + MPChange.ToString("R", CultureInfo.InvariantCulture) + "\t" + Method + "\t" + Time + "\t" + Tick + "\t" + Amount.ToString("R", CultureInfo.InvariantCulture) + "\t" + Name + "\t" + Itemid + "\t" + ItemName + "\t" + Tooltip + "\t" + IconName.ToLowerInvariant();
        }
    }
}
