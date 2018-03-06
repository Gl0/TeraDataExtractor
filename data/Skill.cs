using System;

namespace TeraDataExtractor
{
    public class Skill : IComparable<Skill>
    {
        public Skill(string id, string race, string gender,string pclass,string name,string iconName="")
        {
            Id = id;
            Race = race;
            Gender = gender;
            PClass = pclass;
            Name = name;
            IconName = iconName;
        }
        public Skill(string id, string race, string gender, string pclass, string name, string chained, string detail, string iconName)
        {
            Id = id;
            Race = race;
            Gender = gender;
            PClass = pclass;
            Name = name;
            Chained = chained;
            Detail = detail;
            IconName = PClass == "Common" && Race == "Common" ? "" : iconName;
        }

        public string Race { get; }
        public string Gender { get; }
        public string PClass { get; }
        public string Id { get; }
        public string Name { get; set; }
        public string Chained { get; set; }
        public string Detail { get; set; }
        public string IconName { get; set; }
        public Skill Parent { get; set; } //1st way - counting hits 
        public string hitnum(Skill skill)
        {//1st way - counting hits 
            int hit = 0;
            while (skill.Parent!=null) {
                hit+=1;
                skill = skill.Parent;
            }
            return (hit==0)?"":(hit+1).ToString()+" hit";
        }
        public override int GetHashCode() {
            return (Race + PClass + Id).GetHashCode();
        }
        public static bool operator ==(Skill x, Skill y) {
            if ((object)y == null)
            {
                return false;
            }
            return x.Equals(y);
        }
        public static bool operator !=(Skill x, Skill y)
        {
            return !(x == y);
        }
        public string toCSV() { return Id + ";" + Race + ";" + Gender + ";" + PClass + ";" + Name+ ";"+ Chained+";"+Detail; }
        public string toTSV() { return Id + "\t" + Race + "\t" + Gender + "\t" + PClass + "\t" + Name +"\t"+Chained+"\t"+Detail+"\t"+IconName.ToLowerInvariant(); }
        public string toSSV() { return Id + " " + Race + " " + Gender + " " + PClass + " " + Name; }

        public bool Equals(Skill y)
        {
            if ((object)y == null)
            {
                return false;
            }
            return (Race == y.Race) && (PClass == y.PClass) && (Id == y.Id);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            Skill p = obj as Skill;
            if ((System.Object)p == null)
            {
                return false;
            }
            return this.Equals(p);
        }
        public override string ToString()
        {
            return toCSV();
        }

        public int CompareTo(Skill y)
        {
            if (y == null) return 1;
            return (PClass == y.PClass) ? int.Parse(Id).CompareTo(int.Parse(y.Id)) : (y.PClass == "Common") ? -1 : (PClass == "Common") ? 1 : string.Compare(PClass, y.PClass);
        }
    }

    public class PetSkill : IComparable<PetSkill>
    {
        public PetSkill(string huntingZoneId, string template, string petName, string id, string name, string iconName = "")
        {
            HZ = huntingZoneId;
            Template = template;
            PetName = petName;
            Id = id;
            Name = name;
            IconName = iconName;
        }

        public string HZ { get; }
        public string Template { get; }
        public string PetName { get; }
        public string Id { get; }
        public string Name { get; set; }
        public string IconName { get; set; }
        public Skill Parent { get; set; } //1st way
        public override int GetHashCode()
        {
            return (HZ + Template + Id).GetHashCode();
        }
        public static bool operator ==(PetSkill x, PetSkill y)
        {
            if ((object)y == null)
            {
                return false;
            }
            return x.Equals(y);
        }
        public static bool operator !=(PetSkill x, PetSkill y)
        {
            return !(x == y);
        }
        public string toTSV() { return HZ + "\t" + Template + "\t" + PetName + "\t" + Id + "\t" + Name + "\t" + IconName.ToLowerInvariant(); }
        public string toCSV() { return HZ + ";" + Template + ";" + PetName + ";" + Id + ";" + Name + ";" + IconName.ToLowerInvariant(); }

        public bool Equals(PetSkill y)
        {
            if ((object)y == null)
            {
                return false;
            }
            return (HZ == y.HZ) && (Template == y.Template) && (Id == y.Id);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            PetSkill p = obj as PetSkill;
            if ((System.Object)p == null)
            {
                return false;
            }
            return this.Equals(p);
        }
        public override string ToString()
        {
            return toCSV();
        }

        public int CompareTo(PetSkill y)
        {
            if (y == null) return 1;
            return HZ == y.HZ && Template==y.Template ? int.Parse(Id).CompareTo(int.Parse(y.Id)) : (HZ == y.HZ) ? int.Parse(Template).CompareTo(int.Parse(y.Template)) : int.Parse(HZ).CompareTo(int.Parse(y.HZ));
        }
    }

}
