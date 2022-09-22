# MathfinderBot

Mathfinder is a Discord bot built as a stat-tracker for Pathfinder 1e. Many of its features are built around a math expression engine with a linked statblock. This allows you to create helpful formulas relating to your character. There are many features included, as well as the ability to create rows and grids of buttons that represent saved expressions.

There are many help options built into the bot using the `/mf-help` command. These include examples on how to use particular commands. I will do my best to update both the documents located here and on the bot.

## W.I.P.
This bot is a work in progress! Please be patient while I fix any ongoing issues with character imports or other basic engine functionality. 


## Stats & Expressions
Statblocks contain two primary values: `Stats` and `Expressions`. 

Each Stat contains a base value, as well as a list of bonuses. A `Bonus` contains a value, a name, and a bonus-type. Together, these are used to accurately calculate the total of any Stat.

Expressions are formulae. These can represent anything from a contant number to an expression of expressions including any number of stats. 

While different in their application, Stats and Expressions share a pool of variable names. 


## Rows & Grids
`Rows` are sets of buttons that represent expressions. These can be created from scratch or saved from presets. Use `/row` to call.

`Grids` are sets of Rows. Up to 5 rows can be called in this manner per command, creating an (up to) 5x5 grid of buttons. Use `/grid` to call.


## Character Sheet Imports
While you can setup a character from scratch (manually setting each value), this is not ideal. Mathfinder currently supports three different options for character imports, so that you can update your character at each level.

### PCGen
 - Using the export option `csheet_fantasy_rpgwebprofiler.xml`. Tested with v6.09.05.

### HeroLabs
 - Using the XML export option

### Pathbuilder
 - Using the exported PDF


These files can be uploaded to update any created character. There are known limitation when parsing specifical sheets. For instance, not all bonus-types may be known for a given stat, and cannot be applied accurately, but the totals should remain correct. This can affect the proper calculation of stacking bonuses. I do my best!


## Commands
There are several other commands than the ones listed below. However, I've only documented the ones I feel are worth using at the moment. There are also `dm` versions of some commands, which add an additional `target` option for selecting other player's active sheets. These require the `DM` role.

### **-CHARACTER-**

usage: /char `mode` `char-name` `game`

-:-

`mode` *required*

 - —`Set` Set an active character.
   
   - `char-name` The name of the character to set.

 - —`New` Create a new character.
   
   - `char-name` The name of the character to create.
   - `game` *optional & not recommended.* The selected game to use. The default is Pathfinder. Any other option is purely experimental.

 - —`List` This will list any created characters you have.

 - —`Export` (EXPERIMENTAL) This will export any active character into JSON format.

 - —`Delete` Any character name listed in char-name will be deleted. It will prompt you to confirm this deletion.
   - `char-name` The name of the character to delete. This will pop-up a window to confirm your selection.


### **-CHARACTER-UPDATE-**

usage: /char-update `sheet-type` `file`

-:-

`sheet-type` *required*

 - —`Pathbuilder` Pathbuilder PDF export.

 - —`HeroLabs` HeroLabs XML export.

 - —`PCGen` Export using the `csheet_fantasy_rpgwebprofiler.xml` option.


`file` *required*

 - The file to use.


### **-EVALUATE-**

usage: /eval `expr`

-:-

`expr` *required*

 - The expression to evaluate.

#### Remarks
`/eval` can include `Stats`, `Expressions`, and many different math operators: `+` `-` `*` `/` `>` `<` `==` `!=` `<=` `>=` `%` `()` `=` `+=` `-=` `*=` `/=` `&&` `||` `?:`. There is also a special operator `$` which can coerce a specific bonus from a Stat. Its `+$::` and `-$` usage can add and remove bonuses as well. Use `/mf-help` on the bot for specific examples.

Eval specifically returns integer (whole number) values only. True and false are represented by 1 and 0 respectively. You can use `TRUE` or `FALSE` in any expression
for readability.

### **-VAR-**

usage: /var `action` `var-name` `value`

-:-

`action` *required*

 - —`Set-Expression` 
   - `var-name` Name of the expression.
   - `value` Expression to create. 

 - —`Set-Row` 
   - `var-name` Row name. This will bring up a modal window, where you can make up to 5 expressions.

 - —`Set-Grid`
   - `var-name` Grid name. The same as Set-Row except you can specify a set of rows.

 - —`Set-Craft` (EXPERIMENTAL) This lets you set a craft a mundane item. 
   - `var-name` Item name. 
   - `value` DC to craft.

 - —`List-Stats` Lists all stats for an active character.

 - —`List-Expressions` Lists all expressions for an active character.

 - —`List-Bonus` Lists all bonuses applied to stats.

 - —`List-Row` Lists all saved Rows. 
   - `var-name` *optional.* List a single Row's expressions.

 - —`List-Presets` Lists all attack presets.

 - —`List-Grids` Lists all saved Grids.

 - —`List-Crafts` List all active crafts.

 - —`Remove-Variable` Removes a Stat, Expression, Row, or Grid. 
   - `var-name` The variable name to remove.


### **-WEAPON-PRESET-**

usage: /preset-weapon `number-or-name` `hit-mod` `damage-mod` `hit-bonus` `dmg-bonus` `size`

-:-

`number-or-name` *required*
 - The name or index number associated with it (use /var List-Presets to see a comprehensive list).

`hit-mod` *required*
 - The modifier used for hitting.

`damage-mod`
 - The modifier used for damage (if any)

`hit-bonus`
 - The bonus to hit (if any)

`dmg-bonus`
 - The bonus damage (if any)

`size`
 - The size of the character. If left blank, it will check the character's Stackblock for size. If none is found, it will default to medium.

#### Remarks
This will generate an expression row (buttons) based on a selected preset (Use `/var` `List-Presets` to see your options). You can use `/preset-save` with a `name` to save the last generated preset to your active character sheet.

### **-SHAPE-**

usage: /shape `number-or-name` `hit-mod`

-:-

`numer-or-name` *required*
 - The name or index number associated with it (use /var List-Shapes to see a comprehensive list).
 
`hit-mod` *required*
 - The modifier used for hitting.
 
#### Remarks
This will generate the attacks (primary and/or secondary) and natural weapons associated with a particular creature's shape. In addition, it will list any speeds, senses, or special abilities you may receive from taking the creature's shape.


### **-INVENTORY-**

usage: /inv `action` `name`

-:-

`action` *required*

- —`Add` Add one or many items to your active character's inventory. Leave all other fields blank to bring up a window where you can input a list of items.
  - `item` *optional* The syntax for adding an item is `NAME:WEIGHT:VALUE`. For example `Sword:5:10` would add a Sword of 5 weight and a value of 10. Only name is required.
  - `qty` *optional.* How many to add. Default is 1.

- —`Import` Import an a list of items. CAUTION—This will **replace** any existing list. If you want to add many items, use the `Add` action. You can copy/paste your text file into the subsequent modal.
  - `attachment` A text file containing one item per line.

- —`Export` Export the current list to a text file.

- —`Remove` Remove an item from your list.
  - `item` The name or index number of the item. If a name is given, it will remove the first occurence any matched value.
  - `qty` The number of the specified items to remove.

- —`List` List your current inventory.
