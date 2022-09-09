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
Using the export option `csheet_fantasy_rpgwebprofiler.xml`. Tested with v6.09.05.

### HeroLabs
Using the XML export option

### Pathbuilder
Using the exported PDF


These files can be uploaded to update any created character.


## Commands

### **CHAR**

usage:
/char `mode` `char-name` `game`

-:-

`mode` options:

—`Set`: When this option is used, the char-name field is required. It will activate any character created by the same name.

—`New`: When New is used, the char-name field is required. This will create a new character of the same name.

—`List`: This will list any created characters you have.

—`Export`: (Experimental) This will export your character into JSON format.

—`Delete`: Any character name listed in char-name will be deleted. It will prompt you to confirm this deletion.

--

`char-name`

The name of the character.

--

`game`

If left blank, it will default to Pathfinder. Any other options are purely experimental, and not well supported at the current time.


### **CHAR-UPDATE**

usage:
/char-update `sheet-type` `file`

-:-

`sheet-type` options:

—`Pathbuilder`: Pathbuilder PDF export.

—`HeroLabs`: HeroLabs XML export.

—`PCGen`: Export using the `csheet_fantasy_rpgwebprofiler.xml` option.

--

`file`

The file to use.


### **EVAL**

usage:
/eval `expr`

-:-


`expr`

The expression to evaluate. This can include `Stats`, `Expressions`, and many different math operators: `+` `-` `*` `/` `>` `<` `==` `!=` `<=` `>=` `%` `()` `=` `+=` `-=` `*=` `/=` `&&` `||` `?:`. There is also a special operator `$` which can coerce a specific bonus from a Stat. Its `+$::` and `-$` usage can add and remove bonuses as well.


### **VAR**

usage:
/var `action` `var-name` `value`

-:-

`action` options:

—`Set-Expression`: `var-name` and `value` are the created name and expression respectively. 

—`Set-Row`: `var-name` is the Row name. This will bring up a modal window, where you can make up to 5 expressions. The syntax is: `LABEL:EXPR`.

—`Set-Grid`: `var-name` is the Grid name. The same as Set-Row except you can specify a set of rows.

—`Set-Craft`: (EXPERIMENTAL) This lets you set a craft a mundane item with `var-name` as the name and `value` as the DC to craft.

—`List-Stats`: Lists all stats for an active character.

—`List-Expressions`: Lists all expressions for an active character.

—`List-Bonus`: Lists all bonuses applied to stats.

—`List-Row`: Lists all saved Rows. Optionally, you can use `var-name` to list a specific Row's expressions.

—`List-RowPresets`: Lists all presets

—`List-Grids`: Lists all saved Grids.

—`List-Crafts`: List all active crafts.

—`Remove-Variable`: Removes a Stat,Expression,Row, or Grid with the name `var-name`.

--

`var-name`

The name of the variable.

--

`value`

Used in a few options to represent an expression or other value.



