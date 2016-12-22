using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraDataExtractor
{
    class ParsedSkill
    {
        public ParsedSkill(string internalname,string skillid,string stype)
        {
            SkillId = skillid;
            sType = stype;
            string cut = internalname.ToLowerInvariant().Replace("_"," ");
            allmods=allmods.ConvertAll(x => x.ToLowerInvariant());
            modifiers = new List<string> { };
            bool _cut = true;
            while (_cut) {
                _cut = false;
                foreach (string mod in allmods)
                {
                    if ((mod == " blast") && cut.Contains("priest")) continue;// fix holy shot=holy blast
                    if ((mod == " combo") && !cut.Contains("assassin")) continue;// combo postfix - only for ninja
                    if ((mod == " jin")&&cut.Contains("sorcerer"))
                    {
                        if (cut.Remove(mod,out cut))
                        {
                            modifiers.Add(mod);
                            _cut = true;
                            break;
                        }
                    }
                    if (cut.RemoveFromEnd(mod, out cut))
                    {
                        modifiers.Add(mod);
                        _cut = true;
                        break;
                    }
                }
            }
            foreach (var lvl in levels)
            {
                if (cut.Contains(lvl.Key))
                {
                    cut = cut.Replace(lvl.Key,"");
                    Lvl = lvl.Value;
                    break;
                }
            }

            BaseName = cut;
            if (modifiers.Contains(" continuous") || modifiers.Contains(" large") || modifiers.Contains(" chain") || modifiers.Contains(" short") || modifiers.Contains(" shortairreaction"))
                { Chained = "True"; } else { Chained = "False"; }

            if (modifiers.Contains(" explosion") || modifiers.Contains(" explosionforbot") || modifiers.Contains(" realtargeting"))
                { Detail = "Boom"; }

            if (modifiers.Contains("01") && ((sType.Contains("combo") || modifiers.Contains(" shortairreaction")))&&(!BaseName.Contains("thunder drake")))
            {
                Detail = "hit 1";
            }

            if ((modifiers.Contains("02") || modifiers.Contains("03") || modifiers.Contains("04") || modifiers.Contains("05") || modifiers.Contains("06") || modifiers.Contains("07")
                || modifiers.Contains("08") || modifiers.Contains("09"))&&(!(modifiers.Contains(" projectile")||modifiers.Contains(" shot")) || (modifiers.Contains(" shortairreaction"))))

                { Detail=Detail+"hit "+modifiers.Find(x=>x.Contains("0")).Substring(1,1);}

            if (( modifiers.Contains("00") || modifiers.Contains("01")||modifiers.Contains("02") || modifiers.Contains("03") || modifiers.Contains("04") || modifiers.Contains("05") || modifiers.Contains("06") || modifiers.Contains("07")
                || modifiers.Contains("08") || modifiers.Contains("09")) && (modifiers.Contains(" shot"))&&(!modifiers.Contains(" projectile")) && (!modifiers.Contains(" cast")) && (!sType.Contains("capture"))&& (!BaseName.Contains("thunder drake")))

                { Detail = Detail + "hit " + (int.Parse(modifiers.Find(x => x.Contains("0")).Substring(1, 1))+1).ToString(); }

            if ((modifiers.Contains("00") || modifiers.Contains("01") || modifiers.Contains("02") || modifiers.Contains("03") || modifiers.Contains("04") || modifiers.Contains("05") || modifiers.Contains("06") || modifiers.Contains("07")
                || modifiers.Contains("08") || modifiers.Contains("09")) && (modifiers.Contains(" projectile")) && (!modifiers.Contains(" shot")))
            {
                if (BaseName.Contains("sorcerer"))
                {
                    if (!(skillid.EndsWith("0") && modifiers.Contains("01")))
                    {
                        Detail = "hit " + (int.Parse(modifiers.Find(x => x.Contains("0")).Substring(1, 1)) + 1).ToString();
                    }
                }
                if (BaseName.Contains("priest"))
                {
                    if (!(modifiers.Contains("00") || modifiers.Contains("01")))
                    {
                        Detail = "hit " + modifiers.Find(x => x.Contains("0")).Substring(1, 1);
                    }
                }
            }
            if ((modifiers.Contains("00") || modifiers.Contains("01") || modifiers.Contains("02") || modifiers.Contains("03") || modifiers.Contains("04") ) 
            && (((modifiers.Contains(" projectile")) && (modifiers.Contains(" shot")))||((modifiers.Contains(" cast")) && (modifiers.Contains(" shot")))) && (BaseName.Contains("priest")))
                { Detail = "hit " + modifiers.Find(x => x.Contains("0")).Substring(1, 1); }//holy shot

            if (((modifiers.Contains("00") || modifiers.Contains("01") || modifiers.Contains("02") || modifiers.Contains("03") || modifiers.Contains("04"))
            && modifiers.Contains(" shot")) && (BaseName.Contains("elementalist")) && (sType == "combo" || sType == "combo_instance"))
                { Detail = "hit " + modifiers.Find(x => x.Contains("0")).Substring(1, 1); }//Magic Arrow

            if (modifiers.Contains(" on"))
                { Detail = "On"; }

            if (modifiers.Contains(" off"))
                { Detail = "Off"; }

            if (modifiers.Contains(" superarmor"))
                { Detail = "SA"; }

            if (modifiers.Contains(" fury"))
                { Detail = "Fury"; }

            if (modifiers.Contains(" fail"))
                { Detail = "Dash fail"; }

            if (modifiers.Contains(" blast")&&(!BaseName.Contains("priest")))
                { Detail = "Blast"; }

            if (modifiers.Contains(" flying"))
                { Detail = "forward"; }

            if (modifiers.Contains(" return"))
                { Detail = "backward"; }

            if (modifiers.Contains(" stopping"))
                { Detail = "stopping"; }

            if (modifiers.Contains(" jin"))
                { Detail = "MB"; }

            if (modifiers.Contains(" for distortion"))
                { Detail = Detail + " FD"; }

            if (modifiers.Contains(" side"))
                { Detail = "Side " + Detail; }
        }
        public string BaseName { get; }
        private List<string> modifiers { get; }

        public string Chained { get; }
        public string SkillId { get; }
        public string Lvl { get; }
        private string sType { get; }

        public string Detail { get; } //hit number or other comment, such as "Explosion"

        private List<string> allmods = new List<string>{" "," Start","00","01","02","03","04","05","06","07","08","09","10"," Combo",
            " Continuous"," Cast"," large"," Projectile"," Projectile2"," Projectile3"," RealTargeting"," Flying"," Explosion"," ExplosionforBot",
            " PositionSwap"," Activate"," Invoke"," LockOn"," Charge"," Moving"," Shot"," OverShot"," ON"," OFF"," Attack"," Single",
            " Chain"," Connect"," Long"," Short"," Use"," FURY"," Evade"," False"," True"," Drain"," Cancel"," ShortAirReaction",
            " Change"," immediateCancel"," rearCancel"," Connector"," SuperArmor"," RangeTarget"," Loop"," forOnlyEffect",
            " forSummon"," forDamage", " forBot", " Side", " Fail", " Blast", " Return", " Stopping", " Jin", " For Distortion", " 실패", " 성공"};

        private static Dictionary<string, string> levels = new Dictionary<string, string>
        { {" lv01"," I"},{" lv02"," II"},{" lv03"," III"},{" lv04"," IV"},{" lv05"," V"},{" lv06"," VI"},{" lv07"," VII"},{" lv08"," VIII"},{" lv09"," IX"},{" lv10"," X"},
          {" lv11"," XI"},{" lv12"," XII"},{" lv13"," XIII"},{" lv14"," XIV"},{" lv15"," XV"},{" lv16"," XVI"},{" lv17"," XVII"},{" lv18"," XVIII"},{" lv19"," XIX"},{" lv20"," XX"}};

        //PREFIX 이펙트불필요 = "No effects needed" - glaiver skills, not sure what it's needed for, but now ignoring, since all such skills have explicit name in StrSheet_UserSkill
        //실패 = fail
        //성공 = success
        //01_Start = start chained multihit skills (connect = connectNextSkill=id)
        //01 02 03 04 etc = multihit combo hit number
        //Continuous = chained
        //01_Continuous = chained multihit combo
        //Jin = Overchannel active
        //For Distortion = atk+aspd buff active
        //Cast =
        //Cast_large = chained
        //Projectile 00 = when projectile generated from charged multihit (Shot 00) shot number -1 (sorcerer skillid ends on 0 while Projectile 01 = no multihit = ignore 01, Priest: projectile 02 = hit 2)
        //Projectile2 //can't find way to get to this skill with first way
        //Projectile3_RealTargeting //Explosion
        //Projectile_Flying = forward flying projectile
        //Projectile_Explosion
        //Projectile_ExplosionforBot
        //Projectile_forBot
        //PositionSwap
        //Activate= start counterattack
        //Invoke= invoke counterattack
        //Start 01 = skill or start projectile
        //Start 02 (type="evade") - second evade in a row without starting cooldown
        //LockOn
        //Charge
        //Moving Charge
        //Shot=release locked on/charged skill/dash
        //Shot 00=shot number-1 for multihit charges
        //OverShot = full charged shot, generating max number of profectiles
        //Shot_Projectile
        //OverShot_Projectile
        //ON= switch on
        //OFF= switch off 
        //Attack_Single = connect
        //Attack_Chain = chained
        //Attack Shot = dashredirected skill
        //Attack Connect = dashredirected skill
        //Long Start = connect
        //Short Start = chained
        //Long Cast = connect
        //Short Cast = chained
        //Long = connect
        //Short = chained
        //Attack
        //Roar
        //Single Use Start = connect
        //Chain Use Start = chained
        //FURY = abnormality connected skill, may be added after any berserk skill with all modifiers, before or after useless " 01" (both "_Short_Fury 01" and "_Short 01_FURY" are valid)
        //Evade 
        //False = connect counterattack
        //True = succsessful counterattack
        //Drain
        //Cancel
        //ShortAirReaction - chained multihit
        //Change = type="change"
        //immediateCancel
        //rearCancel
        //Connector=type="connect"
        //SuperArmor = same as FURY for brawler
        //RangeTarget = lockon
        //Loop = repeatable skill (type=combo_instance, nextskill = itself)
        //SuperRocketJump_Continuous - chained rocketjump, (changed internal name, but exist in StrSheet_UserSkill.xml => may be ignored)
        //forOnlyEffect = projectile
        //Projectile_forSummon
        //Projectile_forDamage 
        //Side Ninja side attack
        //Fail Ninja Dash fail
        //Blast Ninja Blast projectile
        //Flying projectile Flying forward
        //Return Ninja projectile Flying backward
        //Stopping Ninga stopping projectile
    }
}
