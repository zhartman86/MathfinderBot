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
            Bonus,
            Row,
            Inventory,
            DM,
        }
        
        public InteractionService   Service { get; set; }
        private CommandHandler      handler;
        
        public Help(CommandHandler handler) => this.handler = handler;

        const string basics =
@"__GETTING STARTED__
To create a character type `/char` and select the 'New' option in the first 
field, as well as specifying a name in the `char-name` field (You can TAB 
through  the different options). game mode is Pathfinder by default.

You can use `/char-update` to upload a copy of your character sheet from
different programs.



[Check out the documentation for more detailed info.](https://github.com/Gellybean/Mathfinder-Bot)";


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
by using `0` (TYPELESS) as the type. KEEP IN MIND—bonuses with the same name applied to the 
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

    /eval `FORT_BONUS`

will get the same value from your active character sheet. If the value is not found, it 
returns `0` instead.

    /eval `1d20 + FORT_BONUS` 

would roll a 20-sided die and add the value from FORT_BASE to it.

You can take an expression like:
    
    `1d20 + FORT_BONUS + ((CON_SCORE - 10) / 2)`

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
of course). to update a character sheet using a PDF, use the `/char-update` 
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
@"__ROWS__
Typing out every saved expression is not practical most of the time. This is
where rows can be a useful time saver. Rows are sets of buttons (up to 5 per 
row) that contain expressions. These can reference other expressions or values 
from your character sheet—or be entirely unique.

You can create a row using the `/var` command, followed by a `Set-Row` in the 
first field, with desired name in the second. This will pop up a window with 
5 fields. Each field represents a possible button, which when clicked, will 
automatically evaluate the typed expression.

When setting a row, the syntax is as follows:

    `LABEL:EXPRESSION`

For example, `HIT:1d20+STR` would represent a button with `HIT` for a label. When 
the button is clicked, it will run the expression `1d20+STR`. **Labels are optional**.

One use of rows is to represent a weapon. You can create one expression to that rolls
a hit, another for damage. A Greatsword could be created like so:

    `HIT:1d20+ATK_S`
    `DMG:2d6+STR`
    `CRT:(2d6*2)+STR*2`

After creating an expr row, you can call it using `/row` followed by the name in the
`rowOne` You can call up to five rows at a time using the subsequent fields, however
you can save a set of rows using grids instead...

__GRIDS__
Grids are an expansion of rows. when creating a grid with the `Set-Grid` sub-option 
of /var,  you can input a row name in each field (up to 5). When called with `/grid`, 
it will stack each row in an (up to) 5x5 grid of buttons per message.

";
        const string inv =
@"
__INVENTORY__
While quite basic in scope, `/inv` should provide a quick and easy way to keep
a list of items.

When adding an item by any means, the syntax is as follows:

    `NAME:WEIGHT:VALUE`

You can ignore the weight and value fields if you don't wish to track those values.
Optionally, you can separate your values with commas or tab-spacing.

__Add__
When adding an item, you can use the syntax used above in the `item` field of /inv,
or you can leave the field blank. This will bring up a modal where you can add many
items at once. Each item must be placed on a separate line.

__Remove__
You can remove an item either by index (this number can be found when using the `List`
option), or by name. When removing by name, this will look for the first item with
the same name and remove it. You can remove several items with the same name at once
by using the `qty` field.

__Importing/Exporting__
You can use these options to quick save or update an inventory list.

When importing a list of items, they must be presented in a line-separated text file.

*BE CAREFUL!*—When importing a list, it will override your **entire inventory!** If you wish
to add several items at once, use the Add option while leaving the item field blank. this
will let you copy/paste many items at once.
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
                case HelpOptions.Character:
                    await RespondAsync(character, ephemeral: true);
                    break;
                case HelpOptions.Inventory:
                    await RespondAsync(inv, ephemeral: true);
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
