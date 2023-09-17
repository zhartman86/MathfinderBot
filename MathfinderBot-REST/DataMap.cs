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

        static DataMap()
        {
            Console.Write("Getting bestiary...");
            var creatures = File.ReadAllText(@"E:\Pathfinder\PFData\Bestiary.json");
            BaseCampaign.Bestiary = JsonConvert.DeserializeObject<List<Creature>>(creatures)!;
            Console.WriteLine($"Creatures => {BaseCampaign.Bestiary.Count}");

            Console.Write("Getting items...");
            var items = File.ReadAllText(@"E:\Pathfinder\PFData\Items.json");
            BaseCampaign.Items = JsonConvert.DeserializeObject<List<Item>>(items)!;
            Console.WriteLine($"Items => {BaseCampaign.Items.Count}");

            Console.Write("Getting rules...");
            var rules = File.ReadAllText(@"E:\Pathfinder\PFData\Rules.json");
            BaseCampaign.Rules = JsonConvert.DeserializeObject<List<Rule>>(rules)!;
            Console.WriteLine($"Rules => {BaseCampaign.Rules.Count}");

            Console.Write("Getting shapes...");
            var shapes = File.ReadAllText(@"E:\Pathfinder\PFData\Shapes.json");
            BaseCampaign.Shapes = JsonConvert.DeserializeObject<List<Shape>>(shapes)!;
            Console.WriteLine($"Shapes => {BaseCampaign.Shapes.Count}");

            Console.Write("Getting spells...");
            var spells = File.ReadAllText(@"E:\Pathfinder\PFData\Spells.json");
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
