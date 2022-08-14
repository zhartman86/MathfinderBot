using Discord;
using Discord.Interactions;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {

        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Help(CommandHandler handler) => this.handler = handler;


        [SlashCommand("help-mf-basics", "Short rundown of the basic functionality of Mathfinder")]
        public async Task HelpBasicsCommand()
        {
            var helpString = 
        @" GETTING STARTED
        1. /char =>NEW =>Character Name
        2. /char =>SET =>Character Name

        These two commands will create a character and set to active.

        /var has several options, two of which will list your current variables: 

        STATS
        These are variables that represent an integer, as well as a list of bonuses currently applied to it.

        EXPRESSIONS
        These are static formulae which share a name-pool with your stats. They can represent a constant value or an expression, including expressions of expressions.

        For example, you may wish to calculate your attack bonus by creating an expression named 'ATTACK' with a value of:

        BAB + STR + SIZE_MOD. 

        ATTACK, in this case, would always keep an up-to-date sum of these 3 variables. You can also use ATTACK in another variable--for instance:

        ATTACK + 1d20

        This would create an expression that, when evaluated, would automatically ""roll"" a d20 and add the modifier from ATTACK.

        /eval

        EVALULATION
        This is where you can create a mathematical formula using the aforementioned variables, or just calculate a pre-made expression by calling its variable name. Currently, this includes + - * / as well as dice expressions (ie 1d20).";

            await RespondAsync(helpString, ephemeral: true);
        }
    }
}
