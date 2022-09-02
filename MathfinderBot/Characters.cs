using Discord;
using Gellybeans.Pathfinder;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Linq.Expressions;


namespace MathfinderBot
{
    public static class Characters
    {
        
        public static Dictionary<ulong, Dictionary<string, StatBlock>>  Database    = new Dictionary<ulong, Dictionary<string, StatBlock>>();       
        public static Dictionary<ulong, StatBlock>                      Active      = new Dictionary<ulong, StatBlock>();               
        
        public static Dictionary<ulong, Init> Inits = new Dictionary<ulong, Init>();
        
        public static void SetActive(ulong id, StatBlock statblock)
        {           
            if(Active.ContainsKey(id)) 
                statblock.ValueChanged -= UpdateStatBlock;
            
            Active[id] = statblock;
            statblock.ValueChanged += UpdateStatBlock;
            
        }
    
    
        public async static void UpdateStatBlock(object? sender, string varName)
        {
            var stats = (StatBlock)sender!;
            await Program.UpdateStatBlock(stats);
        }
    }
}

