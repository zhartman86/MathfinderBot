using System.Text.RegularExpressions;
using System.Xml;
using GroupDocs.Parser;
using Gellybeans.Pathfinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using MongoDB.Bson;

namespace MathfinderBot
{
    public static class Utility
    {
                   
        public static string GetPathfinderQuick(StatBlock stats)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"STR:{stats["STR_SCORE"]}[{Gellybeans.Expressions.Parser.Parse(stats.Expressions["STR"]).Eval(stats, null)}], DEX:{stats["DEX_SCORE"]}[{Gellybeans.Expressions.Parser.Parse(stats.Expressions["DEX"]).Eval(stats, null)}], CON:{stats["CON_SCORE"]}[{Gellybeans.Expressions.Parser.Parse(stats.Expressions["CON"]).Eval(stats, null)}], INT:{stats["INT_SCORE"]}[{Gellybeans.Expressions.Parser.Parse(stats.Expressions["INT"]).Eval(stats, null)}], WIS:{stats["WIS_SCORE"]}[{Gellybeans.Expressions.Parser.Parse(stats.Expressions["WIS"]).Eval(stats, null)}], CHA:{stats["CHA_SCORE"]}[{Gellybeans.Expressions.Parser.Parse(stats.Expressions["CHA"]).Eval(stats, null)}]");
            sb.AppendLine($"AC:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["AC"]).Eval(stats, null)}[T:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["TOUCH"]).Eval(stats, null)}, FF:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["FLAT"]).Eval(stats, null)}]");
            sb.AppendLine($"CMB:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["CMB"]).Eval(stats, null)}, CMD:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["CMD"]).Eval(stats, null)}");
            sb.AppendLine($"FORT:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["FORT"]).Eval(stats, null)}, REF:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["REF"]).Eval(stats, null)}, WILL:{Gellybeans.Expressions.Parser.Parse(stats.Expressions["WILL"]).Eval(stats, null)}");

            return sb.ToString();
        }


        //PATHFINDER UTILS              
        public static StatBlock UpdateWithPathbuilder(Stream stream, StatBlock stats)
        {
            stats.ClearBonuses();
            using var parser = new Parser(stream);
            
            Console.WriteLine("Parsing Pathbuilder pdf...");
            var data = parser.ParseForm();          
            if(data == null)
                return null;
            
            Console.WriteLine("Forms parsed.");
            Console.WriteLine("");
            Console.WriteLine("");

            var map = new Dictionary<string, string>();                    
            for(int i = 0; i < data.Count; i++)
                map[data[i].Name] = data[i].Text;

            if(!map.ContainsKey("CHARNAME"))
                return null;

            stats.CharacterName     = map["CHARNAME"];

            Console.WriteLine($"Parsing {stats.CharacterName}...");
            stats.Info["LEVELS"]    = map["CHARLEVEL"];
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

            Console.WriteLine("size...");
            switch(stats.Info["SIZE"])
            {
                case "Fine":
                    stats.Stats["SIZE_MOD"] = 8;
                    stats.Stats["SIZE_SKL"] = 8;
                    break;
                case "Diminutive":
                    stats.Stats["SIZE_MOD"] = 4;
                    stats.Stats["SIZE_SKL"] = 6;
                    break;
                case "Tiny":
                    stats.Stats["SIZE_MOD"] = 2;
                    stats.Stats["SIZE_SKL"] = 4;
                    break;
                case "Small":
                    stats.Stats["SIZE_MOD"] = 1;
                    stats.Stats["SIZE_SKL"] = 2;
                    break;
                case "Medium":
                    stats.Stats["SIZE_MOD"] = 0;
                    stats.Stats["SIZE_SKL"] = 0;
                    break;
                case "Large":
                    stats.Stats["SIZE_MOD"] = -1;
                    stats.Stats["SIZE_SKL"] = -2;
                    break;
                case "Huge":
                    stats.Stats["SIZE_MOD"] = -2;
                    stats.Stats["SIZE_SKL"] = -4;
                    break;
                case "Gargantuan":
                    stats.Stats["SIZE_MOD"] = -4;
                    stats.Stats["SIZE_SKL"] = -6;
                    break;
                case "Colossal":
                    stats.Stats["SIZE_MOD"] = -8;
                    stats.Stats["SIZE_SKL"] = -8;
                    break;
            }

            Console.WriteLine("scores...");
            stats["STR_SCORE"] = int.TryParse(map["ABILITYBASE0"], out outVal) ? outVal : 0;
            stats["DEX_SCORE"] = int.TryParse(map["ABILITYBASE1"], out outVal) ? outVal : 0;
            stats["CON_SCORE"] = int.TryParse(map["ABILITYBASE2"], out outVal) ? outVal : 0;
            stats["INT_SCORE"] = int.TryParse(map["ABILITYBASE3"], out outVal) ? outVal : 0;
            stats["WIS_SCORE"] = int.TryParse(map["ABILITYBASE4"], out outVal) ? outVal : 0;
            stats["CHA_SCORE"] = int.TryParse(map["ABILITYBASE5"], out outVal) ? outVal : 0;

            Console.WriteLine("levels...");
            var matches = Regex.Matches(map["CHARLEVEL"], @"([0-9]{1,2})");

            int lvls = 0;
            foreach(Match m in matches)
                lvls += int.Parse(m.Value);

            var level       = lvls;
            var hp          = int.Parse(map["HITPOINTS"]);
            var conMod      = (stats.Stats["CON_SCORE"] - 10) / 2;
            
            stats["LEVEL"]      = lvls;
            stats["HP_BASE"]    = hp - (level * conMod);

            Console.WriteLine("bab, cmb, cmd, saves, ac...");
            stats["BAB"]            = int.TryParse(map["BAB"],      out outVal) ? outVal : 0;
            stats["INIT_BONUS"]     = int.TryParse(map["INITMISC"], out outVal) ? outVal : 0;

            stats["CMB_BONUS"] = 0;
            stats.Stats["CMB_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["CMBMISC"], out outVal) ? outVal : 0 });
            
            stats["CMD_BONUS"] = 0;
            stats.Stats["CMD_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["CMDMISC"], out outVal) ? outVal : 0 });

            stats["FORT_BONUS"]   = int.TryParse(map["FORTBASE"],     out outVal) ? outVal : 0;
            stats.Stats["FORT_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["FORTMISC"], out outVal) ? outVal : 0 });
            stats.Stats["FORT_BONUS"].AddBonus(new Bonus { Name = "MAGIC", Type = BonusType.Resistance, Value = int.TryParse(map["FORTMAGIC"], out outVal) ? outVal : 0 });
            
            stats["REF_BONUS"]    = int.TryParse(map["REFLEXBASE"],   out outVal) ? outVal : 0;
            stats.Stats["REF_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["REFLEXMISC"], out outVal) ? outVal : 0 });
            stats.Stats["REF_BONUS"].AddBonus(new Bonus { Name = "MAGIC", Type = BonusType.Resistance, Value = int.TryParse(map["REFLEXMAGIC"], out outVal) ? outVal : 0 });
            
            stats["WILL_BONUS"]   = int.TryParse(map["WILLBASE"],     out outVal) ? outVal : 0;
            stats.Stats["WILL_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["WILLMISC"], out outVal) ? outVal : 0 });
            stats.Stats["WILL_BONUS"].AddBonus(new Bonus { Name = "MAGIC", Type = BonusType.Resistance, Value = int.TryParse(map["WILLMAGIC"], out outVal) ? outVal : 0 });

            stats["AC_BONUS"] = 10;
            stats.Stats["AC_BONUS"].AddBonus(new Bonus { Name = "ARMOR", Type = BonusType.Armor,            Value = int.TryParse(map["ACARMOR"], out outVal) ? outVal : 0 });
            stats.Stats["AC_BONUS"].AddBonus(new Bonus { Name = "SHIELD", Type = BonusType.Shield,          Value = int.TryParse(map["ACSHIELD"], out outVal) ? outVal : 0 });
            stats.Stats["AC_BONUS"].AddBonus(new Bonus { Name = "NATURAL", Type = BonusType.Natural,        Value = int.TryParse(map["ACNATURAL"], out outVal) ? outVal : 0 });
            stats.Stats["AC_BONUS"].AddBonus(new Bonus { Name = "DEFLECTION", Type = BonusType.Deflection,  Value = int.TryParse(map["ACDEFLECTION"], out outVal) ? outVal : 0 });
            stats.Stats["AC_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless,          Value = int.TryParse(map["ACMISC"], out outVal) ? outVal : 0 });

            stats["AC_PENALTY"] = int.TryParse(map["ARMORPENALTY0"],    out outVal) ? outVal : 0;
            
            //this isnt exactly accurate, but it should work?
            stats["AC_MAXDEX"]  = int.TryParse(map["ACDEX"],            out outVal) ? outVal : 99;

            Console.WriteLine("skills...");
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

            
            Console.WriteLine("misc...");
            stats["PP"] = int.TryParse(map["PP"], out outVal) ? outVal : 0;
            stats["GP"] = int.TryParse(map["GP"], out outVal) ? outVal : 0;
            stats["SP"] = int.TryParse(map["SP"], out outVal) ? outVal : 0;
            stats["CP"] = int.TryParse(map["CP"], out outVal) ? outVal : 0;


            Console.WriteLine("weapons...");
            for(int i = 0; i < 5; i++)
            {
                if(!string.IsNullOrEmpty(map[$"WEAPONNAME{i}"]))
                {
                    stats.ExprRows[map[$"WEAPONNAME{i}"].ToUpper()] = new ExprRow()
                    {
                        RowName = map[$"WEAPONNAME{i}"].ToUpper(),
                        Set = new List<Expr>()
                        {
                            new Expr()
                            {
                                Name = "HIT",
                                Expression = $"1d20+{map[$"WEAPONATTACK{i}"].ToUpper()}"
                            },
                            new Expr()
                            {
                                Name = "DMG",
                                Expression = map[$"WEAPONDAMAGE{i}"].ToUpper()
                            },
                            new Expr()
                            {
                                Name = "CRT",
                                Expression = map[$"WEAPONCRITICAL{i}"].ToUpper()
                            }
                        }
                    };
                }
            }
            return stats;
        }

        public static StatBlock UpdateWithHeroLabs(Stream stream, StatBlock stats)
        {
            stats.ClearBonuses();
            var doc = new XmlDocument();
            doc.Load(stream);                  
            
            var outVal = 0;
            var elements = doc.GetElementsByTagName("size");

            stats.Stats["SIZE"] = int.TryParse(elements[0].Attributes["name"].Value, out outVal) ? outVal : 0;

            Console.WriteLine("Setting size...");
            switch(stats.Info["SIZE"])
            {
                case "Fine":
                    stats.Stats["SIZE_MOD"] = 8;
                    stats.Stats["SIZE_SKL"] = 8;
                    break;
                case "Diminutive":
                    stats.Stats["SIZE_MOD"] = 4;
                    stats.Stats["SIZE_SKL"] = 6;
                    break;
                case "Tiny":
                    stats.Stats["SIZE_MOD"] = 2;
                    stats.Stats["SIZE_SKL"] = 4;
                    break;
                case "Small":
                    stats.Stats["SIZE_MOD"] = 1;
                    stats.Stats["SIZE_SKL"] = 2;
                    break;
                case "Medium":
                    stats.Stats["SIZE_MOD"] = 0;
                    stats.Stats["SIZE_SKL"] = 0;
                    break;
                case "Large":
                    stats.Stats["SIZE_MOD"] = -1;
                    stats.Stats["SIZE_SKL"] = -2;
                    break;
                case "Huge":
                    stats.Stats["SIZE_MOD"] = -2;
                    stats.Stats["SIZE_SKL"] = -4;
                    break;
                case "Gargantuan":
                    stats.Stats["SIZE_MOD"] = -4;
                    stats.Stats["SIZE_SKL"] = -6;
                    break;
                case "Colossal":
                    stats.Stats["SIZE_MOD"] = -8;
                    stats.Stats["SIZE_SKL"] = -8;
                    break;
            }


            Console.WriteLine("Setting scores...");
            elements = doc.GetElementsByTagName("attrvalue");
            var eStats = new string[6] { "STR_SCORE", "DEX_SCORE", "CON_SCORE", "INT_SCORE", "WIS_SCORE", "CHA_SCORE" };
            for(int i = 0; i < eStats.Length; i++)
            {
                var split = elements[i].Attributes["text"].Value.Split('/');
                stats.Stats[eStats[i]] = int.Parse(split[0]);
                if(split.Length > 1)
                    stats.Stats[eStats[i]].AddBonus(new Bonus() { Name = "ENH_BONUS", Type = BonusType.Enhancement, Value = int.Parse(split[1]) - int.Parse(split[0]) });
            }

            Console.WriteLine("Setting level...");
            elements = doc.GetElementsByTagName("classes");
            stats.Stats["LEVEL"] = int.TryParse(elements[0].Attributes["level"].Value, out outVal) ?  outVal : 0;

            Console.WriteLine("Setting hp...");
            elements = doc.GetElementsByTagName("health");
            var hpTotal = int.TryParse(elements[0].Attributes["hitpoints"].Value, out outVal) ? outVal : 0;
            stats.Stats["HP_BASE"] = hpTotal - (stats.Stats["LEVEL"] * ((stats.Stats["CON_SCORE"] - 10) / 2));

            elements = doc.GetElementsByTagName("attack");
            stats.Stats["BAB"] = int.TryParse(elements[0].Attributes["baseattack"].Value, out outVal) ? outVal : 0;

            Console.WriteLine("Setting coin...");
            elements = doc.GetElementsByTagName("money");
            stats.Stats["PP"] = int.TryParse(elements[0].Attributes["pp"].Value, out outVal) ? outVal : 0;
            stats.Stats["GP"] = int.TryParse(elements[0].Attributes["gp"].Value, out outVal) ? outVal : 0;
            stats.Stats["SP"] = int.TryParse(elements[0].Attributes["sp"].Value, out outVal) ? outVal : 0;
            stats.Stats["CP"] = int.TryParse(elements[0].Attributes["cp"].Value, out outVal) ? outVal : 0;

            Console.WriteLine("Setting saves...");
            elements = doc.GetElementsByTagName("save");
            //var allSaves = doc.GetElementsByTagName("allsaves");
            eStats = new string[3] { "FORT_BONUS", "REF_BONUS", "WILL_BONUS" };
            for(int i = 0; i < eStats.Length; i++)
            {
                if(int.TryParse(elements[i].Attributes["base"].Value, out outVal))
                    stats.Stats[eStats[i]] = outVal;              
                if(int.TryParse(elements[i].Attributes["fromresist"].Value, out outVal))
                    stats.Stats[eStats[i]].AddBonus(new Bonus { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });
                if(int.TryParse(elements[i].Attributes["frommisc"].Value, out outVal))
                    stats.Stats[eStats[i]].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = outVal });       
                
            }
            
            Console.WriteLine("Setting ac...");
            elements = doc.GetElementsByTagName("armorclass");

            stats.Stats["AC_BONUS"] = 10;
            if(int.TryParse(elements[0].Attributes["fromarmor"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "ARMOR", Type = BonusType.Armor, Value = outVal });
            if(int.TryParse(elements[0].Attributes["fromshield"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "SHIELD", Type = BonusType.Shield, Value = outVal });
            if(int.TryParse(elements[0].Attributes["fromwisdom"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "WIS", Type = BonusType.Typeless, Value = outVal });
            if(int.TryParse(elements[0].Attributes["fromcharisma"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "CHA", Type = BonusType.Typeless, Value = outVal });
            if(int.TryParse(elements[0].Attributes["fromnatural"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "NATURAL", Type = BonusType.Natural, Value = outVal });
            if(int.TryParse(elements[0].Attributes["fromdeflect"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DEFLECTION", Type = BonusType.Deflection, Value = outVal });
            if(int.TryParse(elements[0].Attributes["fromdodge"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DODGE", Type = BonusType.Dodge, Value = outVal });
            if(int.TryParse(elements[0].Attributes["frommisc"].Value, out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });

            Console.WriteLine("Setting penalties...");
            elements = doc.GetElementsByTagName("penalty");
            if(int.TryParse(elements[0].Attributes["text"].Value, out outVal))
                stats.Stats["AC_PENALTY"] = outVal;
            if(int.TryParse(elements[1].Attributes["text"].Value, out outVal))
                stats.Stats["AC_MAXDEX"] = outVal;

            elements = doc.GetElementsByTagName("initiative");
            if(int.TryParse(elements[0].Attributes["misctext"].Value, out outVal))
                stats.Stats["INIT_BONUS"] = outVal;

            Console.WriteLine("Setting skills...");
            elements = doc.GetElementsByTagName("skill");          
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "Acrobatics", "SK_ACR" },
                { "Appraise", "SK_APR" },
                { "Bluff", "SK_BLF" },
                { "Climb", "SK_CLM" },
                { "Diplomacy", "SK_DIP" },
                { "Disable Device", "SK_DSA" },
                { "Disguise", "SK_DSG" },
                { "Escape Artist", "SK_ESC" },
                { "Fly", "SK_FLY" },
                { "Handle Animal", "SK_HND" },
                { "Heal", "SK_HEA"},
                { "Intimidate", "SK_ITM" },
                { "Knowledge (arcana)", "SK_ARC" },
                { "Knowledge (dungeoneering)", "SK_DUN" },
                { "Knowledge (engineering)", "SK_ENG" },
                { "Knowledge (geography)", "SK_GEO" },
                { "Knowledge (history)", "SK_HIS" },
                { "Knowledge (local)", "SK_LCL" },
                { "Knowledge (nature)", "SK_NTR" },
                { "Knowledge (nobility)", "SK_NBL" },
                { "Knowledge (planes)", "SK_PLN" },
                { "Knowledge (religion)", "SK_RLG" },
                { "Linguistics", "SK_LNG" },
                { "Perception", "SK_PRC" },               
                { "Ride", "SK_RDE" },
                { "Sense Motive", "SK_SNS" },
                { "Sleight of Hand", "SK_SLT" },
                { "Spellcraft", "SK_SPL" },
                { "Stealth", "SK_STL" },
                { "Survival", "SK_SUR" },
                { "Swim", "SK_SWM" },
                { "Use Magic Device", "SK_UMD" },
            };

            foreach(var skill in dict)
                foreach(XmlNode node in elements)
                    if(node.Attributes["name"].Value == skill.Key)
                        stats.Stats[skill.Value] = int.TryParse(node.Attributes["ranks"].Value, out outVal) ? outVal : 0;                              
           
            return stats;
        }
    
        public static StatBlock UpdateWithPCGen(Stream stream, StatBlock stats)
        {
            stats.ClearBonuses();
            var doc = new XmlDocument();
            doc.Load(stream);

            var outVal = 0;

            var elements = doc.GetElementsByTagName("node");
            Console.WriteLine(elements.Count);

            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach(XmlNode node in elements)
                dict.Add(node.Attributes["name"].Value, node.InnerXml);

            Console.WriteLine("Setting info...");
            stats.Info["LEVELS"]    = dict["Class"];
            stats.Info["DEITY"]     = dict["Deity"];
            stats.Info["ALIGNMENT"] = dict["Alignment"];
            stats.Info["RACE"]      = dict["Race"];
            stats.Info["SIZE"]      = dict["Size"];
            stats.Info["GENDER"]    = dict["Gender"];
            stats.Info["AGE"]       = dict["Age"];
            stats.Info["HEIGHT"]    = dict["Height"];
            stats.Info["WEIGHT"]    = dict["Weight"];
            stats.Info["HAIR"]      = dict["Hair"];
            stats.Info["EYES"]      = dict["Eyes"];


            Console.WriteLine("Setting size...");
            switch(stats.Info["SIZE"])
            {
                case "Fine":
                    stats.Stats["SIZE_MOD"] = 8;
                    stats.Stats["SIZE_SKL"] = 8;
                    break;
                case "Diminutive":
                    stats.Stats["SIZE_MOD"] = 4;
                    stats.Stats["SIZE_SKL"] = 6;
                    break;
                case "Tiny":
                    stats.Stats["SIZE_MOD"] = 2;
                    stats.Stats["SIZE_SKL"] = 4;
                    break;
                case "Small":
                    stats.Stats["SIZE_MOD"] = 1;
                    stats.Stats["SIZE_SKL"] = 2;
                    break;
                case "Medium":
                    stats.Stats["SIZE_MOD"] = 0;
                    stats.Stats["SIZE_SKL"] = 0;
                    break;
                case "Large":
                    stats.Stats["SIZE_MOD"] = -1;
                    stats.Stats["SIZE_SKL"] = -2;
                    break;
                case "Huge":
                    stats.Stats["SIZE_MOD"] = -2;
                    stats.Stats["SIZE_SKL"] = -4;
                    break;
                case "Gargantuan":
                    stats.Stats["SIZE_MOD"] = -4;
                    stats.Stats["SIZE_SKL"] = -6;
                    break;
                case "Colossal":
                    stats.Stats["SIZE_MOD"] = -8;
                    stats.Stats["SIZE_SKL"] = -8;
                    break;
            }


            Console.WriteLine("Setting scores...");
            stats.Stats["LEVEL"]        = int.TryParse(dict["Level"], out outVal) ? outVal : 0;      
            stats.Stats["STR_SCORE"]    = int.TryParse(dict["Str"], out outVal) ? outVal : 10;
            stats.Stats["DEX_SCORE"]    = int.TryParse(dict["Dex"], out outVal) ? outVal : 10;
            stats.Stats["CON_SCORE"]    = int.TryParse(dict["Con"], out outVal) ? outVal : 10;
            stats.Stats["INT_SCORE"]    = int.TryParse(dict["Int"], out outVal) ? outVal : 10;
            stats.Stats["WIS_SCORE"]    = int.TryParse(dict["Wis"], out outVal) ? outVal : 10;
            stats.Stats["CHA_SCORE"]    = int.TryParse(dict["Cha"], out outVal) ? outVal : 10;

            Console.WriteLine("Setting hp...");
            stats.Stats["HP_BASE"] = int.TryParse(dict["HP"], out outVal) ? outVal - (((stats["CON_SCORE"] - 10) / 2) * stats["LEVEL"]) : 0;
            
            
            Console.WriteLine("Setting ac...");
            stats.Stats["AC_BONUS"] = 10;
            if(int.TryParse(dict["ACArmor"], out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "ARMOR", Type = BonusType.Armor, Value = outVal });
            if(int.TryParse(dict["ACShield"], out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "SHIELD", Type = BonusType.Shield, Value = outVal });
            if(int.TryParse(dict["ACNat"], out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "NATURAL", Type = BonusType.Natural, Value = outVal });
            if(int.TryParse(dict["ACDeflect"], out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DEFLECT", Type = BonusType.Deflection, Value = outVal });
            if(int.TryParse(dict["ACMisc"], out outVal))
                stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });

            stats["AC_PENALTY"] = int.TryParse(dict["Armor1Check"], out outVal) ? outVal : 0;
            stats["AC_MAXDEX"]  = int.TryParse(dict["Armor1Dex"], out outVal) ? outVal : 99;


            Console.WriteLine("Setting bab,saves...");
            stats.Stats["INIT_BONUS"]   = int.TryParse(dict["InitMisc"], out outVal) ? outVal: 0;
            stats.Stats["BAB"]          = int.TryParse(dict["BaseAttack"], out outVal) ? outVal : 0;

            stats.Stats["FORT_BONUS"]   = int.TryParse(dict["Fort"], out outVal) ? outVal : 0;
            if(int.TryParse(dict["FortMagic"], out outVal))
                stats.Stats["FORT_BONUS"].AddBonus(new Bonus() { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });

            stats.Stats["REF_BONUS"]    = int.TryParse(dict["Reflex"], out outVal) ? outVal : 0;
            if(int.TryParse(dict["ReflexMagic"], out outVal))
                stats.Stats["REF_BONUS"].AddBonus(new Bonus() { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });

            stats.Stats["WILL_BONUS"]   = int.TryParse(dict["Will"], out outVal) ? outVal : 0;
            if(int.TryParse(dict["WillMagic"], out outVal))
                stats.Stats["WILL_BONUS"].AddBonus(new Bonus() { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });

            Dictionary<string, string> skillDict = new Dictionary<string, string>()
            {
                { "Acrobatics", "SK_ACR" },
                { "Appraise", "SK_APR" },
                { "Bluff", "SK_BLF" },
                { "Climb", "SK_CLM" },
                { "Diplomacy", "SK_DIP" },
                { "Disable Device", "SK_DSA" },
                { "Disguise", "SK_DSG" },
                { "Escape Artist", "SK_ESC" },
                { "Fly", "SK_FLY" },
                { "Handle Animal", "SK_HND" },
                { "Heal", "SK_HEA"},
                { "Intimidate", "SK_ITM" },
                { "Knowledge (Arcana)", "SK_ARC" },
                { "Knowledge (Dungeoneering)", "SK_DUN" },
                { "Knowledge (Engineering)", "SK_ENG" },
                { "Knowledge (Geography)", "SK_GEO" },
                { "Knowledge (History)", "SK_HIS" },
                { "Knowledge (Local)", "SK_LCL" },
                { "Knowledge (Nature)", "SK_NTR" },
                { "Knowledge (Nobility)", "SK_NBL" },
                { "Knowledge (Planes)", "SK_PLN" },
                { "Knowledge (Religion)", "SK_RLG" },
                { "Linguistics", "SK_LNG" },
                { "Perception", "SK_PRC" },
                { "Ride", "SK_RDE" },
                { "Sense Motive", "SK_SNS" },
                { "Sleight of Hand", "SK_SLT" },
                { "Spellcraft", "SK_SPL" },
                { "Stealth", "SK_STL" },
                { "Survival", "SK_SUR" },
                { "Swim", "SK_SWM" },
                { "Use Magic Device", "SK_UMD" },
            };

            Console.WriteLine("Setting skills...");

            
            foreach(var skill in skillDict)
            {
                foreach(var node in dict)
                {
                    if(node.Value == skill.Key)
                    {
                        stats.Stats[skill.Value] = int.TryParse(dict[$"{node.Key}Rank"].Replace(".0",""), out outVal) ? outVal : 0;
                        if(int.TryParse(dict[$"{node.Key}MiscMod"], out outVal))
                            stats.Stats[skill.Value].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });                       
                    }
                }                            
            }                
            return stats;
        }
    }
}
