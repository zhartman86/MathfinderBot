using Gellybeans.Pathfinder;
using MongoDB.Driver;
using System.Data;

namespace MathfinderBot
{
    public static class Characters
    {
        public static Dictionary<ulong, List<StatBlock>>    Database    = new Dictionary<ulong, List<StatBlock>>();  
        public static Dictionary<ulong, StatBlock>          Active      = new Dictionary<ulong, StatBlock>();        
        public static Dictionary<ulong, Init>               Inits       = new Dictionary<ulong, Init>();
        
        async public static void SetActive(ulong id, StatBlock stats)
        {           
            if(Active.ContainsKey(id))
                stats.ValueChanged -= UpdateStatBlock;

            Active[id] = stats;
            stats.ValueChanged += UpdateStatBlock;
        }
   
        public async static void UpdateStatBlock(object? sender, string varName)
        {
            var stats = (StatBlock)sender!;
            await Program.UpdateStatBlock(stats);
        }
        
        public async static void UpdateValue(object? sender, string value)
        {
            var stats = (StatBlock)sender!;
            
            
            //await Program.UpdateSingleAsync(definiton, stats.Owner);
        }
    }
}

