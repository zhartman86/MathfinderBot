using Discord;
using Gellybeans.Expressions;
using Gellybeans.Pathfinder;
using MongoDB.Driver;
using Newtonsoft.Json;

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
        public readonly static List<AutocompleteResult> autoCompleteXp          = new List<AutocompleteResult>();

        public readonly static Dictionary<ulong, ReadScope> channelVars = new Dictionary<ulong, ReadScope>();

        static DataMap()
        {
            Console.WriteLine("GENERATING ITEMS FROM EVAL");            
            var itemArray = new dynamic[Item.Items.Count];
            var itemDict = new Dictionary<string, int>();
            for(int i = 0; i < Item.Items.Count; i++)
            {
                var item = Item.Items[i].ToArrayValue();
                if(itemDict.TryAdd(item["NAME"], i))
                    itemArray[i] = item;
            }


            Console.WriteLine("GENERATING SPELLS FROM EVAL");
            var spellArray = new dynamic[Spell.Spells.Count];           
            var spellDict = new Dictionary<string, int>();
            for(int i = 0; i < Spell.Spells.Count; i++)
            {
                var spell = Spell.Spells[i].ToArrayValue();
                if(spellDict.TryAdd(spell["NAME"], i))
                    spellArray[i] = spell;
            }

            Console.WriteLine(spellArray.Length);


            channelVars.Add(1144662955892416562, new ReadScope(new Dictionary<string, dynamic>()
            {
                { "__ITEMS",    new ArrayValue(itemArray, itemDict)},
                { "__SPELLS",   new ArrayValue(spellArray, spellDict)}
            }));






            Task.Run(GetXps);

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
    
        public static async Task GetXps()
        {
            autoCompleteXp.Clear();
            Console.Write("Getting Xps...");
            var result = await Program.GetXp().Find(_ => true).ToListAsync();
            result.Sort((x, y) => x.Name.CompareTo(y.Name));
            foreach(var xp in result)
                autoCompleteXp.Add(new AutocompleteResult(xp.Name.Remove(0,4), xp.Name));
            Console.WriteLine("done.");
        }
    }
}