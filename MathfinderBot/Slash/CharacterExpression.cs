using Discord;
using Discord.Interactions;
using Gellybeans.Expressions;

namespace MathfinderBot
{
    public class CharacterExpression : InteractionModuleBase
    {
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public CharacterExpression(CommandHandler handler) => this.handler = handler;

        [SlashCommand("eval", "Do math using character variables.")]
        public async Task CharExpression(string expr)
        {
            Console.WriteLine(expr);
            var user = Context.Interaction.User;
            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var parser = Parser.Parse(expr);
            var result = parser.Eval(Pathfinder.Active[user]);

            var builder = new EmbedBuilder()
                .WithTitle(result.ToString())
                .WithDescription(expr);

            await RespondAsync(embed: builder.Build());


        }
    }
}
