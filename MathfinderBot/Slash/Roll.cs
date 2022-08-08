using Discord;
using Discord.Interactions;
using Gellybeans.Dice;

namespace MathfinderBot
{
    public class Roll : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Roll(CommandHandler handler) => this.handler = handler;

        [SlashCommand("roll", "XdY type expression with optional +- modifier")]
        public async Task RollDice(string expression)
        {
            var diceExpr = new DiceExpression(expression);
            if(diceExpr.Results == null) return;

            var embeded = new EmbedBuilder()
                .WithTitle(diceExpr.TotalResult.ToString())
                .WithDescription
                (
                    diceExpr.ToString() + "\n\n" +
                    Format.Bold(diceExpr.Results.ToString())
                )
                .WithColor(Color.Red);



            await RespondAsync(embed: embeded.Build());
        }

       
    }
}
