using Discord.Interactions;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {
        public enum HelpOptions
        {
            Basics,
            Character,
            Eval,
            Functions,
            Bonus,
            Row,
            DM,
        }
        
        
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Help(CommandHandler handler) => this.handler = handler;

        const string basics = 
@"__GETTING STARTED__
To create a character type /char and select the 'New' option in the first 
field, as well as specifying a name in the `char-name` field (You can TAB 
through  the different options). 

The `game` field is, by default, is set to Pathfinder. Be sure to change
this if you want another system (or none).

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

There are other options than those listed here, but this will get you started!";

        const string bonus =
@"__BONUSES__
Bonuses are special values built into every `Stat`. This is so you may boost its 
total value without losing track of its base amount. This can be helpful for temporary 
boosts, or permanent bonuses given by equipment, or anything else that may be removed 
later.

Every bonus has `name`, `type`, and `value`.

You can apply a bonus through `/eval` like so:

    `STR_TEMP +$ BULLS:ENHANCEMENT:4`

This adds a bonus of `name:BULLS`, `type:ENHANCEMENT`, and `value:4`  to 
`STR_TEMP`. 

To remove the same bonus:

    `STR_TEMP -$ BULLS`

This will remove all bonuses with the `BULLS` name from the specified stat.

To check for a particular bonus on a stat, you can do:

    `STR_TEMP $ ENHANCEMENT`

**STACKING RULES**—The types are currently built for Pathfinder, but can be ignored
by using `0` (TYPELESS) as the type. Also—bonuses with the same name applied to the 
same stat will not stack.";

        const string eval =
@"__EVAL__
Eval is essentially a math engine that links to your active character sheet in order
to 'evaluate' different `Stats`, `Expressions`, or `Constants`. 

The currently implemented operators are:

`+` `-` `*` `/` `>` `<` `==` `!=` `<=` `>=` `%` `()` `=` `+=` `-=` `*=` `/=` `&&` `||` `?:` `+$::` `-$`. 

`$` is a special  operator for changing bonuses, which can be read about in Bonuses.

    `/eval expr:1 + 1` 

would evaluate to `2`, of course. 

*REMEMBER*—you can just hit TAB to move to the next option in a slash-command. You do not 
have to type `/eval expr:` every time. Simply type  `/eval` and TAB over to the next field.
`expr:` will be assumed.

    /eval `1d6` 

will pick a random number between 1-6 as if rolling a 6-sided die.

    /eval `FORT_BASE`

will get the same value from your active character sheet. If the value is not found, it 
returns `0` instead.

    /eval `1d20 + FORT_BASE` 

would roll a 20-sided die and add the value from FORT_BASE to it.

You can take an expression like:
    
    `1d20 + FORT_BASE + ((CON_SCORE - 10) / 2)`

and store it to a separate expression. You could call it something like `FORT` for easy
access.

*KEEP IN MIND*—Stats can be modified with /eval, like `STR_SCORE = 10`, but to create or 
change an Expression, you  must use the `/var` command and pick the `Set-Expression` option.

You could store an expression named `DEX` to calculate the modifier for your ability score:

    `(DEX_SCORE - 10) / 2`

Or you could use the built-in function `mod` to do the same thing:

    `mod(DEX_SCORE)`

This will automatically use the above formula to return the mod.

*OTHER STUFF*—`TRUE` and `FALSE` are special values which always return 1 and 0 respectively.";

        const string character =
@"__CHARACTER__
Your character sheet is a collection of stats, expressions, expression-rows,
and grids (multi-rows). Use the Basics help option if you need help getting
started with creation.

You can add, change, remove these variables manually, as well as use 
an exported Pathbuilder character sheet PDF (if you're using Pathfinder,
of course). to update a character sheet using a PDF, use the `/update` 
command. The second option will let you drag-and-drop a file for use.

The weapons section of the Pathbuilder sheet will be parsed as well.
This includes the name of the weapon, the attack field, and damage.
This will attempt to create a row with the name, a `HIT` button, as
well as a `DMG` button to represent the attack and damage rolls.
These weapons/attacks can be accessed by using `/row` followed by the
name in the second field.

You can change the Attack and Damage fields to access variables on 
your sheet. If you wanted to use—for instance—`ATK_S` instead of the 
already-calculated hit formula, you could more easily track your attack
bonus in ""real-time"", as any temporary bonuses made to it would be 
added automatically.

I will try to add support for other sheets/rule-sets in the future!
";

        const string dm =
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

*KEEP IN MIND*— /eval can be used to change stats, apply/remove bonuses and penalties from a
character's statblock.  
";

        const string row =
@"__ROWS AND GRIDS__
Rows are sets of buttons (up to 5 per row) that are saved expressions. These can
reference expressions or values from your character sheet, or be entirely unique.

You can create a row using the `/var` command, followed by a `Set-Row` in the 
first field, folllowed by the desired name in the second. This will pop up a 
window with 5 fields. Each field represents a possible button, which when clicked, 
will automatically evaluate the typed expression.

When setting a row, the syntax is as follows:

    `LABEL:EXPR`

For example, if you did `HIT:1d20+STR`, it would represent a button with `HIT` for 
a label. When the button is clicked, it will run the expression `1d20+STR`.

After creating a row, you can call it using `/row` followed by the name of the row.


One use of rows is to represent a weapon. You can create one expression to that rolls
a hit, another for damage. A Greatsword could be created like so:

    `HIT:1d20+ATK_S`
    `DMG:2d6+STR`
    `CRT:(2d6*2)+STR`

This would create a row of buttons you could call by name with /row.

";

        const string functions =
@"__FUNCTIONS__
Functions are built-in variables that take arguments and return numbers. 
They can be called directly using `/eval`, or added to expressions.

The currently available functions are:

    `abs(x)` — Returns the absolute value of x
    `clamp(x,y,z)` — Returns value x, clamped between y and z
    `if(x,y)` — Returns y if x is TRUE (1), otherwise returns 0
    `max(x,y)` — Returns biggest number between x and y
    `min(x,y)` — Returns smallest number between x and y
    `mod(x)` — Returns the ability score modifier of x
    `rand(x,y)` — Returns a random number between x and y
";

        [SlashCommand("mf-help", "A rundown of different features built into Mathfinder")]
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
                case HelpOptions.Functions:
                    await RespondAsync(functions, ephemeral: true);
                    break;
                case HelpOptions.Character:
                    await RespondAsync(character, ephemeral: true);
                    break;
                case HelpOptions.DM:
                    await RespondAsync(dm, ephemeral: true);
                    break;
                case HelpOptions.Row:
                    await RespondAsync(row, ephemeral: true);
                    break;
            }           
        }       
    }
}
