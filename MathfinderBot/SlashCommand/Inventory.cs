using Discord.Interactions;
using Discord;
using Gellybeans.Pathfinder;
using MongoDB.Driver;


namespace MathfinderBot
{
    public class Inventory : InteractionModuleBase
    {
        public enum InventoryAction
        {
            Add,
            List
        }
        
        
        ulong user;
        CommandHandler handler;
        IMongoCollection<StatBlock> collection;

        static Dictionary<ulong, string> lastInputs = new Dictionary<ulong, string>();

        public Inventory(CommandHandler handler) => this.handler = handler;

        public async override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            collection = Program.database.GetCollection<StatBlock>("statblocks");

            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }
        }


        [SlashCommand("inv", "Inventory mangement")]
        public async Task InventoryCommand(InventoryAction action)
        {

        }
            
    
    }
    
   
}
