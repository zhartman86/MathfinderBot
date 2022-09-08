# MathfinderBot

Mathfinder is a Discord bot built as a stat-tracker for Pathfinder 1e. Many of its features are built around a math expression engine with a linked statblock. This allows you to create helpful formulas relating to your character. There are many more features included, as well as the ability to creates rows and grids of buttons that represent saved expressions.

There are many help options built into the bot using the `/mf-help` command. I will do my best to update both the documents located here and on the bot!

## W.I.P.
This bot is a work in progress! Please be patient while I fix any ongoing issues with character imports or other basic engine functionality. 


## Stats & Expressions
Statblocks contain two primary values: `Stats` and `Expressions`. 

Each Stat contains a base value, as well as a list of bonuses. A `Bonus` contains a value, a name, and a bonus-type. Together, these are used to accurately calculate the total of any Stat.

Expressions are formulae. These can represent anything from a contant number to an expression of expressions including any number of stats. 

While different in their application, Stats and Expressions share variable names.


## Rows & Grids
`Rows` are sets of buttons you can call at anytime to run saved expressions. These can be created from scratch or saved from presets.

`Grids` are sets of Rows. Up to 5 rows can be called in this manner per command, creating an (up to) 5x5 grid of buttons.


## Character Sheet Imports
While you can setup a character from scratch (manually setting each value), this is not ideal. Mathfinder currently supports three different options for character imports, so that you can update your character at each level.

### PCGen
Using the export option `csheet_fantasy_rpgwebprofiler.xml`. Tested on v6.09.05.

### HeroLabs
Using the XML export option

### Pathbuilder
Using the exported PDF


These files can be uploaded to update any created character.


## Commands

**CHAR**

usage:
/char `mode` `char-name` `game`
-:-

`mode`
`Set` When this option is used, the char-name field is required. It will activate any character created by the same name.

`New` When New is used, the char-name field is required. This will create a new character of the same name.

`List` This will list any created characters you have.

`Export` (Experimental) This will export your character into JSON format.

`Delete` Any character name listed in char-name will be deleted. It will prompt you to confirm this deletion.

**EVAL**

usage:
/eval `expr`
-:-


`expr`
The expression to evaluate. This can include `Stats`, `Expressions`, and many different math operators: `+` `-` `*` `/` `>` `<` `==` `!=` `<=` `>=` `%` `()` `=` `+=` `-=` `*=` `/=` `&&` `||` `?:`. There is also a special operator `$` which can coerce a specific bonus from a Stat. Its `+$` and `-$` usage can add and remove bonuses as well.

