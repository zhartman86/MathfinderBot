using System.Text.RegularExpressions;
using System.Xml;
using GroupDocs.Parser;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using Newtonsoft.Json.Linq;
using System.Text;
using Discord;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Reflection.Metadata;

namespace MathfinderBot
{
    public static class Utility
    {

        public static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz1234567890";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Hash(string s) 
        {
            using var sha1 = SHA1.Create();
            return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(RandomString(4) + s))).Substring(0,6);
        }

        public static async Task<List<IUser>> ParseTargets(string targets)
        {
            var targetList = new List<IUser>();
            var regex = new Regex(@"\D+");
            var replace = regex.Replace(targets, " ");
            var split = replace.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for(int i = 0; i < split.Length; i++)
            {
                var id = 0ul;
                ulong.TryParse(split[i], out id);
                var dUser = await Program.GetUser(id);
                if(dUser != null) targetList.Add(dUser);
            }
            return targetList;
        }

        public static int Levenshtein(string search, string actual)
        {
                search = search.ToLower();
                actual = actual.ToLower();
                
                int n = search.Length;
                int m = actual.Length;
                int[,] d = new int[n + 1, m + 1];

                // Verify arguments.
                if(n == 0) return m;
                if(m == 0) return n;

                // Initialize arrays.
                for(int i = 0; i <= n; d[i, 0] = i++) { }
                for(int j = 0; j <= m; d[0, j] = j++) { }

                // Begin looping.
                for(int i = 1; i <= n; i++)
                {
                    for(int j = 1; j <= m; j++)
                    {
                        // Compute cost.
                        int cost = (actual[j - 1] == search[i - 1]) ? 0 : 1;
                        d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                    }
                }
                // Return cost.
                return d[n, m];
        }
        
        //Imports             
        public static async Task<StatBlock> UpdateWithPathbuilder(Stream stream, StatBlock stats)
        {
            var task = await Task.Run(()  => 
            {
                stats.ClearBonuses();
                using var parser = new GroupDocs.Parser.Parser(stream);

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

                stats.CharacterName = map["CHARNAME"];

                Console.WriteLine($"Parsing {stats.CharacterName}...");
                stats.Vars["LEVELS"]    = new StringValue(map["CHARLEVEL"]);
                stats.Vars["DEITY"]     = new StringValue(map["DEITY"]);
                stats.Vars["ALIGNMENT"] = new StringValue(map["ALIGMENT"]);
                stats.Vars["RACE"]      = new StringValue(map["RACE"]);
                stats.Vars["HOMELAND"]  = new StringValue(map["HOMELAND"]);
                stats.Vars["SIZE"]      = new StringValue(map["SIZE"]);
                stats.Vars["GENDER"]    = new StringValue(map["GENDER"]);
                stats.Vars["AGE"]       = new StringValue(map["AGE"]);
                stats.Vars["HEIGHT"]    = new StringValue(map["HEIGHT"]);
                stats.Vars["WEIGHT"]    = new StringValue(map["WEIGHT"]);
                stats.Vars["HAIR"]      = new StringValue(map["HAIR"]);
                stats.Vars["EYES"]      = new StringValue(map["EYES"]);


                Console.WriteLine("size...");
                switch(stats.Vars["SIZE"].ToString())
                {
                    case "Fine":
                        stats.Vars["SIZE_MOD"] = new Stat(8);
                        stats.Vars["SIZE_SKL"] = new Stat(8);
                        break;
                    case "Diminutive":
                        stats.Vars["SIZE_MOD"] = new Stat(4);
                        stats.Vars["SIZE_SKL"] = new Stat(6);
                        break;
                    case "Tiny":
                        stats.Vars["SIZE_MOD"] = new Stat(2);
                        stats.Vars["SIZE_SKL"] = new Stat(4);
                        break;
                    case "Small":
                        stats.Vars["SIZE_MOD"] = new Stat(1);
                        stats.Vars["SIZE_SKL"] = new Stat(2);
                        break;
                    case "Medium":
                        stats.Vars["SIZE_MOD"] = new Stat(0);
                        stats.Vars["SIZE_SKL"] = new Stat(0);
                        break;
                    case "Large":
                        stats.Vars["SIZE_MOD"] = new Stat(-1);
                        stats.Vars["SIZE_SKL"] = new Stat(-2);
                        break;
                    case "Huge":
                        stats.Vars["SIZE_MOD"] = new Stat(-2);
                        stats.Vars["SIZE_SKL"] = new Stat(-4);
                        break;
                    case "Gargantuan":
                        stats.Vars["SIZE_MOD"] = new Stat(-4);
                        stats.Vars["SIZE_SKL"] = new Stat(-6);
                        break;
                    case "Colossal":
                        stats.Vars["SIZE_MOD"] = new Stat(-8);
                        stats.Vars["SIZE_SKL"] = new Stat(-8);
                        break;
                }

                Console.WriteLine("scores...");
                stats["STR_SCORE"] = int.TryParse(map["ABILITYBASE0"], out var outVal) ? new Stat(outVal) : new Stat(10);
                stats["DEX_SCORE"] = int.TryParse(map["ABILITYBASE1"], out outVal) ? new Stat(outVal) : new Stat(10);
                stats["CON_SCORE"] = int.TryParse(map["ABILITYBASE2"], out outVal) ? new Stat(outVal) : new Stat(10);
                stats["INT_SCORE"] = int.TryParse(map["ABILITYBASE3"], out outVal) ? new Stat(outVal) : new Stat(10);
                stats["WIS_SCORE"] = int.TryParse(map["ABILITYBASE4"], out outVal) ? new Stat(outVal) : new Stat(10);
                stats["CHA_SCORE"] = int.TryParse(map["ABILITYBASE5"], out outVal) ? new Stat(outVal) : new Stat(10);

                Console.WriteLine("levels...");
                var matches = Regex.Matches(map["CHARLEVEL"], @"([0-9]{1,2})");

                int lvls = 0;
                foreach(Match m in matches)
                    lvls += int.Parse(m.Value);

                var level = lvls;
                var hp = int.Parse(map["HITPOINTS"]);
                var conMod = (stats.Vars["CON_SCORE"] - 10) / 2;

                stats["LEVEL"] = new Stat(lvls);
                stats["HP_BASE"] = new Stat(hp - (level * conMod));

                Console.WriteLine("bab, cmb, cmd, saves, ac...");
                stats["BAB"] = int.TryParse(map["BAB"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats["INIT_BONUS"] = int.TryParse(map["INITMISC"], out outVal) ? new Stat(outVal) : new Stat(0);

                stats["CMB_BONUS"] = new Stat(0);
                stats.Vars["CMB_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["CMBMISC"], out outVal) ? outVal : 0 });

                stats["CMD_BONUS"] = new Stat(0);
                stats.Vars["CMD_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["CMDMISC"], out outVal) ? outVal : 0 });

                stats["FORT_BONUS"] = int.TryParse(map["FORTBASE"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["FORT_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["FORTMISC"], out outVal) ? outVal : 0 });
                stats.Vars["FORT_BONUS"].AddBonus(new Bonus { Name = "MAGIC", Type = BonusType.Resistance, Value = int.TryParse(map["FORTMAGIC"], out outVal) ? outVal : 0 });

                stats["REF_BONUS"] = int.TryParse(map["REFLEXBASE"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["REF_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["REFLEXMISC"], out outVal) ? outVal : 0 });
                stats.Vars["REF_BONUS"].AddBonus(new Bonus { Name = "MAGIC", Type = BonusType.Resistance, Value = int.TryParse(map["REFLEXMAGIC"], out outVal) ? outVal : 0 });

                stats["WILL_BONUS"] = int.TryParse(map["WILLBASE"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["WILL_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["WILLMISC"], out outVal) ? outVal : 0 });
                stats.Vars["WILL_BONUS"].AddBonus(new Bonus { Name = "MAGIC", Type = BonusType.Resistance, Value = int.TryParse(map["WILLMAGIC"], out outVal) ? outVal : 0 });

                stats["AC_BONUS"] = new Stat(0);
                stats.Vars["AC_BONUS"].AddBonus(new Bonus { Name = "ARMOR", Type = BonusType.Armor, Value = int.TryParse(map["ACARMOR"], out outVal) ? outVal : 0 });
                stats.Vars["AC_BONUS"].AddBonus(new Bonus { Name = "SHIELD", Type = BonusType.Shield, Value = int.TryParse(map["ACSHIELD"], out outVal) ? outVal : 0 });
                stats.Vars["AC_BONUS"].AddBonus(new Bonus { Name = "NATURAL", Type = BonusType.Natural, Value = int.TryParse(map["ACNATURAL"], out outVal) ? outVal : 0 });
                stats.Vars["AC_BONUS"].AddBonus(new Bonus { Name = "DEFLECTION", Type = BonusType.Deflection, Value = int.TryParse(map["ACDEFLECTION"], out outVal) ? outVal : 0 });
                stats.Vars["AC_BONUS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["ACMISC"], out outVal) ? outVal : 0 });

                stats["AC_PENALTY"] = int.TryParse(map["ARMORPENALTY0"], out outVal) ? new Stat(outVal) : new Stat(0);

                //this isnt exactly accurate, but it should work?
                stats["AC_MAXDEX"] = int.TryParse(map["ACDEX"], out outVal) ? new Stat(outVal) : new Stat(99);

                Console.WriteLine("skillss...");
                stats["SK_ACR"] = int.TryParse(map["ACROBATICSRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_ACR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["ACROBATICSMISC"], out outVal) ? outVal : 0 });

                stats["SK_APR"] = int.TryParse(map["APPRAISERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_APR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["APPRAISEMISC"], out outVal) ? outVal : 0 });

                stats["SK_BLF"] = int.TryParse(map["BLUFFRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_BLF"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["BLUFFMISC"], out outVal) ? outVal : 0 });

                stats["SK_CLM"] = int.TryParse(map["CLIMBRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_CLM"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["CLIMBMISC"], out outVal) ? outVal : 0 });

                stats["SK_DIP"] = int.TryParse(map["DIPLOMACYRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_DIP"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["DIPLOMACYMISC"], out outVal) ? outVal : 0 });

                stats["SK_DSA"] = int.TryParse(map["DISABLE DEVICERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_DSA"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["DISABLE DEVICEMISC"], out outVal) ? outVal : 0 });

                stats["SK_DSG"] = int.TryParse(map["DISGUISERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_DSG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["DISGUISEMISC"], out outVal) ? outVal : 0 });

                stats["SK_ESC"] = int.TryParse(map["ESCAPE ARTISTRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_ESC"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["ESCAPE ARTISTMISC"], out outVal) ? outVal : 0 });

                stats["SK_FLY"] = int.TryParse(map["FLYRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_FLY"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["FLYMISC"], out outVal) ? outVal : 0 });

                stats["SK_HND"] = int.TryParse(map["HANDLE ANIMALRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_HND"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["HANDLE ANIMALMISC"], out outVal) ? outVal : 0 });

                stats["SK_HEA"] = int.TryParse(map["HEALRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_HEA"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["HEALMISC"], out outVal) ? outVal : 0 });

                stats["SK_ITM"] = int.TryParse(map["INTIMIDATERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_ITM"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["INTIMIDATEMISC"], out outVal) ? outVal : 0 });

                stats["SK_ARC"] = int.TryParse(map["KNOWLEDGE (ARCANA)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_ARC"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (ARCANA)MISC"], out outVal) ? outVal : 0 });

                stats["SK_DUN"] = int.TryParse(map["KNOWLEDGE (DUNGEONEERING)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_DUN"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (DUNGEONEERING)MISC"], out outVal) ? outVal : 0 });

                stats["SK_ENG"] = int.TryParse(map["KNOWLEDGE (ENGINEERING)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_ENG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (ENGINEERING)MISC"], out outVal) ? outVal : 0 });

                stats["SK_GEO"] = int.TryParse(map["KNOWLEDGE (GEOGRAPHY)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_GEO"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (GEOGRAPHY)MISC"], out outVal) ? outVal : 0 });

                stats["SK_HIS"] = int.TryParse(map["KNOWLEDGE (HISTORY)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_HIS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (HISTORY)MISC"], out outVal) ? outVal : 0 });

                stats["SK_LCL"] = int.TryParse(map["KNOWLEDGE (LOCAL)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_LCL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (LOCAL)MISC"], out outVal) ? outVal : 0 });

                stats["SK_NTR"] = int.TryParse(map["KNOWLEDGE (NATURE)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_NTR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (NATURE)MISC"], out outVal) ? outVal : 0 });

                stats["SK_NBL"] = int.TryParse(map["KNOWLEDGE (NOBILITY)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_NBL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (NOBILITY)MISC"], out outVal) ? outVal : 0 });

                stats["SK_PLN"] = int.TryParse(map["KNOWLEDGE (PLANES)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_PLN"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (PLANES)MISC"], out outVal) ? outVal : 0 });

                stats["SK_RLG"] = int.TryParse(map["KNOWLEDGE (RELIGION)RANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_RLG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["KNOWLEDGE (RELIGION)MISC"], out outVal) ? outVal : 0 });

                stats["SK_LNG"] = int.TryParse(map["LINGUISTICSRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_LNG"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["LINGUISTICSMISC"], out outVal) ? outVal : 0 });

                stats["SK_PRC"] = int.TryParse(map["PERCEPTIONRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_PRC"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["PERCEPTIONMISC"], out outVal) ? outVal : 0 });

                stats["SK_RDE"] = int.TryParse(map["RIDERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_RDE"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["RIDEMISC"], out outVal) ? outVal : 0 });

                stats["SK_SNS"] = int.TryParse(map["SENSE MOTIVERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_SNS"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SENSE MOTIVEMISC"], out outVal) ? outVal : 0 });

                stats["SK_SLT"] = int.TryParse(map["SLEIGHT OF HANDRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_SLT"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SLEIGHT OF HANDMISC"], out outVal) ? outVal : 0 });

                stats["SK_SPL"] = int.TryParse(map["SPELLCRAFTRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_SPL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SPELLCRAFTMISC"], out outVal) ? outVal : 0 });

                stats["SK_STL"] = int.TryParse(map["STEALTHRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_STL"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["STEALTHMISC"], out outVal) ? outVal : 0 });

                stats["SK_SUR"] = int.TryParse(map["SURVIVALRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_SUR"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SURVIVALMISC"], out outVal) ? outVal : 0 });

                stats["SK_SWM"] = int.TryParse(map["SWIMRANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_SWM"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["SWIMMISC"], out outVal) ? outVal : 0 });

                stats["SK_UMD"] = int.TryParse(map["USE MAGIC DEVICERANKS"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats.Vars["SK_UMD"].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(map["USE MAGIC DEVICEMISC"], out outVal) ? outVal : 0 });


                Console.WriteLine("misc...");
                stats["PP"] = int.TryParse(map["PP"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats["GP"] = int.TryParse(map["GP"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats["SP"] = int.TryParse(map["SP"], out outVal) ? new Stat(outVal) : new Stat(0);
                stats["CP"] = int.TryParse(map["CP"], out outVal) ? new Stat(outVal) : new Stat(0);


                return stats!;
            });
            return task!;
        }

        public static StatBlock UpdateWithHeroLabs(Stream stream, StatBlock stats)
        {
            //stats.ClearBonuses();
            //var doc = new XmlDocument();
            //doc.Load(stream);                  
            
            //var outVal = 0;
            //var elements = doc.GetElementsByTagName("size");

            //stats.Stats["SIZE"] = int.TryParse(elements[0].Attributes["name"].Value, out outVal) ? outVal : 0;

            //Console.WriteLine("Setting size...");
            //switch(stats.Info["SIZE"])
            //{
            //    case "Fine":
            //        stats.Stats["SIZE_MOD"] = 8;
            //        stats.Stats["SIZE_SKL"] = 8;
            //        break;
            //    case "Diminutive":
            //        stats.Stats["SIZE_MOD"] = 4;
            //        stats.Stats["SIZE_SKL"] = 6;
            //        break;
            //    case "Tiny":
            //        stats.Stats["SIZE_MOD"] = 2;
            //        stats.Stats["SIZE_SKL"] = 4;
            //        break;
            //    case "Small":
            //        stats.Stats["SIZE_MOD"] = 1;
            //        stats.Stats["SIZE_SKL"] = 2;
            //        break;
            //    case "Medium":
            //        stats.Stats["SIZE_MOD"] = 0;
            //        stats.Stats["SIZE_SKL"] = 0;
            //        break;
            //    case "Large":
            //        stats.Stats["SIZE_MOD"] = -1;
            //        stats.Stats["SIZE_SKL"] = -2;
            //        break;
            //    case "Huge":
            //        stats.Stats["SIZE_MOD"] = -2;
            //        stats.Stats["SIZE_SKL"] = -4;
            //        break;
            //    case "Gargantuan":
            //        stats.Stats["SIZE_MOD"] = -4;
            //        stats.Stats["SIZE_SKL"] = -6;
            //        break;
            //    case "Colossal":
            //        stats.Stats["SIZE_MOD"] = -8;
            //        stats.Stats["SIZE_SKL"] = -8;
            //        break;
            //}


            //Console.WriteLine("Setting scores...");
            //elements = doc.GetElementsByTagName("attrvalue");
            //var eStats = new string[6] { "STR_SCORE", "DEX_SCORE", "CON_SCORE", "INT_SCORE", "WIS_SCORE", "CHA_SCORE" };
            //for(int i = 0; i < eStats.Length; i++)
            //{
            //    var split = elements[i].Attributes["text"].Value.Split('/');
            //    stats.Stats[eStats[i]] = int.Parse(split[0]);
            //    if(split.Length > 1)
            //        stats.Stats[eStats[i]].AddBonus(new Bonus() { Name = "ENH_BONUS", Type = BonusType.Enhancement, Value = int.Parse(split[1]) - int.Parse(split[0]) });
            //}

            //Console.WriteLine("Setting level...");
            //elements = doc.GetElementsByTagName("classes");
            //stats.Stats["LEVEL"] = int.TryParse(elements[0].Attributes["level"].Value, out outVal) ?  outVal : 0;

            //Console.WriteLine("Setting hp...");
            //elements = doc.GetElementsByTagName("health");
            //var hpTotal = int.TryParse(elements[0].Attributes["hitpoints"].Value, out outVal) ? outVal : 0;
            //stats.Stats["HP_BASE"] = hpTotal - (stats.Stats["LEVEL"] * ((stats.Stats["CON_SCORE"] - 10) / 2));

            //elements = doc.GetElementsByTagName("attack");
            //stats.Stats["BAB"] = int.TryParse(elements[0].Attributes["baseattack"].Value, out outVal) ? outVal : 0;

            //Console.WriteLine("Setting coin...");
            //elements = doc.GetElementsByTagName("money");
            //stats.Stats["PP"] = int.TryParse(elements[0].Attributes["pp"].Value, out outVal) ? outVal : 0;
            //stats.Stats["GP"] = int.TryParse(elements[0].Attributes["gp"].Value, out outVal) ? outVal : 0;
            //stats.Stats["SP"] = int.TryParse(elements[0].Attributes["sp"].Value, out outVal) ? outVal : 0;
            //stats.Stats["CP"] = int.TryParse(elements[0].Attributes["cp"].Value, out outVal) ? outVal : 0;

            //Console.WriteLine("Setting saves...");
            //elements = doc.GetElementsByTagName("save");
            ////var allSaves = doc.GetElementsByTagName("allsaves");
            //eStats = new string[3] { "FORT_BONUS", "REF_BONUS", "WILL_BONUS" };
            //for(int i = 0; i < eStats.Length; i++)
            //{
            //    if(int.TryParse(elements[i].Attributes["base"].Value, out outVal))
            //        stats.Stats[eStats[i]] = outVal;              
            //    if(int.TryParse(elements[i].Attributes["fromresist"].Value, out outVal))
            //        stats.Stats[eStats[i]].AddBonus(new Bonus { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });
            //    if(int.TryParse(elements[i].Attributes["frommisc"].Value, out outVal))
            //        stats.Stats[eStats[i]].AddBonus(new Bonus { Name = "MISC", Type = BonusType.Typeless, Value = outVal });       
                
            //}
            
            //Console.WriteLine("Setting ac...");
            //elements = doc.GetElementsByTagName("armorclass");

            //stats.Stats["AC_BONUS"] = 0;
            //if(int.TryParse(elements[0].Attributes["fromarmor"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "ARMOR", Type = BonusType.Armor, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["fromshield"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "SHIELD", Type = BonusType.Shield, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["fromwisdom"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "WIS", Type = BonusType.Typeless, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["fromcharisma"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "CHA", Type = BonusType.Typeless, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["fromnatural"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "NATURAL", Type = BonusType.Natural, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["fromdeflect"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DEFLECTION", Type = BonusType.Deflection, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["fromdodge"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DODGE", Type = BonusType.Dodge, Value = outVal });
            //if(int.TryParse(elements[0].Attributes["frommisc"].Value, out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });

            //Console.WriteLine("Setting penalties...");
            //elements = doc.GetElementsByTagName("penalty");
            //if(int.TryParse(elements[0].Attributes["text"].Value, out outVal))
            //    stats.Stats["AC_PENALTY"] = outVal;
            //if(int.TryParse(elements[1].Attributes["text"].Value, out outVal))
            //    stats.Stats["AC_MAXDEX"] = outVal;

            //elements = doc.GetElementsByTagName("initiative");
            //if(int.TryParse(elements[0].Attributes["misctext"].Value, out outVal))
            //    stats.Stats["INIT_BONUS"] = outVal;

            //Console.WriteLine("Setting skillss...");
            //elements = doc.GetElementsByTagName("skills");          
            //Dictionary<string, string> dict = new Dictionary<string, string>()
            //{
            //    { "Acrobatics", "SK_ACR" },
            //    { "Appraise", "SK_APR" },
            //    { "Bluff", "SK_BLF" },
            //    { "Climb", "SK_CLM" },
            //    { "Diplomacy", "SK_DIP" },
            //    { "Disable Device", "SK_DSA" },
            //    { "Disguise", "SK_DSG" },
            //    { "Escape Artist", "SK_ESC" },
            //    { "Fly", "SK_FLY" },
            //    { "Handle Animal", "SK_HND" },
            //    { "Heal", "SK_HEA"},
            //    { "Intimidate", "SK_ITM" },
            //    { "Knowledge (arcana)", "SK_ARC" },
            //    { "Knowledge (dungeoneering)", "SK_DUN" },
            //    { "Knowledge (engineering)", "SK_ENG" },
            //    { "Knowledge (geography)", "SK_GEO" },
            //    { "Knowledge (history)", "SK_HIS" },
            //    { "Knowledge (local)", "SK_LCL" },
            //    { "Knowledge (nature)", "SK_NTR" },
            //    { "Knowledge (nobility)", "SK_NBL" },
            //    { "Knowledge (planes)", "SK_PLN" },
            //    { "Knowledge (religion)", "SK_RLG" },
            //    { "Linguistics", "SK_LNG" },
            //    { "Perception", "SK_PRC" },               
            //    { "Ride", "SK_RDE" },
            //    { "Sense Motive", "SK_SNS" },
            //    { "Sleight of Hand", "SK_SLT" },
            //    { "Spellcraft", "SK_SPL" },
            //    { "Stealth", "SK_STL" },
            //    { "Survival", "SK_SUR" },
            //    { "Swim", "SK_SWM" },
            //    { "Use Magic Device", "SK_UMD" },
            //};

            //foreach(var skills in dict)
            //    foreach(XmlNode node in elements)
            //        if(node.Attributes["name"].Value == skills.Key)
            //            stats.Stats[skills.Value] = int.TryParse(node.Attributes["ranks"].Value, out outVal) ? outVal : 0;                              
           
            return stats;
        }
    
        public static StatBlock UpdateWithPCGen(Stream stream, StatBlock stats)
        {
            //stats.ClearBonuses();
            //var doc = new XmlDocument();
            //doc.Load(stream);

            //var outVal = 0;

            //var elements = doc.GetElementsByTagName("node");
            //Console.WriteLine(elements.Count);

            //Dictionary<string, string> dict = new Dictionary<string, string>();

            //foreach(XmlNode node in elements)
            //    dict.Add(node.Attributes["name"].Value, node.InnerXml);

            //Console.WriteLine("Setting info...");
            //stats.Info["LEVELS"]    = dict["Class"];
            //stats.Info["DEITY"]     = dict["Deity"];
            //stats.Info["ALIGNMENT"] = dict["Alignment"];
            //stats.Info["RACE"]      = dict["Race"];
            //stats.Info["SIZE"]      = dict["Size"];
            //stats.Info["GENDER"]    = dict["Gender"];
            //stats.Info["AGE"]       = dict["Age"];
            //stats.Info["HEIGHT"]    = dict["Height"];
            //stats.Info["WEIGHT"]    = dict["Weight"];
            //stats.Info["HAIR"]      = dict["Hair"];
            //stats.Info["EYES"]      = dict["Eyes"];


            //Console.WriteLine("Setting size...");
            //switch(stats.Info["SIZE"])
            //{
            //    case "Fine":
            //        stats.Stats["SIZE_MOD"] = 8;
            //        stats.Stats["SIZE_SKL"] = 8;
            //        break;
            //    case "Diminutive":
            //        stats.Stats["SIZE_MOD"] = 4;
            //        stats.Stats["SIZE_SKL"] = 6;
            //        break;
            //    case "Tiny":
            //        stats.Stats["SIZE_MOD"] = 2;
            //        stats.Stats["SIZE_SKL"] = 4;
            //        break;
            //    case "Small":
            //        stats.Stats["SIZE_MOD"] = 1;
            //        stats.Stats["SIZE_SKL"] = 2;
            //        break;
            //    case "Medium":
            //        stats.Stats["SIZE_MOD"] = 0;
            //        stats.Stats["SIZE_SKL"] = 0;
            //        break;
            //    case "Large":
            //        stats.Stats["SIZE_MOD"] = -1;
            //        stats.Stats["SIZE_SKL"] = -2;
            //        break;
            //    case "Huge":
            //        stats.Stats["SIZE_MOD"] = -2;
            //        stats.Stats["SIZE_SKL"] = -4;
            //        break;
            //    case "Gargantuan":
            //        stats.Stats["SIZE_MOD"] = -4;
            //        stats.Stats["SIZE_SKL"] = -6;
            //        break;
            //    case "Colossal":
            //        stats.Stats["SIZE_MOD"] = -8;
            //        stats.Stats["SIZE_SKL"] = -8;
            //        break;
            //}


            //Console.WriteLine("Setting scores...");
            //stats.Stats["LEVEL"]        = int.TryParse(dict["Level"], out outVal) ? outVal : 0;      
            //stats.Stats["STR_SCORE"]    = int.TryParse(dict["Str"], out outVal) ? outVal : 10;
            //stats.Stats["DEX_SCORE"]    = int.TryParse(dict["Dex"], out outVal) ? outVal : 10;
            //stats.Stats["CON_SCORE"]    = int.TryParse(dict["Con"], out outVal) ? outVal : 10;
            //stats.Stats["INT_SCORE"]    = int.TryParse(dict["Int"], out outVal) ? outVal : 10;
            //stats.Stats["WIS_SCORE"]    = int.TryParse(dict["Wis"], out outVal) ? outVal : 10;
            //stats.Stats["CHA_SCORE"]    = int.TryParse(dict["Cha"], out outVal) ? outVal : 10;

            //Console.WriteLine("Setting hp...");
            //stats.Stats["HP_BASE"] = int.TryParse(dict["HP"], out outVal) ? outVal - (((stats["CON_SCORE"] - 10) / 2) * stats["LEVEL"]) : 0;
            
            
            //Console.WriteLine("Setting ac...");
            //stats.Stats["AC_BONUS"] = 0;
            //if(int.TryParse(dict["ACArmor"], out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "ARMOR", Type = BonusType.Armor, Value = outVal });
            //if(int.TryParse(dict["ACShield"], out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "SHIELD", Type = BonusType.Shield, Value = outVal });
            //if(int.TryParse(dict["ACNat"], out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "NATURAL", Type = BonusType.Natural, Value = outVal });
            //if(int.TryParse(dict["ACDeflect"], out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DEFLECT", Type = BonusType.Deflection, Value = outVal });
            //if(int.TryParse(dict["ACMisc"], out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });

            //stats["AC_PENALTY"] = int.TryParse(dict["Armor1Check"], out outVal) ? outVal : 0;
            //stats["AC_MAXDEX"]  = int.TryParse(dict["Armor1Dex"], out outVal) ? outVal : 99;


            //Console.WriteLine("Setting bab,saves...");
            //stats.Stats["INIT_BONUS"]   = int.TryParse(dict["InitMisc"], out outVal) ? outVal: 0;
            //stats.Stats["BAB"]          = int.TryParse(dict["BaseAttack"], out outVal) ? outVal : 0;

            //stats.Stats["FORT_BONUS"]   = int.TryParse(dict["Fort"], out outVal) ? outVal : 0;
            //if(int.TryParse(dict["FortMagic"], out outVal))
            //    stats.Stats["FORT_BONUS"].AddBonus(new Bonus() { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });

            //stats.Stats["REF_BONUS"]    = int.TryParse(dict["Reflex"], out outVal) ? outVal : 0;
            //if(int.TryParse(dict["ReflexMagic"], out outVal))
            //    stats.Stats["REF_BONUS"].AddBonus(new Bonus() { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });

            //stats.Stats["WILL_BONUS"]   = int.TryParse(dict["Will"], out outVal) ? outVal : 0;
            //if(int.TryParse(dict["WillMagic"], out outVal))
            //    stats.Stats["WILL_BONUS"].AddBonus(new Bonus() { Name = "RESISTANCE", Type = BonusType.Resistance, Value = outVal });

            //Dictionary<string, string> skillsDict = new Dictionary<string, string>()
            //{
            //    { "Acrobatics", "SK_ACR" },
            //    { "Appraise", "SK_APR" },
            //    { "Bluff", "SK_BLF" },
            //    { "Climb", "SK_CLM" },
            //    { "Diplomacy", "SK_DIP" },
            //    { "Disable Device", "SK_DSA" },
            //    { "Disguise", "SK_DSG" },
            //    { "Escape Artist", "SK_ESC" },
            //    { "Fly", "SK_FLY" },
            //    { "Handle Animal", "SK_HND" },
            //    { "Heal", "SK_HEA"},
            //    { "Intimidate", "SK_ITM" },
            //    { "Knowledge (Arcana)", "SK_ARC" },
            //    { "Knowledge (Dungeoneering)", "SK_DUN" },
            //    { "Knowledge (Engineering)", "SK_ENG" },
            //    { "Knowledge (Geography)", "SK_GEO" },
            //    { "Knowledge (History)", "SK_HIS" },
            //    { "Knowledge (Local)", "SK_LCL" },
            //    { "Knowledge (Nature)", "SK_NTR" },
            //    { "Knowledge (Nobility)", "SK_NBL" },
            //    { "Knowledge (Planes)", "SK_PLN" },
            //    { "Knowledge (Religion)", "SK_RLG" },
            //    { "Linguistics", "SK_LNG" },
            //    { "Perception", "SK_PRC" },
            //    { "Ride", "SK_RDE" },
            //    { "Sense Motive", "SK_SNS" },
            //    { "Sleight of Hand", "SK_SLT" },
            //    { "Spellcraft", "SK_SPL" },
            //    { "Stealth", "SK_STL" },
            //    { "Survival", "SK_SUR" },
            //    { "Swim", "SK_SWM" },
            //    { "Use Magic Device", "SK_UMD" },
            //};

            //Console.WriteLine("Setting skillss...");

            
            //foreach(var skills in skillsDict)
            //{
            //    foreach(var node in dict)
            //    {
            //        if(node.Value == skills.Key)
            //        {
            //            stats.Stats[skills.Value] = int.TryParse(dict[$"{node.Key}Rank"].Replace(".0",""), out outVal) ? outVal : 0;
            //            if(int.TryParse(dict[$"{node.Key}MiscMod"], out outVal))
            //                stats.Stats[skills.Value].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });                       
            //        }
            //    }                            
            //}                
            return stats;
        }
    
        public static StatBlock UpdateWithMotto(byte[] stream, StatBlock stats)
        {
            //stats.ClearBonuses();
            //var jsonStr = Encoding.UTF8.GetString(stream);
            //var json = JObject.Parse(jsonStr);

            //stats.CharacterName = json["name"].Value<string>();

            //Console.WriteLine("setting name");
            //var regex = new Regex(@"\D+");
            //var replace = regex.Replace(json["level"].Value<string>(), " ");
            //var split = replace.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            //var levels = 0;
            //for(int i = 0; i < split.Length; i++)
            //{
            //    levels += int.Parse(split[i]);
            //}

            //stats.Stats["LEVEL"] = levels;

            //switch(json["size"].Value<string>())
            //{
            //    case "F":
            //        stats.Stats["SIZE_MOD"] = 8;
            //        stats.Stats["SIZE_SKL"] = 8;
            //        break;
            //    case "D":
            //        stats.Stats["SIZE_MOD"] = 4;
            //        stats.Stats["SIZE_SKL"] = 6;
            //        break;
            //    case "T":
            //        stats.Stats["SIZE_MOD"] = 2;
            //        stats.Stats["SIZE_SKL"] = 4;
            //        break;
            //    case "S":
            //        stats.Stats["SIZE_MOD"] = 1;
            //        stats.Stats["SIZE_SKL"] = 2;
            //        break;
            //    case "M":
            //        stats.Stats["SIZE_MOD"] = 0;
            //        stats.Stats["SIZE_SKL"] = 0;
            //        break;
            //    case "L":
            //        stats.Stats["SIZE_MOD"] = -1;
            //        stats.Stats["SIZE_SKL"] = -2;
            //        break;
            //    case "H":
            //        stats.Stats["SIZE_MOD"] = -2;
            //        stats.Stats["SIZE_SKL"] = -4;
            //        break;
            //    case "G":
            //        stats.Stats["SIZE_MOD"] = -4;
            //        stats.Stats["SIZE_SKL"] = -6;
            //        break;
            //    case "C":
            //        stats.Stats["SIZE_MOD"] = -8;
            //        stats.Stats["SIZE_SKL"] = -8;
            //        break;
            //}

            //var outVal = 0;
            //JToken outToken = null;

            //Console.WriteLine("setting scores");
            //stats.Stats["STR_SCORE"] = int.TryParse(json["abilities"]["str"].Value<string>(), out outVal) ? outVal : 10;
            //stats.Stats["DEX_SCORE"] = int.TryParse(json["abilities"]["dex"].Value<string>(), out outVal) ? outVal : 10;
            //stats.Stats["CON_SCORE"] = int.TryParse(json["abilities"]["con"].Value<string>(), out outVal) ? outVal : 10;
            //stats.Stats["INT_SCORE"] = int.TryParse(json["abilities"]["int"].Value<string>(), out outVal) ? outVal : 10;
            //stats.Stats["WIS_SCORE"] = int.TryParse(json["abilities"]["wis"].Value<string>(), out outVal) ? outVal : 10;
            //stats.Stats["CHA_SCORE"] = int.TryParse(json["abilities"]["cha"].Value<string>(), out outVal) ? outVal : 10;

            //var hp = int.TryParse(json["hp"]["total"].Value<string>(), out outVal) ? outVal : 0;
            //var conMod = (stats["CON_SCORE"] - 10) / 2;

            //stats.Stats["HP_BASE"] = hp - (conMod * levels);

            //Console.WriteLine("setting saves");
            //stats.Stats["BAB"] = int.TryParse(json["bab"].Value<string>(), out outVal) ? outVal : 0;
            
            //if(json["initiative"].Value<JObject>().ContainsKey("miscModifier"))
            //    stats.Stats["INIT_BONUS"] = int.TryParse(json["initiative"]["miscModifier"].Value<string>(), out outVal) ? outVal : 0;

            //stats.Stats["FORT_BONUS"] = int.TryParse(json["saves"]["fort"]["base"].Value<string>(), out outVal) ? outVal : 0;
            //if(json["saves"]["fort"].Value<JObject>().ContainsKey("miscModifier") && int.TryParse(json["saves"]["fort"]["miscModifier"].Value<string>(), out outVal))
            //    stats.Stats["FORT_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });
            //stats.Stats["REF_BONUS"] = int.TryParse(json["saves"]["reflex"]["base"].Value<string>(), out outVal) ? outVal : 0;
            //if(json["saves"]["reflex"].Value<JObject>().ContainsKey("miscModifier") && int.TryParse(json["saves"]["reflex"]["miscModifier"].Value<string>(), out outVal))
            //    stats.Stats["REF_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });
            //stats.Stats["WILL_BONUS"] = int.TryParse(json["saves"]["will"]["base"].Value<string>(), out outVal) ? outVal : 0;
            //if(json["saves"]["will"].Value<JObject>().ContainsKey("miscModifier") && int.TryParse(json["saves"]["will"]["miscModifier"].Value<string>(), out outVal))
            //    stats.Stats["WILL_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = outVal });

            //Console.WriteLine("setting ac");
            //stats.Stats["AC_BONUS"] = 0;
            //if(json["ac"].Value<JObject>().ContainsKey("naturalArmor") && int.TryParse(json["ac"]["naturalArmor"].Value<string>(), out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "NATURAL", Type = BonusType.Natural, Value = outVal });
            //if(json["ac"].Value<JObject>().ContainsKey("miscModifier") && int.TryParse(Regex.Replace(json["ac"]["miscModifier"].Value<string>(), @"[+]([0-9]) Dodge", "$1"), out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DODGE", Type = BonusType.Dodge, Value = outVal });
            //if(json["ac"].Value<JObject>().ContainsKey("deflectionModifier") && int.TryParse(json["ac"]["deflectionModifier"].Value<string>(), out outVal)) 
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "DEFLECTION", Type = BonusType.Deflection, Value = outVal });
            //if(json["ac"].Value<JObject>().ContainsKey("armorBonus") && int.TryParse(json["ac"]["armorBonus"].Value<string>(), out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "ARMOR", Type = BonusType.Armor, Value = outVal });
            //if(json["ac"].Value<JObject>().ContainsKey("shieldBonus") && int.TryParse(json["ac"]["shieldBonus"].Value<string>(), out outVal))
            //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "SHIELD", Type = BonusType.Shield, Value = outVal });


            //Console.WriteLine("setting skillss");
            //if(json["skills"].Value<JObject>().ContainsKey("acrobatics"))
            //{
            //    if(json["skills"]["acrobatics"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ACR"] = int.TryParse(json["skills"]["acrobatics"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["acrobatics"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["acrobatics"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_ACR"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["acrobatics"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["acrobatics"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_ACR"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["acrobatics"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["acrobatics"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ACR"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("appraise"))
            //{
            //    if(json["skills"]["appraise"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_APR"] = int.TryParse(json["skills"]["appraise"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["appraise"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["appraise"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_APR"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["appraise"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["appraise"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_APR"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["appraise"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["appraise"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_APR"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("bluff"))
            //{
            //    if(json["skills"]["bluff"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_BLF"] = int.TryParse(json["skills"]["bluff"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["bluff"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["bluff"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_BLF"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["bluff"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["bluff"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_BLF"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["bluff"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["bluff"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_BLF"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}
            
            //if(json["skills"].Value<JObject>().ContainsKey("climb"))
            //{
            //    if(json["skills"]["climb"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_CLM"] = int.TryParse(json["skills"]["climb"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["climb"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["climb"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_CLM"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["climb"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["climb"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_CLM"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["climb"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["climb"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_CLM"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("diplomacy"))
            //{
            //    if(json["skills"]["diplomacy"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_DIP"] = int.TryParse(json["skills"]["diplomacy"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["diplomacy"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["diplomacy"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_DIP"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["diplomacy"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["diplomacy"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_DIP"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["diplomacy"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["diplomacy"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_DIP"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("disableDevice"))
            //{
            //    if(json["skills"]["disableDevice"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_DSA"] = int.TryParse(json["skills"]["disableDevice"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["disableDevice"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["disableDevice"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_DSA"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["disableDevice"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["disableDevice"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_DSA"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["disableDevice"].Value<JObject>().ContainsKey("classSkill") && json["skills"].Value<JObject>().ContainsKey("disableDevice"))
            //        stats.Stats["SK_DSA"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("disguise"))
            //{
            //    if(json["skills"]["disguise"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_DSG"] = int.TryParse(json["skills"]["disguise"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["disguise"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["disguise"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_DSG"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["disguise"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["disguise"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_DSG"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["disguise"].Value<JObject>().ContainsKey("classSkill") && json["skills"].Value<JObject>().ContainsKey("disguise"))
            //        stats.Stats["SK_DSG"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("escapeArtist"))
            //{
            //    if(json["skills"]["escapeArtist"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ESC"] = int.TryParse(json["skills"]["escapeArtist"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["escapeArtist"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["escapeArtist"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_ESC"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["escapeArtist"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["escapeArtist"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_ESC"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["escapeArtist"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["escapeArtist"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ESC"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}


            //if(json["skills"].Value<JObject>().ContainsKey("fly"))
            //{
            //    if(json["skills"]["fly"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_FLY"] = int.TryParse(json["skills"]["fly"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["fly"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["fly"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_FLY"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["fly"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["fly"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_FLY"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["fly"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["fly"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_FLY"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("handleAnimal"))
            //{
            //    if(json["skills"]["handleAnimal"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_HND"] = int.TryParse(json["skills"]["handleAnimal"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["handleAnimal"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["handleAnimal"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_HND"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["handleAnimal"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["handleAnimal"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_HND"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["handleAnimal"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["handleAnimal"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_HND"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("heal"))
            //{
            //    if(json["skills"]["heal"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_HEA"] = int.TryParse(json["skills"]["heal"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["heal"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["heal"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_HEA"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["heal"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["heal"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_HEA"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["heal"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["heal"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_HEA"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("intimidate"))
            //{
            //    if(json["skills"]["intimidate"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ITM"] = int.TryParse(json["skills"]["intimidate"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["intimidate"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["intimidate"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_ITM"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["intimidate"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["intimidate"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_ITM"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["intimidate"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["intimidate"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ITM"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeArcana"))
            //{
            //    if(json["skills"]["knowledgeArcana"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ARC"] = int.TryParse(json["skills"]["knowledgeArcana"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeArcana"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeArcana"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_ARC"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeArcana"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeArcana"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_ARC"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeArcana"].Value<JObject>().ContainsKey("classSkill") & json["skills"]["knowledgeArcana"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ARC"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeDungeoneering"))
            //{
            //    if(json["skills"]["knowledgeDungeoneering"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_DUN"] = int.TryParse(json["skills"]["knowledgeDungeoneering"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeDungeoneering"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeDungeoneering"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_DUN"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeDungeoneering"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeDungeoneering"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_DUN"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeDungeoneering"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeDungeoneering"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_DUN"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeEngineering"))
            //{
            //    if(json["skills"]["knowledgeEngineering"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ENG"] = int.TryParse(json["skills"]["knowledgeEngineering"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeEngineering"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeEngineering"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_ENG"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeEngineering"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeEngineering"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_ENG"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeEngineering"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeEngineering"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_ENG"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeGeography"))
            //{
            //    if(json["skills"]["knowledgeGeography"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_GEO"] = int.TryParse(json["skills"]["knowledgeGeography"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeGeography"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeGeography"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_GEO"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeGeography"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeGeography"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_GEO"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeGeography"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeGeography"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_GEO"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeHistory"))
            //{
            //    if(json["skills"]["knowledgeHistory"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_HIS"] = int.TryParse(json["skills"]["knowledgeHistory"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeHistory"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeHistory"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_HIS"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeHistory"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeHistory"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_HIS"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeHistory"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeHistory"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_HIS"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeLocal"))
            //{
            //    if(json["skills"]["knowledgeLocal"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_LCL"] = int.TryParse(json["skills"]["knowledgeLocal"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeLocal"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeLocal"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_LCL"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeLocal"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeLocal"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_LCL"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeLocal"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeLocal"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_LCL"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeNature"))
            //{
            //    if(json["skills"]["knowledgeNature"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_NTR"] = int.TryParse(json["skills"]["knowledgeNature"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeNature"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeNature"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_NTR"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeNature"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeNature"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_NTR"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeNature"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeNature"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_NTR"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeNobility"))
            //{
            //    if(json["skills"]["knowledgeNobility"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_NBL"] = int.TryParse(json["skills"]["knowledgeNobility"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeNobility"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeNobility"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_NBL"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeNobility"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeNobility"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_NBL"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeNobility"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeNobility"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_NBL"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgePlanes"))
            //{
            //    if(json["skills"]["knowledgePlanes"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_PLN"] = int.TryParse(json["skills"]["knowledgePlanes"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgePlanes"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgePlanes"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_PLN"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgePlanes"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgePlanes"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_PLN"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgePlanes"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgePlanes"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_PLN"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("knowledgeReligion"))
            //{
            //    if(json["skills"]["knowledgeReligion"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_RLG"] = int.TryParse(json["skills"]["knowledgeReligion"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["knowledgeReligion"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["knowledgeReligion"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_RLG"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["knowledgeReligion"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["knowledgeReligion"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_RLG"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["knowledgeReligion"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["knowledgeReligion"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_RLG"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}
            
            //if(json["skills"].Value<JObject>().ContainsKey("linguistics"))
            //{
            //    if(json["skills"]["linguistics"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_LNG"] = int.TryParse(json["skills"]["linguistics"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["linguistics"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["linguistics"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_LNG"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["linguistics"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["linguistics"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_LNG"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["linguistics"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["linguistics"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_LNG"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("perception"))
            //{
            //    if(json["skills"]["perception"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_PRC"] = int.TryParse(json["skills"]["perception"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["perception"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["perception"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_PRC"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["perception"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["perception"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_PRC"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["perception"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["perception"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_PRC"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("ride"))
            //{
            //    if(json["skills"]["ride"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_RDE"] = int.TryParse(json["skills"]["ride"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["ride"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["ride"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_RDE"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["ride"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["ride"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_RDE"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["ride"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["ride"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_RDE"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("senseMotive"))
            //{
            //    if(json["skills"]["senseMotive"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SNS"] = int.TryParse(json["skills"]["senseMotive"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["senseMotive"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["senseMotive"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_SNS"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["senseMotive"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["senseMotive"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_SNS"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["senseMotive"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["senseMotive"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SNS"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("sleightOfHand"))
            //{
            //    if(json["skills"]["sleightOfHand"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SLT"] = int.TryParse(json["skills"]["sleightOfHand"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["sleightOfHand"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["sleightOfHand"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_SLT"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["sleightOfHand"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["sleightOfHand"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_SLT"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["sleightOfHand"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["sleightOfHand"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SLT"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("spellcraft"))
            //{
            //    if(json["skills"]["spellcraft"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SPL"] = int.TryParse(json["skills"]["spellcraft"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["spellcraft"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["spellcraft"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_SPL"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["spellcraft"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["spellcraft"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_SPL"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["spellcraft"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["spellcraft"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SPL"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("stealth"))
            //{
            //    if(json["skills"]["stealth"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_STL"] = int.TryParse(json["skills"]["stealth"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["stealth"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["stealth"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_STL"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["stealth"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["stealth"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_STL"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["stealth"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["stealth"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_STL"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("survival"))
            //{
            //    if(json["skills"]["survival"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SUR"] = int.TryParse(json["skills"]["survival"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["survival"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["survival"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_SUR"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["survival"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["survival"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_SUR"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["survival"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["survival"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SUR"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("swim"))
            //{
            //    if(json["skills"]["swim"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SWM"] = int.TryParse(json["skills"]["swim"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["swim"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["swim"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_SWM"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["swim"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["swim"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_SWM"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["swim"].Value<JObject>().ContainsKey("classSkill") & json["skills"]["swim"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_SWM"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}

            //if(json["skills"].Value<JObject>().ContainsKey("useMagicDevice"))
            //{
            //    if(json["skills"]["useMagicDevice"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_UMD"] = int.TryParse(json["skills"]["useMagicDevice"]["ranks"].Value<string>(), out outVal) ? outVal : 0;
            //    if(json["skills"]["useMagicDevice"].Value<JObject>().ContainsKey("racial") && int.TryParse(json["skills"]["useMagicDevice"]["racial"].Value<string>(), out outVal))
            //        stats.Stats["SK_UMD"].AddBonus(new Bonus() { Name = "RACIAL", Type = BonusType.Racial, Value = outVal });
            //    if(json["skills"]["useMagicDevice"].Value<JObject>().ContainsKey("trait") && int.TryParse(json["skills"]["useMagicDevice"]["trait"].Value<string>(), out outVal))
            //        stats.Stats["SK_UMD"].AddBonus(new Bonus() { Name = "TRAIT", Type = BonusType.Trait, Value = outVal });
            //    if(json["skills"]["useMagicDevice"].Value<JObject>().ContainsKey("classSkill") && json["skills"]["useMagicDevice"].Value<JObject>().ContainsKey("ranks"))
            //        stats.Stats["SK_UMD"].AddBonus(new Bonus() { Name = "CLASS_SKILL", Type = BonusType.Typeless, Value = 3 });
            //}


            return stats;
        }

        public static async Task<StatBlock> UpdateWithScribe(Stream stream, StatBlock stats)
        {
            //stats.ClearBonuses();
            
            //Console.WriteLine("scribe...");
            //var baseStats = new Regex(@"^(?<name>.*?) -.*?ECL: (?<level>.*?)\n.*?Size: (?<size>.*?)\n.*HP: (?<hp>.*?)\n.*STR:[ ]{0,5}(?<str>.*?) .*\nDEX:[ ]{0,5}(?<dex>.*?) .*\nCON:[ ]{0,5}(?<con>.*?) .*\nINT:[ ]{0,5}(?<int>.*?) .*\nWIS:[ ]{0,5}(?<wis>.*?) .*\nCHA:[ ]{0,5}(?<cha>.*?) .*\nFortitude: [+-](?<fort>.*?)\nReflex   : [+-](?<ref>.*?)\nWill     : [+-](?<will>.*?)\n", RegexOptions.Singleline);
            //var acMisc = new Regex(@"AC Normal     : (?<ac>.*?)\n.*Armor :.*(\(AC [+](?<armor>[0-9]{1,2})).*max dex [+](?<maxdex>[0-9])?.*BAB: [+](?<bab>.*?)\nInitiative: (?<init>.*?)\n", RegexOptions.Singleline);
            //var skills = new Regex(@"-- Skills --.*Acrobatics.*?= (?<acrr>[0-9]*).*?[+].*?[+-].*?[+] (?<acrm>[-+][0-9]*).*Appraise.*?= (?<aprr>[0-9]*).*?[+].*?[+-].*?[+] (?<aprm>[-+][0-9]*).*Bluff.*?= (?<blfr>[0-9]*).*?[+].*?[+-].*?[+] (?<blfm>[-+][0-9]*).*Climb.*?= (?<clmr>[0-9]*).*?[+].*?[+-].*?[+] (?<aclm>[-+][0-9]*).*Diplomacy.*?= (?<dipr>[0-9]*).*?[+].*?[+-].*?[+] (?<dipm>[-+][0-9]*).*Disable.*?= (?<dsar>[0-9]*).*?[+].*?[+-].*?[+] (?<dsam>[-+][0-9]*).*Disguise.*?= (?<dsgr>[0-9]*).*?[+].*?[+-].*?[+] (?<dsgm>[-+][0-9]*).*Escape.*?= (?<escr>[0-9]*).*?[+].*?[+-].*?[+] (?<escm>[-+][0-9]*).*Fly.*?= (?<flyr>[0-9]*).*?[+].*?[+-].*?[+] (?<flym>[-+][0-9]*).*Handle.*?= (?<hndr>[0-9]*).*?[+].*?[+-].*?[+] (?<hndm>[-+][0-9]*).*Heal.*?= (?<hear>[0-9]*).*?[+].*?[+-].*?[+] (?<heam>[-+][0-9]*).*Intimidate.*?= (?<intr>[0-9]*).*?[+].*?[+-].*?[+] (?<intm>[-+][0-9]*)(.*Knowledge \(arcana.*?= (?<arcr>[0-9]*).*?[+].*?[+-].*?[+] (?<arcm>[-+][0-9]*))?(.*Knowledge \(dungeoneering.*?= (?<dunr>[0-9]*).*?[+].*?[+-].*?[+] (?<dunm>[-+][0-9]*))?(.*Knowledge \(engineering.*?= (?<engr>[0-9]*).*?[+].*?[+-].*?[+] (?<engm>[-+][0-9]*))?(.*Knowledge \(geography.*?= (?<geor>[0-9]*).*?[+].*?[+-].*?[+] (?<geom>[-+][0-9].*))?(.*Knowledge \(history.*?= (?<hisr>[0-9]*).*?[+].*?[+-].*?[+] (?<hism>[-+][0-9]*))?(.*Knowledge \(local.*?= (?<lclr>[0-9]*).*?[+].*?[+-].*?[+] (?<lclm>[-+][0-9]*))?(.*Knowledge \(nature.*?= (?<ntrr>[0-9]*).*?[+].*?[+-].*?[+] (?<ntrm>[-+][0-9]*))?(.*Knowledge \(nobility.*?= (?<nblr>[0-9]*).*?[+].*?[+-].*?[+] (?<nblm>[-+][0-9]*))?(.*Knowledge \(planes.*?= (?<plnr>[0-9]*).*?[+].*?[+-].*?[+] (?<plnm>[-+][0-9]*))?(.*Knowledge \(religion.*?= (?<rlgr>[0-9]*).*?[+].*?[+-].*?[+] (?<rlgm>[-+][0-9]*))?.*Linguistics.*?= (?<lngr>[0-9]*).*?[+].*?[+-].*?[+] (?<lngm>[-+][0-9]*).*Perception.*?= (?<prcr>[0-9]*).*?[+].*?[+-].*?[+] (?<prcm>[-+][0-9]*).*Ride.*?= (?<rder>[0-9]*).*?[+].*?[+-].*?[+] (?<rdem>[-+][0-9]*).*Sense.*?= (?<snsr>[0-9]*).*?[+].*?[+-].*?[+] (?<snsm>[-+][0-9]*).*Sleight.*?= (?<sltr>[0-9]*).*?[+].*?[+-].*?[+] (?<sltm>[-+][0-9]*).*Spellcraft.*?= (?<splr>[0-9]*).*?[+].*?[+-].*?[+] (?<splm>[-+][0-9]*).*Stealth.*?= (?<stlr>[0-9]*).*?[+].*?[+-].*?[+] (?<stlm>[-+][0-9]*).*Survival.*?= (?<surr>[0-9]*).*?[+].*?[+-].*?[+] (?<surm>[-+][0-9]*).*Swim.*?= (?<swmr>[0-9]*).*?[+].*?[+-].*?[+] (?<swmm>[-+][0-9]*).*Use Magic.*?= (?<umdr>[0-9]*).*?[+].*?[+-].*?[+] (?<umdm>[-+][0-9]*)", RegexOptions.Singleline);

            //using var reader = new StreamReader(stream);
            //var text = await reader.ReadToEndAsync();
            
            //Console.WriteLine("matching...");
            
            //var match = baseStats.Match(text);
            //if(match.Success)
            //{
            //    stats.Stats["LEVEL"] = int.TryParse(match.Groups["level"].Value, out int outLevel) ? outLevel : 0;

            //    var size = match.Groups["size"].Value.Trim();
            //    switch(size)
            //    {
            //        case "Fine":
            //            stats.Stats["SIZE_MOD"] = 8;
            //            stats.Stats["SIZE_SKL"] = 8;
            //            break;
            //        case "Diminutive":
            //            stats.Stats["SIZE_MOD"] = 4;
            //            stats.Stats["SIZE_SKL"] = 6;
            //            break;
            //        case "Tiny":
            //            stats.Stats["SIZE_MOD"] = 2;
            //            stats.Stats["SIZE_SKL"] = 4;
            //            break;
            //        case "Small":
            //            stats.Stats["SIZE_MOD"] = 1;
            //            stats.Stats["SIZE_SKL"] = 2;
            //            break;
            //        case "Medium":
            //            stats.Stats["SIZE_MOD"] = 0;
            //            stats.Stats["SIZE_SKL"] = 0;
            //            break;
            //        case "Large":
            //            stats.Stats["SIZE_MOD"] = -1;
            //            stats.Stats["SIZE_SKL"] = -2;
            //            break;
            //        case "Huge":
            //            stats.Stats["SIZE_MOD"] = -2;
            //            stats.Stats["SIZE_SKL"] = -4;
            //            break;
            //        case "Gargantuan":
            //            stats.Stats["SIZE_MOD"] = -4;
            //            stats.Stats["SIZE_SKL"] = -6;
            //            break;
            //        case "Colossal":
            //            stats.Stats["SIZE_MOD"] = -8;
            //            stats.Stats["SIZE_SKL"] = -8;
            //            break;
            //    }                         

            //    stats.Stats["STR_SCORE"] = int.TryParse(match.Groups["str"].Value, out int outVal) ? outVal : 0; 
            //    stats.Stats["DEX_SCORE"] = int.TryParse(match.Groups["dex"].Value, out outVal)     ? outVal : 0; 
            //    stats.Stats["CON_SCORE"] = int.TryParse(match.Groups["con"].Value, out int outCon) ? outCon : 0; 
            //    stats.Stats["INT_SCORE"] = int.TryParse(match.Groups["int"].Value, out outVal)     ? outVal : 0; 
            //    stats.Stats["WIS_SCORE"] = int.TryParse(match.Groups["wis"].Value, out outVal)     ? outVal : 0; 
            //    stats.Stats["CHA_SCORE"] = int.TryParse(match.Groups["cha"].Value, out outVal)     ? outVal : 0;



            //    stats.Stats["HP_BASE"] = (int.TryParse(match.Groups["hp"].Value, out outVal) ? outVal : 0) - (outLevel * ((outCon - 10) / 2));

            //    var fort = int.TryParse(match.Groups["fort"].Value, out outVal) ? outVal : 0;
            //    var refl = int.TryParse(match.Groups["ref"].Value, out outVal) ? outVal : 0;
            //    var will = int.TryParse(match.Groups["will"].Value, out outVal) ? outVal : 0;

            //    stats.Stats["FORT_BONUS"]  = fort - ((stats["CON_SCORE"] - 10) / 2);
            //    stats.Stats["REF_BONUS"]   = refl - ((stats["DEX_SCORE"] - 10) / 2);
            //    stats.Stats["WILL_BONUS"]  = will - ((stats["WIS_SCORE"] - 10) / 2);
            //}

            //stats["AC_BONUS"] = 0;
            //match = acMisc.Match(text);
            //if(match.Success)
            //{
            //    var acTotal = int.TryParse(match.Groups["ac"].Value, out int outVal) ? outVal : 0;
            //    var dex = (stats["DEX_SCORE"] - 10) / 2;
            //    var armor = int.TryParse(match.Groups["armor"].Value, out outVal) ? outVal : 0;
            //    var diff =  acTotal - (10 + dex + armor);
            //    stats.Stats["AC_MAXDEX"] = int.TryParse(match.Groups["maxdex"].Value, out outVal) ? outVal : 0;
            //    if(armor > 0)
            //        stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "ARMOR", Type = BonusType.Armor, Value = armor });         
            //    //if(diff > 0)
            //    //    stats.Stats["AC_BONUS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = diff });

            //    stats.Stats["BAB"] = int.TryParse(match.Groups["bab"].Value, out outVal) ? outVal : 0;
            //    //stats.Stats["INIT_BONUS"] = int.TryParse(match.Groups["bab"].Value, out outVal) ? outVal : 0;
            //}

  
            //match = skills.Match(text);
            //if(match.Success)
            //{
            //    int outVal;
            //    if(match.Groups["acrr"].Success)
            //    {
            //        stats.Stats["SK_ACR"] = int.TryParse(match.Groups["acrr"].Value, out outVal) ? outVal : 0;                    
            //        stats.Stats["SK_ACR"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["acrm"].Value, out outVal) ? outVal : 0 });
            //    }               
            //    if(match.Groups["aprr"].Success)
            //    {
            //        stats.Stats["SK_APR"] = int.TryParse(match.Groups["aprr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_APR"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["aprm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["blfr"].Success)
            //    {
            //        stats.Stats["SK_BLF"] = int.TryParse(match.Groups["blfr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_BLF"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["blfm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["clmr"].Success)
            //    {
            //        stats.Stats["SK_CLM"] = int.TryParse(match.Groups["clmr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_CLM"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["clmm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["dipr"].Success)
            //    {
            //        stats.Stats["SK_DIP"] = int.TryParse(match.Groups["dipr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_DIP"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["dipm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["dsar"].Success)
            //    {
            //        stats.Stats["SK_DSA"] = int.TryParse(match.Groups["dsar"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_DSA"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["dsam"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["dsgr"].Success)
            //    {
            //        stats.Stats["SK_DSG"] = int.TryParse(match.Groups["dsgr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_DSG"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["dsgm"].Value, out outVal) ? outVal : 0 });
            //    }                
            //    if(match.Groups["escr"].Success)
            //    {
            //        stats.Stats["SK_ESC"] = int.TryParse(match.Groups["escr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_ESC"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["escm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["flyr"].Success)
            //    {
            //        stats.Stats["SK_FLY"] = int.TryParse(match.Groups["flyr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_FLY"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["flym"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["hndr"].Success)
            //    {
            //        stats.Stats["SK_HND"] = int.TryParse(match.Groups["hndr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_HND"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["hndm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["hear"].Success)
            //    {
            //        stats.Stats["SK_HEA"] = int.TryParse(match.Groups["hear"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_HEA"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["heam"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["itmr"].Success)
            //    {
            //        stats.Stats["SK_ITM"] = int.TryParse(match.Groups["itmr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_ITM"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["itmm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["lngr"].Success)
            //    {
            //        stats.Stats["SK_LNG"] = int.TryParse(match.Groups["lngr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_LNG"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["lngm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["prcr"].Success)
            //    {
            //        stats.Stats["SK_PRC"] = int.TryParse(match.Groups["prcr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_PRC"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["prcm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["rder"].Success)
            //    {
            //        stats.Stats["SK_RDE"] = int.TryParse(match.Groups["rder"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_RDE"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["rdem"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["snsr"].Success)
            //    {
            //        stats.Stats["SK_SNS"] = int.TryParse(match.Groups["snsr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_SNS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["snsm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["sltr"].Success)
            //    {
            //        stats.Stats["SK_SLT"] = int.TryParse(match.Groups["sltr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_SLT"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["sltm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["splr"].Success)
            //    {
            //        stats.Stats["SK_SPL"] = int.TryParse(match.Groups["splr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_SPL"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["splm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["stlr"].Success)
            //    {
            //        stats.Stats["SK_STL"] = int.TryParse(match.Groups["stlr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_STL"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["stlm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["surr"].Success)
            //    {
            //        stats.Stats["SK_SUR"] = int.TryParse(match.Groups["surr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_SUR"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["surm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["swmr"].Success)
            //    {
            //        stats.Stats["SK_SWM"] = int.TryParse(match.Groups["swmr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_SWM"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["swmm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["umdr"].Success)
            //    {
            //        stats.Stats["SK_UMD"] = int.TryParse(match.Groups["umdr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_UMD"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["umdm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["arcr"].Success)
            //    {
            //        stats.Stats["SK_ARC"] = int.TryParse(match.Groups["arcr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_ARC"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["arcm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["dunr"].Success)
            //    {
            //        stats.Stats["SK_DUN"] = int.TryParse(match.Groups["dunr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_DUN"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["dunm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["engr"].Success)
            //    {
            //        stats.Stats["SK_ENG"] = int.TryParse(match.Groups["engr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_ENG"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["engm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["geor"].Success)
            //    {
            //        stats.Stats["SK_GEO"] = int.TryParse(match.Groups["geor"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_GEO"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["geom"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["hisr"].Success)
            //    {
            //        stats.Stats["SK_HIS"] = int.TryParse(match.Groups["hisr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_HIS"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["hism"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["lclr"].Success)
            //    {
            //        stats.Stats["SK_LCL"] = int.TryParse(match.Groups["lclr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_LCL"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["lclm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["ntrr"].Success)
            //    {
            //        stats.Stats["SK_NTR"] = int.TryParse(match.Groups["ntrr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_NTR"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["ntrm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["nblr"].Success)
            //    {
            //        stats.Stats["SK_NBL"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["nblm"].Value, out outVal) ? outVal : 0 });
            //        stats.Stats["SK_NBL"] = int.TryParse(match.Groups["nblr"].Value, out outVal) ? outVal : 0;
            //    }
            //    if(match.Groups["plnr"].Success)
            //    {
            //        stats.Stats["SK_PLN"] = int.TryParse(match.Groups["plnr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_PLN"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["plnm"].Value, out outVal) ? outVal : 0 });
            //    }
            //    if(match.Groups["rlgr"].Success)
            //    {
            //        stats.Stats["SK_RLG"] = int.TryParse(match.Groups["rlgr"].Value, out outVal) ? outVal : 0;
            //        stats.Stats["SK_RLG"].AddBonus(new Bonus() { Name = "MISC", Type = BonusType.Typeless, Value = int.TryParse(match.Groups["rlgm"].Value, out outVal) ? outVal : 0 });
            //    }                              
            //}

            return stats;
        
        }
        
    }
}
