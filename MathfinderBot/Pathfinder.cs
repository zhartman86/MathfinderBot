using Discord;
using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public static class Pathfinder
    {
        public static Dictionary<ulong, Dictionary<string, StatBlock>>  Database    = new Dictionary<ulong, Dictionary<string, StatBlock>>();       
        public static Dictionary<ulong, StatBlock>                      Active      = new Dictionary<ulong, StatBlock>();               
    
        
        public static void SetActive(ulong id, StatBlock statblock)
        {
            if(Active.ContainsKey(id)) statblock.ValueAssigned -= UpdateStat;
            
            Active[id] = statblock;
            statblock.ValueAssigned += UpdateStat;
        }
    
    
        public async static void UpdateStat(object? sender, string statName)
        {
           await Program.UpdateOneStat((StatBlock)sender!, statName);
        }
    }
}

