using Gellybeans.Pathfinder;
using MongoDB.Driver;

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
                stats.ValueChanged -= UpdateValue;
                
            Active[id] = stats;
            stats.ValueChanged += UpdateValue;
        }
          
        public static void UpdateValue(object? sender, string value)
        {
            var stats = (StatBlock)sender!;
            UpdateDefinition<StatBlock> update = null!;
            switch(value)
            {
                case "stats":
                    update = Builders<StatBlock>.Update.Set(x => x.Stats, Active[stats.Owner].Stats);
                    break;
                case "inv":
                    update = Builders<StatBlock>.Update.Set(x => x.Inventory, Active[stats.Owner].Inventory);
                    break;
                case string val when val.Contains("stat:"):
                    var statName = value.Split(':')[1];
                    update = Builders<StatBlock>.Update.Set(x => x.Stats[statName], Active[stats.Owner].Stats[statName]);
                    break;
                case string val when val.Contains("expr:"):
                    var exprName = value.Split(':')[1];
                    update = Builders<StatBlock>.Update.Set(x => x.Expressions[exprName], Active[stats.Owner].Expressions[exprName]);
                    break;
                case string val when val.Contains("row:"):
                    var rowName = value.Split(':')[1];
                    update = Builders<StatBlock>.Update.Set(x => x.ExprRows[rowName], Active[stats.Owner].ExprRows[rowName]);
                    break;
                case string val when val.Contains("grid:"):
                    var gridName = value.Split(':')[1];
                    update = Builders<StatBlock>.Update.Set(x => x.Grids[gridName], Active[stats.Owner].Grids[gridName]);
                    break;
            }

            if(update != null)
                Program.UpdateSingle(update, stats.Owner);
        }
    }
}

