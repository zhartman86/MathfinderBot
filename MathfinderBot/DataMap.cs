using Gellybeans.Pathfinder;
using Newtonsoft.Json;

namespace MathfinderBot
{
    public static class DataMap
    {
        public static List<Armor>       Armor       { get; set; } = new List<Armor>();       
        public static List<Creature>    Bestiary   { get; set; } = new List<Creature>();       
        public static List<Item>        Items       { get; set; } = new List<Item>();
        public static List<Shape>       Shapes      { get; set; } = new List<Shape>();        
        public static List<Spell>       Spells      { get; set; } = new List<Spell>();
        public static List<Weapon>      Weapons     { get; set; } = new List<Weapon>();
        
        public static Dictionary<string, List<(string, string)>> Modifiers = new Dictionary<string, List<(string, string)>>()
        {
            { "AID", null },
            { "ALTER_SELF", new List<(string, string)>(){
                ("ALTER_SELF_SMALL",    "SMALL" ),
                ("ALTER_SELF_MEDIUM",   "MEDIUM")}},
            { "ANIMAL_GROWTH", new List<(string, string)>(){
                ("ANIMAL_GROWTH_DIMINUTIVE", "DIMINUTIVE"),
                ("ANIMAL_GROWTH_TINY",       "TINY"      ),
                ("ANIMAL_GROWTH_SMALL",      "SMALL"     ),
                ("ANIMAL_GROWTH_MEDIUM",     "MEDIUM"    ),
                ("ANIMAL_GROWTH_LARGE",      "LARGE"     ),
                ("ANIMAL_GROWTH_HUGE",       "HUGE"      ),
                ("ANIMAL_GROWTH_GARGANTUAN", "GARGANTUAN"),
                ("ANIMAL_GROWTH_COLOSSAL",   "COLOSSAL"  )}},
            { "ARCHONS_AURA", null },
            { "BANE", null },
            { "BEARS_ENDURANCE", null },
            { "BEAST_SHAPE", new List<(string, string)>(){
                ("BEAST_SHAPE_DIMINUTIVE", "DIMINUTIVE"),
                ("BEAST_SHAPE_TINY",       "TINY"      ),
                ("BEAST_SHAPE_SMALL",      "SMALL"     ),
                ("BEAST_SHAPE_MEDIUM",     "MEDIUM"    ),
                ("BEAST_SHAPE_LARGE",      "LARGE"     ),
                ("BEAST_SHAPE_HUGE",       "HUGE"      )}},
            { "BESTOW_CURSE", null },
            { "BESTOW_CURSE_GREATER", null },
            { "BLEND", null },
            { "BLESS", null },
            { "BLESSING_OF_THE_MOLE", null },
            { "BULLS_STRENGTH", null },
            { "CATS_GRACE", null },
            { "CHANNEL_VIGOR", new List<(string, string)>(){
                ("LIMBS",   "HASTE"               ),
                ("MIND",    "CHANNEL_VIGOR_MIND"  ),
                ("SPIRIT",  "CHANNEL_VIGOR_SPIRIT"),
                ("TORSO",   "CHANNEL_VIGOR_TORSO" )}},
            { "CONTAGEOUS_ZEAL", null },
            { "DEADEYES_LORE", null },
            { "DISCOVERY_TORCH", null },
            { "DIVINE_FAVOR", new List<(string, string)>(){
                ("DIVINE_FAVOR_1", "+1"),
                ("DIVINE_FAVOR_2", "+2"),
                ("DIVINE_FAVOR_3", "+3")}},
            { "DRAGON_FORM", new List<(string, string)>(){
                ("DRAGON_FORM_MEDIUM",  "MEDIUM"),
                ("DRAGON_FORM_LARGE",   "LARGE" ),
                ("DRAGON_FORM_HUGE",    "HUGE"  )}},
            { "EAGLES_SPLENDOR", null },           
            { "ELEMENTAL_BODY_I", new List<(string, string)>(){
                ("ELEMENTAL_BODY_SMALL_AIR",    "AIR"  ),
                ("ELEMENTAL_BODY_SMALL_EARTH",  "EARTH"),
                ("ELEMENTAL_BODY_SMALL_FIRE",   "FIRE" ),
                ("ELEMENTAL_BODY_SMALL_WATER",  "WATER")}},
            { "ELEMENTAL_BODY_II", new List<(string, string)>(){
                ("ELEMENTAL_BODY_MEDIUM_AIR",   "AIR"  ),
                ("ELEMENTAL_BODY_MEDIUM_EARTH", "EARTH"),
                ("ELEMENTAL_BODY_MEDIUM_FIRE",  "FIRE" ),
                ("ELEMENTAL_BODY_MEDIUM_WATER", "WATER")}},
            { "ELEMENTAL_BODY_III", new List<(string, string)>(){
                ("ELEMENTAL_BODY_LARGE_AIR",    "AIR"  ),
                ("ELEMENTAL_BODY_LARGE_EARTH",  "EARTH"),
                ("ELEMENTAL_BODY_LARGE_FIRE",   "FIRE" ),
                ("ELEMENTAL_BODY_LARGE_WATER",  "WATER")}},
            { "ELEMENTAL_BODY_IV", new List<(string, string)>(){
                ("ELEMENTAL_BODY_HUGE_AIR",     "AIR(" ),
                ("ELEMENTAL_BODY_HUGE_EARTH",   "EARTH"),
                ("ELEMENTAL_BODY_HUGE_FIRE",    "FIRE" ),
                ("ELEMENTAL_BODY_HUGE_WATER",   "WATER")}},
            { "ENLARGE_PERSON", new List<(string, string)>(){
                ("ENLARGE_PERSON_DIMINUTIVE",  "DIMINUTIVE"),
                ("ENLARGE_PERSON_TINY",        "TINY"      ),
                ("ENLARGE_PERSON_SMALL",       "SMALL"     ),
                ("ENLARGE_PERSON_MEDIUM",      "MEDIUM"    ),
                ("ENLARGE_PERSON_LARGE",       "LARGE"     ),
                ("ENLARGE_PERSON_HUGE",        "HUGE"      ),
                ("ENLARGE_PERSON_GARGANTUAN",  "GARGANTUAN"),
                ("ENLARGE_PERSON_COLOSSAL",    "COLOSSAL"  )}},
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
            { "MONSTROUS_PHYSIQUE", new List<(string, string)>(){
                ("MONSTROUS_PHYSIQUE_DIMINUTIVE",   "DIMINUTIVE"),
                ("MONSTROUS_PHYSIQUE_TINY",         "TINY"  ),
                ("MONSTROUS_PHYSIQUE_SMALL",        "SMALL" ),
                ("MONSTROUS_PHYSIQUE_MEDIUM",       "MEDIUM"),
                ("MONSTROUS_PHYSIQUE_LARGE",        "LARGE" ),
                ("MONSTROUS_PHYSIQUE_HUGE",         "HUGE"  )}},
            { "NAGA_SHAPE", null },
            { "OOZE_FORM", new List<(string, string)>(){
                ("FEY_FORM_SMALL",  "SMALL" ),
                ("FEY_FORM_MEDIUM", "MEDIUM"),
                ("FEY_FORM_LARGE",  "LARGE" ),
                ("FEY_FORM_HUGE",   "HUGE"  )}},
            { "OWLS_WISDOM", null },
            { "PARAGON_SURGE", null },
            { "PLANT_SHAPE", new List<(string, string)>(){
                ("PLANT_SHAPE_SMALL",   "SMALL" ),
                ("PLANT_SHAPE_MEDIUM",  "MEDIUM"),
                ("PLANT_SHAPE_LARGE",   "LARGE" ),
                ("PLANT_SHAPE_HUGE",    "HUGE"  )}},
            { "PRAYER", new List<(string, string)>(){
                ("ALLY", "PRAYER_A"),
                ("FOE",  "PRAYER_F")}},
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
            { "SHIELD", null },
            { "SHIELD_OF_FAITH", new List<(string, string)>(){
                ("SHIELD_OF_FAITH_1", "+2"),
                ("SHIELD_OF_FAITH_2", "+3"),
                ("SHIELD_OF_FAITH_3", "+4"),
                ("SHIELD_OF_FAITH_4", "+5")}},
            { "SHOCK_SHIELD", null },
            { "STUNNING_BARRIER", null },
            { "STUNNING_BARRIER_GREATER", null },
            { "TAP_INNER_BEAUTY", null },
            { "TRANSFORMATION", null },
            { "UNDEAD_ANATOMY", new List<(string, string)>(){
                ("UNDEAD_ANATOMY_DIMINUTIVE",   "DIMINUTIVE"),
                ("UNDEAD_ANATOMY_TINY",         "TINY"      ),
                ("UNDEAD_ANATOMY_SMALL",        "SMALL"     ),
                ("UNDEAD_ANATOMY_MEDIUM",       "MEDIUM"    ),
                ("UNDEAD_ANATOMY_LARGE",        "LARGE"     ),
                ("UNDEAD_ANATOMY_HUGE",         "HUGE"      )}},
            { "UNPREPARED_COMBATANT", null },
            { "VERMIN_SHAPE", new List<(string, string)>(){
                ("VERMIN_SHAPE_TINY",   "TINY"  ),
                ("VERMIN_SHAPE_SMALL",  "SMALL" ),
                ("VERMIN_SHAPE_MEDIUM", "MEDIUM"),
                ("VERMIN_SHAPE_LARGE",  "LARGE" )}},
            { "WRATHFUL_MANTLE", new List<(string, string)>(){
                ("+1",  "WRATHFUL_MANTLE_1"),
                ("+2",  "WRATHFUL_MANTLE_2"),
                ("+3",  "WRATHFUL_MANTLE_3"),
                ("+4",  "WRATHFUL_MANTLE_4"),
                ("+5",  "WRATHFUL_MANTLE_5")}},


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
           
            Console.Write("Getting armor...");
            var armor = File.ReadAllText(@"D:\PFData\Armor.json");
            Armor = JsonConvert.DeserializeObject<List<Armor>>(armor)!;
            Console.WriteLine($"Armor => {Armor.Count}");

            Console.Write("Getting bestiary...");
            var creatures = File.ReadAllText(@"D:\PFData\Bestiary.json");
            Bestiary = JsonConvert.DeserializeObject<List<Creature>>(creatures)!;
            Console.WriteLine($"Creatures => {Bestiary.Count}");

            Console.Write("Getting items...");
            var items = File.ReadAllText(@"D:\PFData\Items.json");
            Items = JsonConvert.DeserializeObject<List<Item>>(items)!;
            Console.WriteLine($"Items => {Items.Count}");

            Console.Write("Getting shapes...");
            var shapes = File.ReadAllText(@"D:\PFData\Shapes.json");
            Shapes = JsonConvert.DeserializeObject<List<Shape>>(shapes)!;
            Console.WriteLine($"Shapes => {Shapes.Count}");

            Console.Write("Getting spells...");
            var spells = File.ReadAllText(@"D:\PFData\Spells.json");
            Spells = JsonConvert.DeserializeObject<List<Spell>>(spells)!;
            Console.WriteLine($"Spells => {Spells.Count}");

            Console.Write("Getting weapons...");
            var attacks = File.ReadAllText(@"D:\PFData\Weapons.json");
            Weapons = JsonConvert.DeserializeObject<List<Weapon>>(attacks)!;
            Console.WriteLine($"Attacks => {Weapons.Count}");

        }
    }
}
