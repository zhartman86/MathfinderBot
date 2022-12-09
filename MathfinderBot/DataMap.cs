using Discord;
using Gellybeans;
using Gellybeans.Pathfinder;
using Newtonsoft.Json;
using System.Text;
using System.Xml.Linq;

namespace MathfinderBot
{
    public static class DataMap
    {
        public static Campaign BaseCampaign { get; set; } = new Campaign() { Owner = 0, Name = "BASE"};

        public readonly static List<AutocompleteResult> autoCompleteCreatures   = new List<AutocompleteResult>();
        public readonly static List<AutocompleteResult> autoCompleteItems       = new List<AutocompleteResult>();
        public readonly static List<AutocompleteResult> autoCompleteRules       = new List<AutocompleteResult>();
        public readonly static List<AutocompleteResult> autoCompleteShapes      = new List<AutocompleteResult>();
        public readonly static List<AutocompleteResult> autoCompleteSpells      = new List<AutocompleteResult>();

        public static Dictionary<int, Secret> Secrets = new Dictionary<int, Secret>()
        {
            { 0, new Secret()
                {
                    Index = 0,
                    Name = "Seeing Stone",
                    EventString = "You see a perfectly smooth sphere, unmarked and unmarred. When you place your hand upon its surface, an orange fire erupts from within.",
                    Choices = ("Take the stone.", "Leave it alone."),
                    Take = "...",
                    Description = "A mysterious orb that glows when touched.",
                }},

            { 1, new Secret()
                {
                    Index = 1,
                    Name         = "Old Wooden Sword",
                    EventString  = "You find yourself in a dark cave, a faint noise echoes from within. Suddenly, two flames erupt near the center of the room, a figure cloaked in red garments standing between them. He opens his arms as the dirt beneath his feet blows back to reveal a sword.\r\nThe man looks at you and says, \"It's dangerous to go alone! Take this.\"",
                    Choices      = ("Take it", "\"Nah.\""),
                    Take         = "The haft rests uneasy in your grip. If the stars were to align, mayhaps you could deal a lethal strike.",
                    Description  = "A seemingly ancient wooden sword",
                    Apply = (duel, i) =>
                    {
                        var r = new Random().Next(-1,3);
                        duel.Duelists[i].Total += r;
                        duel.Duelists[i].Events += $"You strike for {r} damage!";
                        return true;
                    }
                }},
            { 2, new Secret()
                {
                    //lowest wins.
                    Index = 2,
                    Name = "Reverse Gravity",
                    EventString = "The ground drops beneath your feet as you drift away. Before you can grasp your situation, a shocking *thud* breaks your attention. You've *landed* on the ceiling.\r\nWith a semblance of orientation, you spot something strange. It twists in a sickly purple darkness—moving closer with every breath, every hint of life...",
                    Choices = ("Remain", "Wake Up"),
                    Take = "You allow the energies to absorb within you.",
                    Description = "",
                    Apply = (duel, i) =>
                    {
                        duel.Win = duel.Duelists[0].Total < duel.Duelists[1].Total ? 0 :
                            duel.Duelists[0].Total > duel.Duelists[1].Total ? 1 :
                            -1;

                        return true;
                    }                   
                }},
            { 3, new Secret()
                {
                    //reroll 1s and 20s
                    Name = "The Balancing Stone (dumb name change)"
                }
            }
        };

        static DataMap()
        {
            Console.Write("Getting bestiary...");
            var creatures = File.ReadAllText(@"D:\PFData\Bestiary.json");
            BaseCampaign.Bestiary = JsonConvert.DeserializeObject<List<Creature>>(creatures)!;
            Console.WriteLine($"Creatures => {BaseCampaign.Bestiary.Count}");

            Console.Write("Getting items...");
            var items = File.ReadAllText(@"D:\PFData\Items.json");
            BaseCampaign.Items = JsonConvert.DeserializeObject<List<Item>>(items)!;
            Console.WriteLine($"Items => {BaseCampaign.Items.Count}");

            Console.Write("Getting rules...");
            var rules = File.ReadAllText(@"D:\PFData\Rules.json");
            BaseCampaign.Rules = JsonConvert.DeserializeObject<List<Rule>>(rules)!;
            Console.WriteLine($"Rules => {BaseCampaign.Rules.Count}");

            Console.Write("Getting shapes...");
            var shapes = File.ReadAllText(@"D:\PFData\Shapes.json");
            BaseCampaign.Shapes = JsonConvert.DeserializeObject<List<Shape>>(shapes)!;
            Console.WriteLine($"Shapes => {BaseCampaign.Shapes.Count}");

            Console.Write("Getting spells...");
            var spells = File.ReadAllText(@"D:\PFData\Spells.json");
            BaseCampaign.Spells = JsonConvert.DeserializeObject<List<Spell>>(spells)!;
            Console.WriteLine($"Spells => {BaseCampaign.Spells.Count}");
        

            foreach(Creature creature in BaseCampaign.Bestiary)
                autoCompleteCreatures.Add(new AutocompleteResult(creature.Name, creature.Name));
            
            foreach(Item item in BaseCampaign.Items)
                autoCompleteItems.Add(new AutocompleteResult(item.Name, item.Name));

            foreach(Rule rule in BaseCampaign.Rules)
                autoCompleteRules.Add(new AutocompleteResult(rule.Name, rule.Name));       

            foreach(Shape shape in BaseCampaign.Shapes)
                autoCompleteShapes.Add(new AutocompleteResult(shape.Name, shape.Name));

            foreach(Spell spell in BaseCampaign.Spells)
                autoCompleteSpells.Add(new AutocompleteResult(spell.Name, spell.Name));

            Console.WriteLine("done.");







        }  
    
    }
}
