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
        public static StatBlock ParsePDF()
        {
            using var parser = new Parser(@"C:/Users/zach/Downloads/charactersheet.pdf");
            Console.WriteLine("Parse created");

            var data = parser.ParseForm();
            Console.WriteLine("Data parsed");

            var map = new Dictionary<string, string>();
            
            
            for(int i = 0; i < data.Count; i++)
            {
                Console.WriteLine($"{data[i].Name}: {data[i].Text}");
                map[data[i].Name] = data[i].Text;
            }
            var stats = StatBlock.DefaultPathfinder(map["CHARNAME"]);

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

            stats.Stats["STR_SCORE"] = int.TryParse(map["ABILITYBASE0"], out outVal) ? outVal : 0;
            stats.Stats["DEX_SCORE"] = int.TryParse(map["ABILITYBASE1"], out outVal) ? outVal : 0;
            stats.Stats["CON_SCORE"] = int.TryParse(map["ABILITYBASE2"], out outVal) ? outVal : 0;
            stats.Stats["INT_SCORE"] = int.TryParse(map["ABILITYBASE3"], out outVal) ? outVal : 0;
            stats.Stats["WIS_SCORE"] = int.TryParse(map["ABILITYBASE4"], out outVal) ? outVal : 0;
            stats.Stats["CHA_SCORE"] = int.TryParse(map["ABILITYBASE5"], out outVal) ? outVal : 0;

            var level       = int.Parse(Regex.Match(map["CHARLEVEL"], @"[0-9]{1,2}").Value);
            var hp          = int.Parse(map["HITPOINTS"]);
            var conMod      = (stats.Stats["CON_SCORE"] - 10 / 2);
            
            stats.Stats["HP_BASE"]      = hp - (level * conMod);
            
            stats.Stats["INIT_BONUS"]    = int.TryParse(map["INITMISC"],     out outVal) ? outVal : 0;
            stats.Stats["FORT_BONUS"]   = int.TryParse(map["FORTMISC"],     out outVal) ? outVal : 0;
            stats.Stats["REF_BONUS"]    = int.TryParse(map["REFLEXMISC"],   out outVal) ? outVal : 0;
            stats.Stats["WILL_BONUS"]   = int.TryParse(map["WILLMISC"],     out outVal) ? outVal : 0;


            stats.Stats["SK_ACR"] = int.TryParse(map["ACROBATICSRANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_APR"] = int.TryParse(map["APPRAISERANKS"],          out outVal) ? outVal : 0;
            stats.Stats["SK_BLF"] = int.TryParse(map["BLUFFRANKS"],             out outVal) ? outVal : 0;
            stats.Stats["SK_CLM"] = int.TryParse(map["CLIMBRANKS"],             out outVal) ? outVal : 0;
            stats.Stats["SK_DIP"] = int.TryParse(map["DIPLOMACYRANKS"],         out outVal) ? outVal : 0;
            stats.Stats["SK_DSA"] = int.TryParse(map["DISABLE DEVICERANKS"],    out outVal) ? outVal : 0;
            stats.Stats["SK_DSG"] = int.TryParse(map["DISGUISERANKS"],          out outVal) ? outVal : 0;
            stats.Stats["SK_ESC"] = int.TryParse(map["ESCAPE ARTISTRANKS"],     out outVal) ? outVal : 0;
            stats.Stats["SK_FLY"] = int.TryParse(map["FLYRANKS"],               out outVal) ? outVal : 0;
            stats.Stats["SK_HND"] = int.TryParse(map["HANDLE ANIMALRANKS"],     out outVal) ? outVal : 0;
            stats.Stats["SK_HEA"] = int.TryParse(map["HEALRANKS"],              out outVal) ? outVal : 0;
            stats.Stats["SK_ITM"] = int.TryParse(map["INTIMIDATERANKS"],        out outVal) ? outVal : 0;

            stats.Stats["SK_ARC"] = int.TryParse(map["KNOWLEDGE (ARCANA)RANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_DUN"] = int.TryParse(map["KNOWLEDGE (DUNGEONEERING)RANKS"], out outVal) ? outVal : 0;
            stats.Stats["SK_ENG"] = int.TryParse(map["KNOWLEDGE (ENGINEERING)RANKS"],   out outVal) ? outVal : 0;
            stats.Stats["SK_GEO"] = int.TryParse(map["KNOWLEDGE (GEOGRAPHY)RANKS"],     out outVal) ? outVal : 0;
            stats.Stats["SK_HIS"] = int.TryParse(map["KNOWLEDGE (HISTORY)RANKS"],       out outVal) ? outVal : 0;
            stats.Stats["SK_LCL"] = int.TryParse(map["KNOWLEDGE (LOCAL)RANKS"],         out outVal) ? outVal : 0;
            stats.Stats["SK_NTR"] = int.TryParse(map["KNOWLEDGE (NATURE)RANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_NBL"] = int.TryParse(map["KNOWLEDGE (NOBILITY)RANKS"],      out outVal) ? outVal : 0;
            stats.Stats["SK_PLN"] = int.TryParse(map["KNOWLEDGE (PLANES)RANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_RLG"] = int.TryParse(map["KNOWLEDGE (RELIGION)RANKS"],      out outVal) ? outVal : 0;

            stats.Stats["SK_LNG"] = int.TryParse(map["LINGUISTICSRANKS"],       out outVal) ? outVal : 0;
            stats.Stats["SK_PRC"] = int.TryParse(map["PERCEPTIONRANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_RDE"] = int.TryParse(map["RIDERANKS"],              out outVal) ? outVal : 0;
            stats.Stats["SK_SNS"] = int.TryParse(map["SENSE MOTIVERANKS"],      out outVal) ? outVal : 0;
            stats.Stats["SK_SLT"] = int.TryParse(map["SLEIGHT OF HANDRANKS"],   out outVal) ? outVal : 0;
            stats.Stats["SK_SPL"] = int.TryParse(map["SPELLCRAFTRANKS"],        out outVal) ? outVal : 0;
            stats.Stats["SK_STL"] = int.TryParse(map["STEALTHRANKS"],           out outVal) ? outVal : 0;
            stats.Stats["SK_SUR"] = int.TryParse(map["SURVIVALRANKS"],          out outVal) ? outVal : 0;
            stats.Stats["SK_SWM"] = int.TryParse(map["SWIMRANKS"],              out outVal) ? outVal : 0;
            stats.Stats["SK_UMD"] = int.TryParse(map["USE MAGIC DEVICERANKS"],  out outVal) ? outVal : 0;


            return stats;
        }
    }
}
