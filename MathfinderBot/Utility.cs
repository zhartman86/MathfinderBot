using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using GroupDocs.Parser;
using GroupDocs.Parser.Data;

using Gellybeans.Pathfinder;


namespace MathfinderBot
{
    public static class Utility
    {
        public static StatBlock ParsePDF(Stream stream, StatBlock stats)
        {
            using var parser = new Parser(stream);
            Console.WriteLine("Parse created");

            var data = parser.ParseForm();
            Console.WriteLine("Data parsed");

            var map = new Dictionary<string, string>();
            
            
            for(int i = 0; i < data.Count; i++)
                map[data[i].Name] = data[i].Text;


            stats.CharacterName     = map["CHARNAME"];

            stats.Info["DEITY"]     = map["DEITY"];
            stats.Info["ALIGNMENT"] = map["ALIGMENT"];
            stats.Info["RACE"]      = map["RACE"];
            stats.Info["HOMELAND"]  = map["HOMELAND"];
            stats.Info["SIZE"]      = map["SIZE"];
            stats.Info["GENDER"]    = map["GENDER"];
            stats.Info["AGE"]       = map["AGE"];
            stats.Info["HEIGHT"]    = map["HEIGHT"];
            stats.Info["WEIGHT"]    = map["WEIGHT"];
            stats.Info["HAIR"]      = map["HAIR"];
            stats.Info["EYES"]      = map["EYES"];

            var outVal = 0;

            stats["STR_SCORE"] = int.TryParse(map["ABILITYBASE0"], out outVal) ? outVal : 0;
            stats["DEX_SCORE"] = int.TryParse(map["ABILITYBASE1"], out outVal) ? outVal : 0;
            stats["CON_SCORE"] = int.TryParse(map["ABILITYBASE2"], out outVal) ? outVal : 0;
            stats["INT_SCORE"] = int.TryParse(map["ABILITYBASE3"], out outVal) ? outVal : 0;
            stats["WIS_SCORE"] = int.TryParse(map["ABILITYBASE4"], out outVal) ? outVal : 0;
            stats["CHA_SCORE"] = int.TryParse(map["ABILITYBASE5"], out outVal) ? outVal : 0;


            var matches = Regex.Matches(map["CHARLEVEL"], @"([0-9]{1,2})");

            int lvls = 0;
            foreach(Match m in matches)
                lvls += int.Parse(m.Value);


            var level       = lvls;
            var hp          = int.Parse(map["HITPOINTS"]);
            var conMod      = (stats.Stats["CON_SCORE"] - 10) / 2;
            
            stats["LEVEL"]      = lvls;
            stats["HP_BASE"]    = hp - (level * conMod);
            
            stats["INIT_BONUS"]   = int.TryParse(map["INITMISC"],     out outVal) ? outVal : 0;
            stats["FORT_BONUS"]   = int.TryParse(map["FORTMISC"],     out outVal) ? outVal : 0;
            stats["REF_BONUS"]    = int.TryParse(map["REFLEXMISC"],   out outVal) ? outVal : 0;
            stats["WILL_BONUS"]   = int.TryParse(map["WILLMISC"],     out outVal) ? outVal : 0;


            stats["SK_ACR"] = int.TryParse(map["ACROBATICSRANKS"],                  out outVal) ? outVal : 0;
            stats.Stats["SK_ACR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["ACROBATICSMISC"], out outVal) ? outVal : 0 });
            
            stats["SK_APR"] = int.TryParse(map["APPRAISERANKS"],                    out outVal) ? outVal : 0;
            stats.Stats["SK_APR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["APPRAISEMISC"], out outVal) ? outVal : 0 });

            stats["SK_BLF"] = int.TryParse(map["BLUFFRANKS"],                       out outVal) ? outVal : 0;
            stats.Stats["SK_BLF"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["BLUFFMISC"], out outVal) ? outVal : 0 });

            stats["SK_CLM"] = int.TryParse(map["CLIMBRANKS"],                       out outVal) ? outVal : 0;
            stats.Stats["SK_CLM"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["CLIMBMISC"], out outVal) ? outVal : 0 });

            stats["SK_DIP"] = int.TryParse(map["DIPLOMACYRANKS"],                   out outVal) ? outVal : 0;
            stats.Stats["SK_DIP"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["DIPLOMACYMISC"], out outVal) ? outVal : 0 });

            stats["SK_DSA"] = int.TryParse(map["DISABLE DEVICERANKS"],              out outVal) ? outVal : 0;
            stats.Stats["SK_DSA"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["DISABLE DEVICEMISC"], out outVal) ? outVal : 0 });

            stats["SK_DSG"] = int.TryParse(map["DISGUISERANKS"],                    out outVal) ? outVal : 0;
            stats.Stats["SK_DSG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["DISGUISEMISC"], out outVal) ? outVal : 0 });

            stats["SK_ESC"] = int.TryParse(map["ESCAPE ARTISTRANKS"],               out outVal) ? outVal : 0;
            stats.Stats["SK_ESC"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["ESCAPE ARTISTMISC"], out outVal) ? outVal : 0 });

            stats["SK_FLY"] = int.TryParse(map["FLYRANKS"],                         out outVal) ? outVal : 0;
            stats.Stats["SK_FLY"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["FLYMISC"], out outVal) ? outVal : 0 });

            stats["SK_HND"] = int.TryParse(map["HANDLE ANIMALRANKS"],               out outVal) ? outVal : 0;
            stats.Stats["SK_HND"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["HANDLE ANIMALMISC"], out outVal) ? outVal : 0 });

            stats["SK_HEA"] = int.TryParse(map["HEALRANKS"],                        out outVal) ? outVal : 0;
            stats.Stats["SK_HEA"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["HEALMISC"], out outVal) ? outVal : 0 });

            stats["SK_ITM"] = int.TryParse(map["INTIMIDATERANKS"],                  out outVal) ? outVal : 0;
            stats.Stats["SK_ITM"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["INTIMIDATEMISC"], out outVal) ? outVal : 0 });

            stats["SK_ARC"] = int.TryParse(map["KNOWLEDGE (ARCANA)RANKS"],          out outVal) ? outVal : 0;
            stats.Stats["SK_ARC"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (ARCANA)MISC"], out outVal) ? outVal : 0 });

            stats["SK_DUN"] = int.TryParse(map["KNOWLEDGE (DUNGEONEERING)RANKS"],   out outVal) ? outVal : 0;
            stats.Stats["SK_DUN"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (DUNGEONEERING)MISC"], out outVal) ? outVal : 0 });

            stats["SK_ENG"] = int.TryParse(map["KNOWLEDGE (ENGINEERING)RANKS"],     out outVal) ? outVal : 0;
            stats.Stats["SK_ENG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (ENGINEERING)MISC"], out outVal) ? outVal : 0 });

            stats["SK_GEO"] = int.TryParse(map["KNOWLEDGE (GEOGRAPHY)RANKS"],       out outVal) ? outVal : 0;
            stats.Stats["SK_GEO"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (GEOGRAPHY)MISC"], out outVal) ? outVal : 0 });

            stats["SK_HIS"] = int.TryParse(map["KNOWLEDGE (HISTORY)RANKS"],         out outVal) ? outVal : 0;
            stats.Stats["SK_HIS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (HISTORY)MISC"], out outVal) ? outVal : 0 });

            stats["SK_LCL"] = int.TryParse(map["KNOWLEDGE (LOCAL)RANKS"],           out outVal) ? outVal : 0;
            stats.Stats["SK_LCL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (LOCAL)MISC"], out outVal) ? outVal : 0 });

            stats["SK_NTR"] = int.TryParse(map["KNOWLEDGE (NATURE)RANKS"],          out outVal) ? outVal : 0;
            stats.Stats["SK_NTR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (NATURE)MISC"], out outVal) ? outVal : 0 });

            stats["SK_NBL"] = int.TryParse(map["KNOWLEDGE (NOBILITY)RANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_NBL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (NOBILITY)MISC"], out outVal) ? outVal : 0 });

            stats["SK_PLN"] = int.TryParse(map["KNOWLEDGE (PLANES)RANKS"],          out outVal) ? outVal : 0;
            stats.Stats["SK_PLN"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (PLANES)MISC"], out outVal) ? outVal : 0 });

            stats["SK_RLG"] = int.TryParse(map["KNOWLEDGE (RELIGION)RANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_RLG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (RELIGION)MISC"], out outVal) ? outVal : 0 });

            stats["SK_LNG"] = int.TryParse(map["LINGUISTICSRANKS"],                 out outVal) ? outVal : 0;
            stats.Stats["SK_LNG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["LINGUISTICSMISC"], out outVal) ? outVal : 0 });

            stats["SK_PRC"] = int.TryParse(map["PERCEPTIONRANKS"],                  out outVal) ? outVal : 0;
            stats.Stats["SK_PRC"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["PERCEPTIONMISC"], out outVal) ? outVal : 0 });

            stats["SK_RDE"] = int.TryParse(map["RIDERANKS"],                        out outVal) ? outVal : 0;
            stats.Stats["SK_RDE"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["RIDEMISC"], out outVal) ? outVal : 0 });

            stats["SK_SNS"] = int.TryParse(map["SENSE MOTIVERANKS"],                out outVal) ? outVal : 0;
            stats.Stats["SK_SNS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SENSE MOTIVEMISC"], out outVal) ? outVal : 0 });

            stats["SK_SLT"] = int.TryParse(map["SLEIGHT OF HANDRANKS"],             out outVal) ? outVal : 0;
            stats.Stats["SK_SLT"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SLEIGHT OF HANDMISC"], out outVal) ? outVal : 0 });

            stats["SK_SPL"] = int.TryParse(map["SPELLCRAFTRANKS"],                  out outVal) ? outVal : 0;
            stats.Stats["SK_SPL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SPELLCRAFTMISC"], out outVal) ? outVal : 0 });

            stats["SK_STL"] = int.TryParse(map["STEALTHRANKS"],                     out outVal) ? outVal : 0;
            stats.Stats["SK_STL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["STEALTHMISC"], out outVal) ? outVal : 0 });

            stats["SK_SUR"] = int.TryParse(map["SURVIVALRANKS"],                    out outVal) ? outVal : 0;
            stats.Stats["SK_SUR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SURVIVALMISC"], out outVal) ? outVal : 0 });

            stats["SK_SWM"] = int.TryParse(map["SWIMRANKS"],                        out outVal) ? outVal : 0;
            stats.Stats["SK_SWM"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SWIMMISC"], out outVal) ? outVal : 0 });

            stats["SK_UMD"] = int.TryParse(map["USE MAGIC DEVICERANKS"],            out outVal) ? outVal : 0;
            stats.Stats["SK_UMD"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["USE MAGIC DEVICEMISC"], out outVal) ? outVal : 0 });

            return stats;
        }
    }
}
