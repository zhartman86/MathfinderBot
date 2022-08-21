using Discord;
using Discord.Interactions;
using System.Text;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {

        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Help(CommandHandler handler) => this.handler = handler;


        [SlashCommand("mf-help-basics", "Short rundown of the basic functionality of Mathfinder")]
        public async Task HelpBasicsCommand()
        {
            var helpString =
@"__GETTING STARTED__
To create a character type /char and select the 'New' option in the first field, 
as well as specifying a name in the next field. This will create a character and 
set it to active.

*OPTIONALLY* In the `options` field, you can set your base ability scores by typing
a comma separated group of numbers like `10,11,12,13,14,15` to represent 
`STR_SCORE`,`DEX_SCORE`,`CON_SCORE`,`INT_SCORE`,`WIS_SCORE`,`CHA_SCORE`.
            
Once you have an active character, there are several more commands you may use.
This includes: /var, /eval, /row, /grid
            
__/eval__
This lets you 'evaluate' stats and expressions, as well as assign a number to 
a stat using the `=` operator. Other operators: `+ - * / > < == != <= =< %`, 
as well as dice expressions: `1d20`. You can use () to indicate order of operations. 
This is also useful for multiplying dice: `(2d6*2) + 5*2` is the equivilent of `4d6 + 10.` 
            
Expressions and Stats share a name-pool, but represent different types of values. 
            
A `Stat` is an integer that contains both a `base` value, and a `bonus` value. 
This allows for tracking multiple mods to a particular value.
            
`Expressions` represent stored fomulae: `1d20 + 5`. This example, when called, would 
automatically ""roll"" a value between 1-20 and add 5. You can do anything from static 
values to complex equations using Expressions of Expressions.


__/var__
This can be used to set/delete/view the different sets of variables, as well as creating or 
changing Expressions. There are many sub-options, or `modes` to choose from.

__/row__
/row will let you set up to 5 rows of saved expressions, represented by buttons. You can 
create rows by using /row-set. 

/grid can be used to call a set of saved rows.

There are other options than those listed here, but this will get you started using Mathfinder!";

            //using var stream = new MemoryStream(Encoding.ASCII.GetBytes(helpString));
            //await RespondWithFileAsync(stream, "help-basics.txt",  ephemeral: true);
            await RespondAsync(helpString, ephemeral: true);
        }
    }
}
