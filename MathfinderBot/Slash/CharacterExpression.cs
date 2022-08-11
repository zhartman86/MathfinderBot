using System.Text;
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
            if(!Pathfinder.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();

            var parser = Parser.Parse(expr);
            var result = parser.Eval(Pathfinder.Active[user], sb);


            var builder = new EmbedBuilder()
                .WithTitle(result.ToString())
                .WithDescription(expr + "\n\n" + sb.ToString());

            Console.WriteLine(sb.ToString());

            await RespondAsync(embed: builder.Build());


        }
    }
}
