# MathfinderBot

Mathfinder is a Discord bot built as a stat-tracker for Pathfinder 1e. Many of its features are built around a math expression engine with a linked statblock. This allows you to create helpful formulas relating to your character. 

## Stats & Expressions
Statblocks contain two primary values: `Stats` and `Expressions`. 

Each Stat contains a base value, as well as a list of bonuses. A `Bonus` contains a value, a name, and a bonus-type. Together, these are used to accurately calculate the total of any Stat.

Expressions are formulae. These can represent anything from a contant number to an expression of expressions including any number of stats. 

While different in their application, Stats and Expressions share variable names.
