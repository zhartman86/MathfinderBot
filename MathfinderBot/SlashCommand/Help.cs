using System;
using Discord;
using Discord.Interactions;
using System.Text;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {
        public enum HelpOptions
        {
            Basics,
            Eval,
            Character,
            Bonus,
            DM,
        }
        
        
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Help(CommandHandler handler) => this.handler = handler;

        static string basics = 
@"__GETTING STARTED__
To create a character type /char and select the 'New' option in the first 
field, as well as specifying a name in the `char-name` field (You can TAB 
through  the different options). This will create a character and set it 
to active. 

*OPTIONALLY* In the `options` field, you can set your base ability scores 
by typing a comma separated group of numbers like `10,11,12,13,14,15` to 
represent  `STR_SCORE`,`DEX_SCORE`,`CON_SCORE`,`INT_SCORE`,`WIS_SCORE`,
and `CHA_SCORE`.
            
Once you have an active character, there are several more commands you may 
use. This includes: /var, /eval, /row, /grid
            
__/eval__
This lets you 'evaluate' stats and expressions, as well as assign a number to 
a stat using the `=` operator. There are many other operators, as well as dice 
expressions: `1d20`. You can use () to indicate order of  operations. This is 
also useful for multiplying dice: `(2d6*2) + 5*2` is the equivilent of `4d6 + 10.` 
            
Expressions and Stats share a name-pool, but represent different types of values. 
            
A `Stat` is an integer that contains both a `base` value, and a `bonus` value. 
This allows for tracking multiple mods to a particular value.
            
`Expressions` represent stored fomulae: `1d20 + 5`. This example, when called, 
would  automatically ""roll"" a value between 1-20 and add 5. You can do anything 
from static values to complex equations using Expressions of Expressions.

__/var__
This can be used to set/delete/view the different sets of variables, as well as 
creating or changing Expressions. There are many sub-options, or `modes` to 
choose from in the first field.

__/row__
This will let you set up to 5 rows of saved expressions, represented by buttons. 
You can create rows by using /var and 'Set-Row'

/grid can be used to call a set of saved rows.

There are other options than those listed here, but this will get you started using Mathfinder!";

        static string bonus =
@"__BONUSES__
Bonuses are special values built into every `Stat`. This is so you may boost its total 
value without losing track of its base amount. This can be helpful for temporary boosts, 
or permanent bonuses given by equipment (something you may remove later).

Every bonus has `name`, `type`, and `value`. The types are currently built for Pathfinder, 
but can be ignored by using the `Typeless` (0) type. You can apply a bonus through /eval 
like so:

    `STR_SCORE +$ BULLS:7:4`

This indicates to add a bonus of `name:BULLS`, `type:7` (Enhancement), and `value:4`.
You can remove the same bonus by doing:

    `STR_SCORE -$ BULLS`

This will effectively remove all bonuses with the same name from the stat.

*KEEP IN MIND—*Bonuses with the same name applied to the same stat will not stack!

There is also a /bonus command to do the same thing, as well as apply bonuses to multiple
targets at a time.
";

        static string eval =
@"__EVAL__
Eval is essentially a math engine that links to your active character sheet in order
to 'evaluate' different `Stats` and `Expressions`. The currently implemented operators 
are: `+` `-` `*` `/` `>` `<` `==` `!=` `<=` `>=` `%` `()` `=` `+=` `-=` `*=` `/=` `?:` 
`-$::` `+$::`. `$` is a special  operator for changing bonuses, which can be read about 
in the bonus help option.

    `/eval expr:1 + 1` 

would evaluate to `2`, of course. 

Remember—you can just hit TAB to move to the next option in a slash-command. You do not 
have to type `/eval expr:` every time. Simply type  `/eval` and move over to the next to 
the next field. `expr:` will be assumed for now on.

    /eval `1d6` 

will pick a random number between 1-6 as if rolling a 6-sided die.

    /eval `SAVE_FORT` 

will get the same value from your active character sheet. If the value is not found, it 
returns `0` instead.

    /eval `1d20 + SAVE_FORT` 

would roll a 20-sided die and add the value from SAVE_FORT to it.

You can take an expression like:
    
    `1d20 + SAVE_REFLEX + ((DEX_SCORE - 10) / 2)` 

and store it to another variable as a way of not only tallying your total values to an ability, 
but rolling the check as well! 

Stats can be modified with /eval, like `STR_SCORE = 10`, but to create or change an Expression, you 
must use the `/var` command and pick the `Set-Expression` option.

You could store an expression named `DEX` to calculate the modifier for your ability score:

    `(DEX_SCORE - 10) / 2`

Or you could use the built-in function `mod` to do the same thing

    `mod(DEX_SCORE)`

Will automatically use the above formula to return the appropriate number. There are a number of
other functions like `min(x,y)`, `max(x,y)`, `clamp(val,min,max)`, `abs(x)`, `rand(min,max)`

`TRUE` and `FALSE` are special values which always return 1 and 0 respectively.";

        static string character =
@"__CHARACTER__
Your character sheet stores different sets of values that you can access.

__Char__
/char gives you access to a few options for character management. Remember that you must `Set` a
character before it becomes active for use.

__Stats__
Stats are a set of integers, each referenced by a variable name such as `STR_SCORE`. They store both 
a base and bonus value. Several bonuses can be applied to a single stat, each with their own names
and types to determine how they stack.

__Expressions__
These are stored procedures such as `10 + 10`, or `(STR SCORE - 10) / 2`. Expressions share names with
Stats.

__Rows__
A row is a set of expressions represented by buttons that you can call with `/row`. You can set them
using the /var command and subsequent `Set-Row` selection.

__Grids__
You can create up to a 5x5 set of buttons using `/grid`. This is a stored set of Row names that can 
be called by a single variable. Like rows, you can set them with /var. 

";

        static string dm =
@"__DM STUFF__

__/req__
A DM can call for a roll using the `/req` command, followed by an expression. You can include
anything that might be on a character sheet. Once the command is executed, a button will 
appear calling for the roll. Players can click on it to execute the evaluation against their
own sheets.

__/sec__
This works exactly like `/eval` except there is an additional `target` field and the roll is
made in secret. If the target field is empty, it will check for any character sheet the DM has
active.
";



        [SlashCommand("mf-help", "Short rundown of the basic functionality of Mathfinder")]
        public async Task HelpCommand(HelpOptions options)
        {
            switch(options)
            {
                case HelpOptions.Eval:
                    await RespondAsync(eval, ephemeral: true);
                    break;
                case HelpOptions.Bonus:
                    await RespondAsync(bonus, ephemeral: true);
                    break;
                case HelpOptions.Basics:
                    await RespondAsync(basics, ephemeral: true);
                    break;
                case HelpOptions.Character:
                    await RespondAsync(character, ephemeral: true);
                    break;
                case HelpOptions.DM:
                    await RespondAsync(dm, ephemeral: true);
                    break;
            }           
        }       
    }
}
