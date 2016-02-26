using System;

namespace TeraDataExtractor
{
    public class Skill : IComparable<Skill>
    {
        public Skill(string id, string race, string gender,string pclass,string name)
        {
            Id = id;
            Race = race;
            Gender = gender;
            PClass = pclass;
            Name = name;
        }
        public Skill(string id, string race, string gender, string pclass, string name, string chained, string detail)
        {
            Id = id;
            Race = race;
            Gender = gender;
            PClass = pclass;
            Name = name;
            Chained = chained;
            Detail = detail;
        }

        public string Race { get; }
        public string Gender { get; }
        public string PClass { get; }
        public string Id { get; }
        public string Name { get; set; }
        public string Chained { get; set; }
        public string Detail { get; set; }
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
        public string toTSV() { return Id + "\t" + Race + "\t" + Gender + "\t" + PClass + "\t" + Name +"\t"+Chained+"\t"+Detail ; }
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
}
