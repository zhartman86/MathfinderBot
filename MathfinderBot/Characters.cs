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
                await Unsubscribe(stats);
                  
            Active[id] = stats;
            await Subscribe(Active[id]);     
        }
   
        public async static void UpdateStatBlock(object? sender, string varName)
        {
            var stats = (StatBlock)sender!;
            await Program.UpdateStatBlock(stats);
        }
    
        public async static void UpdateInventory(object? sender, object? info)
        {
            var stats = (StatBlock)sender!;
            await Program.UpdateSingleAsync(Builders<StatBlock>.Update.Set(x => x.Inventory, stats.Inventory), stats.Owner);
        }

        public async static Task Subscribe(StatBlock stats)
        {
            await Task.Run(() => {
                stats.ValueChanged += UpdateStatBlock;
                stats.InventoryChanged += UpdateInventory;});         
        }
    
        public async static Task Unsubscribe(StatBlock stats)
        {
            await Task.Run(() => {
                stats.ValueChanged      -= UpdateStatBlock;
                stats.InventoryChanged  -= UpdateInventory;});
        }
    }
}

