using Gellybeans.Pathfinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MathfinderBot
{
    public static class DataMap
    {
        
        public static List<Attack>  Attacks { get; set; } = new List<Attack>();
        public static JArray        Bestiary { get; set; } = null;
        
        public static Dictionary<string, List<(string, string)>> Modifiers = new Dictionary<string, List<(string, string)>>()
        {
            //spells
            { "ANIMAL_GROWTH", new List<(string, string)>(){
                ("ANIMAL_GROWTH_DIMINUTIVE", "DIMINUTIVE"),
                ("ANIMAL_GROWTH_TINY",       "TINY"      ),
                ("ANIMAL_GROWTH_SMALL",      "SMALL"     ),
                ("ANIMAL_GROWTH_MEDIUM",     "MEDIUM"    ),
                ("ANIMAL_GROWTH_LARGE",      "LARGE"     ),
                ("ANIMAL_GROWTH_HUGE",       "HUGE"      ),
                ("ANIMAL_GROWTH_GARGANTUAN", "GARGANTUAN"),
                ("ANIMAL_GROWTH_COLOSSAL",   "COLOSSAL"  )}},
            { "BANE", null },
            { "BEARS_ENDURANCE", null },
            { "BEAST_SHAPE", new List<(string, string)>(){
                ("BEAST_SHAPE_DIMINUTIVE", "DIMINUTIVE"),
                ("BEAST_SHAPE_TINY",       "TINY"      ),
                ("BEAST_SHAPE_SMALL",      "SMALL"     ),
                ("BEAST_SHAPE_MEDIUM",     "MEDIUM"    ),
                ("BEAST_SHAPE_LARGE",      "LARGE"     ),
                ("BEAST_SHAPE_HUGE",       "HUGE"      )}},
            { "BLEND", null },
            { "BLESS", null },
            { "BULLS_ENDURANCE", null },
            { "CATS_GRACE", null },
            { "DISCOVERY_TORCH", null },
            { "DIVINE_FAVOR", new List<(string, string)>(){
                ("DIVINE_FAVOR_1", "+1"),
                ("DIVINE_FAVOR_2", "+2"),
                ("DIVINE_FAVOR_3", "+3")}},
            { "DEADEYES_LORE", null },
            { "DRAGON_FORM", new List<(string, string)>(){
                ("DRAGON_FORM_MEDIUM",  "MEDIUM"),
                ("DRAGON_FORM_LARGE",   "LARGE" ),
                ("DRAGON_FORM_HUGE",    "HUGE"  )}},
            { "EAGLES_SPLENDOR", null },
            { "ENLARGE_PERSON", new List<(string, string)>(){
                ("ENLARGE_PERSON_DIMINUTIVE",  "DIMINUTIVE"),
                ("ENLARGE_PERSON_TINY",        "TINY"      ),
                ("ENLARGE_PERSON_SMALL",       "SMALL"     ),
                ("ENLARGE_PERSON_MEDIUM",      "MEDIUM"    ),
                ("ENLARGE_PERSON_LARGE",       "LARGE"     ),
                ("ENLARGE_PERSON_HUGE",        "HUGE"      ),
                ("ENLARGE_PERSON_GARGANTUAN",  "GARGANTUAN"),
                ("ENLARGE_PERSON_COLOSSAL",    "COLOSSAL"  )}},
            { "ELEMENTAL_BODY", new List<(string, string)>(){
                ("ELEMENTAL_BODY_SMALL_AIR",    "AIR(S)"  ),
                ("ELEMENTAL_BODY_SMALL_EARTH",  "EARTH(S)"),
                ("ELEMENTAL_BODY_SMALL_FIRE",   "FIRE(S)" ),
                ("ELEMENTAL_BODY_SMALL_WATER",  "WATER(S)"),
                ("ELEMENTAL_BODY_MEDIUM_AIR",   "AIR(M)"  ),
                ("ELEMENTAL_BODY_MEDIUM_EARTH", "EARTH(M)"),
                ("ELEMENTAL_BODY_MEDIUM_FIRE",  "FIRE(M)" ),
                ("ELEMENTAL_BODY_MEDIUM_WATER", "WATER(M)"),
                ("ELEMENTAL_BODY_LARGE_AIR",    "AIR(L)"  ),
                ("ELEMENTAL_BODY_LARGE_EARTH",  "EARTH(L)"),
                ("ELEMENTAL_BODY_LARGE_FIRE",   "FIRE(L)" ),
                ("ELEMENTAL_BODY_LARGE_WATER",  "WATER(L)"),
                ("ELEMENTAL_BODY_HUGE_AIR",     "AIR(H)"  ),
                ("ELEMENTAL_BODY_HUGE_EARTH",   "EARTH(H)"),
                ("ELEMENTAL_BODY_HUGE_FIRE",    "FIRE(H)" ),
                ("ELEMENTAL_BODY_HUGE_WATER",   "WATER(H)")}},
            { "FEY_FORM", new List<(string, string)>(){
                ("FEY_FORM_DIMINUTIVE",  "DIMINUTIVE"),
                ("FEY_FORM_TINY",        "TINY"      ),
                ("FEY_FORM_SMALL",       "SMALL"     ),
                ("FEY_FORM_MEDIUM",      "MEDIUM"    ),
                ("FEY_FORM_LARGE",       "LARGE"     ),
                ("FEY_FORM_HUGE",        "HUGE"      )}},
            { "FLAGBEARER", null },
            { "FOXS_CUNNING", null },
            { "GIANT_FORM", new List<(string, string)>(){
                ("GIANT_FORM_LARGE", "LARGE" ),
                ("GIANT_FORM_HUGE",  "HUGE"  )}},
            { "HASTE", null },
            { "INSPIRE_COURAGE", new List<(string, string)>(){
                ("INSPIRE_COURAGE_1", "+1"),
                ("INSPIRE_COURAGE_2", "+2"),
                ("INSPIRE_COURAGE_3", "+3"),
                ("INSPIRE_COURAGE_4", "+4"),
                ("INSPIRE_COURAGE_5", "+5")}},
            { "MAGE_ARMOR", null },
            { "MAGICAL_BEAST_SHAPE", new List<(string, string)>(){
                ("MAGICAL_BEAST_SHAPE_DIMINUTIVE", "DIMINUTIVE"),
                ("MAGICAL_BEAST_SHAPE_TINY",       "TINY"      ),
                ("MAGICAL_BEAST_SHAPE_SMALL",      "SMALL"     ),
                ("MAGICAL_BEAST_SHAPE_MEDIUM",     "MEDIUM"    ),
                ("MAGICAL_BEAST_SHAPE_LARGE",      "LARGE"     ),
                ("MAGICAL_BEAST_SHAPE_HUGE",       "HUGE"      )}},
            { "NAGA_SHAPE", null },
            { "OOZE_FORM", new List<(string, string)>(){
                ("FEY_FORM_SMALL",  "SMALL" ),
                ("FEY_FORM_MEDIUM", "MEDIUM"),
                ("FEY_FORM_LARGE",  "LARGE" ),
                ("FEY_FORM_HUGE",   "HUGE"  )}},
            { "OWLS_WISDOM", null },
            { "PLANT_SHAPE", new List<(string, string)>(){
                ("PLANT_SHAPE_SMALL",   "SMALL" ),
                ("PLANT_SHAPE_MEDIUM",  "MEDIUM"),
                ("PLANT_SHAPE_LARGE",   "LARGE" ),
                ("PLANT_SHAPE_HUGE",    "HUGE"  )}},
            { "PUP_SHAPE", null },
            { "REDUCE_PERSON", new List<(string, string)>(){
                ("REDUCE_PERSON_FINE",       "FINE"      ),
                ("REDUCE_PERSON_DIMINUTIVE", "DIMINUTIVE"),
                ("REDUCE_PERSON_TINY",       "TINY"      ),
                ("REDUCE_PERSON_SMALL",      "SMALL"     ),
                ("REDUCE_PERSON_MEDIUM",     "MEDIUM"    ),
                ("REDUCE_PERSON_LARGE",      "LARGE"     ),
                ("REDUCE_PERSON_HUGE",       "HUGE"      ),
                ("REDUCE_PERSON_GARGANTUAN", "GARGANTUAN")}},
            { "RESISTANCE", null },
            { "SHOCK_SHIELD", null },
            { "SHIELD", null },
            { "SHIELD_OF_FAITH", new List<(string, string)>(){
                ("SHIELD_OF_FAITH_1", "+2"),
                ("SHIELD_OF_FAITH_2", "+3"),
                ("SHIELD_OF_FAITH_3", "+4"),
                ("SHIELD_OF_FAITH_4", "+5")}},
            { "STUNNING_BARRIER", null },
            { "STUNNING_BARRIER_GREATER", null },
            { "TAP_INNER_BEAUTY", null },
            { "TRANSFORMATION", null },
            { "UNPREPARED_COMBATANT", null },
            { "VERMIN_SHAPE", new List<(string, string)>(){
                ("VERMIN_SHAPE_TINY",   "TINY"  ),
                ("VERMIN_SHAPE_SMALL",  "SMALL" ),
                ("VERMIN_SHAPE_MEDIUM", "MEDIUM"),
                ("VERMIN_SHAPE_LARGE",  "LARGE" )}},

            //conditions
            { "BLINDED",    null },
            { "COWERING",   null },
            { "DAZZLED",    null },
            { "DEAFENED",   null },
            { "ENTANGLED",  null },
            { "EXHAUSTED",  null },
            { "FASCINATED", null },
            { "FATIGUED",   null },
            { "FRIGHTENED", null },
            { "GRAPPLED",   null },
            { "HELPLESS",   null },
            { "PANICKED",   null },
            { "PARALYZED",  null },
            { "PINNED",     null },
            { "SHAKEN",     null },
            { "SICKENED",   null },
            { "STUNNED",    null },
        };

        static DataMap()
        {
            Console.Write("Getting weapons...");
            var attacks = File.ReadAllText(@"D:\PFData\Weapons.json");
            Attacks = JsonConvert.DeserializeObject<List<Attack>>(attacks);
            Console.WriteLine($"Attacks => {Attacks.Count}");

            Console.Write("Getting bestiary...");
            var bestiary = File.ReadAllText(@"D:\PFData\Bestiary.json");
            Bestiary = JsonConvert.DeserializeObject<JArray>(bestiary);
            Console.WriteLine($"Bestiary => {Bestiary.Count}");
        }

    }
}
