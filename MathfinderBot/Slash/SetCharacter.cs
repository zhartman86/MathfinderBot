using Discord.Interactions;

namespace MathfinderBot
{
    public class SetCharacter : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public SetCharacter(CommandHandler handler) => this.handler = handler;


        [SlashCommand("set-char", "Set active character")]
        public async Task Command(string character)
        {
            var user = Context.Interaction.User;
            if(Pathfinder.Database.ContainsKey(user) && Pathfinder.Database[user].ContainsKey(character))
            {
                Pathfinder.Active[user] = Pathfinder.Database[user][character];
                await RespondAsync("Character " + character + " set!");
            }
            else
            {
                await RespondAsync("Character not found");
            }
        }

    }
}
