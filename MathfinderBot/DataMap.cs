using Discord;
using Gellybeans;
using Gellybeans.Pathfinder;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
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
                    Apply = async (duel, active, i) => await Task.Run(() =>
                    {
                        var r = new Random().Next(-1,3);
                        duel.Duelists[i].Total += r;
                        duel.Duelists[i].Events += $"\r\nYou strike for {r} damage!";
                        return true;
                    })
                }},
            { 2, new Secret()
                {
                    //lowest wins.
                    Index = 2,
                    Name = "Reverse Gravity",
                    EventString = "The ground drops beneath your feet as you drift away. Before you realize what's happening, a shocking *thud* breaks your ascent. You've *landed* on the ceiling.\r\nWith a semblance of orientation, you spot something strange. A whispy, morphic entity twists in a sickly purple darkness—moving closer with every breath you inhale, every hint of life...",
                    Choices = ("Remain", "Wake up"),
                    Take = "You allow the energies to absorb within you.",
                    Description = "",
                    Apply = async (duel, active, i) => await Task.Run(() =>
                    {
                        duel.Winner = duel.Duelists[0].Total < duel.Duelists[1].Total ? 0 :
                            duel.Duelists[0].Total > duel.Duelists[1].Total ? 1 :
                            -1;

                        return true;
                    })
                }},
            { 3, new Secret()
                {
                    //chance to force a d20 expression, reroll if so
                    Index = 3,
                    Name = "Balancing Stone",
                    EventString = "You find yourself in a windswept field of lush violet-green grasses, bowing and bending to forces uncontrollable. In contrast, you see a pile of smooth rocks stacked twenty tall—unmoving. Each stone, in their own way, had learned to walk the chaotic winds around their well-worn shape. You think of the time it must have taken for realizations to occur...",
                    Choices = ("Take a stone", "Leave the formation undisturbed"),
                    Take = "You reach out and take the topmost stone without tipping the rest.\r\n\r\nThe small stone retains a semblence of its former uneroded figure. Many fairly equal faces surround its wind-beveled edges.",
                    Description = "A smooth stone that retains a hint of its former shape.",
                    Properties = new Dictionary<string, string>() { { "hasParasol", "false" } },
                    Apply = async (duel, active, i) => await Task.Run(async () =>
                    {
                        var applied = false;
                        var r = new Random().Next(1,101);
                        if(r >= 75 && duel.Expression != "1d20" && duel.Expression != "d20")
                        {
                            duel.Expression = "d20";
                            duel.Duelists[0].Total = new Random().Next(1,21);
                            duel.Duelists[1].Total = new Random().Next(1,21);                           
                            applied = true;
                        }

                       
                        
                        return applied;
                    })
                }
            },
            { 4, new Secret()
                {
                    Index = 4,
                    Name = "Weatherywind",
                    EventString = "Tossing in the wind with a life of its own, a parasol dances across grassy field. Its cover reflects a subtle irridescence, lined with frilly cloth with ribbons tied to its edges.",
                    Choices = ("Catch it", "Let it blow by"),
                    Take = "You wait for the right opportunity to reach out and grab the erradic parasol, but it senses your wanting.\r\n\r\nInstead, it slows to a steady float and then glides carefully into your grasp.",
                    Description = "A frillish, vibrant parasol.",
                    Apply = async (duel, active, i) => await Task.Run(async () =>
                    {
                        
                        return true;
                    })
                    
                }
            },
            { 5, new Secret()
                {
                    Index = 5,
                    Name = "Vampire",
                    Apply = async (duel, active, i) => await Task.Run(async () =>
                    {
                        return true;
                    })
                }    
            },
            { 6, new Secret()
                { 
                    Index = 6,
                    Name = ""
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
