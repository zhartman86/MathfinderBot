using Discord.Interactions;

namespace MathfinderBot.Slash
{
    public class Variable : InteractionModuleBase
    {
        public enum VarAction
        {

        }
        
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Variable(CommandHandler handler) => this.handler = handler;

        [SlashCommand("var", "Get a defined stat")]
        public async Task Var(string var)
        {
            var user = Context.Interaction.User;
            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null) { await RespondAsync("No active character", ephemeral: true); return; }

            await RespondAsync(Pathfinder.Active[user][var].ToString());
        }
    }
}
