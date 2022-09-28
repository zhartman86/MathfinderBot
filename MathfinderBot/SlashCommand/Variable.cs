using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using Discord;

namespace MathfinderBot
{
    public class Variable : InteractionModuleBase
    {
        public enum AbilityScoreDmg
        {
            [ChoiceDisplay("None")]
            BONUS,
            
            STR,
            DEX,
            CON,
            INT,
            WIS,
            CHA
        }

        public enum AbilityScoreHit
        {          
            STR,
            DEX,
            CON,
            INT,
            WIS,
            CHA
        }

        public enum SizeOption
        {
            None,
            Fine,
            Diminutive,
            Tiny,
            Small,
            Medium,
            Large,
            Huge,
            Gargantuan,
            Colossal
        }

        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,

            [ChoiceDisplay("Set-Row")]
            SetRow,

            [ChoiceDisplay("Set-Grid")]
            SetGrid,

            [ChoiceDisplay("Set-Craft")]
            SetCraft,

            [ChoiceDisplay("List-Vars")]
            ListVars,        

            [ChoiceDisplay("List-Bonus")]
            ListBonus,
  
            [ChoiceDisplay("List-Weapons")]
            ListWeapons,

            [ChoiceDisplay("List-Armor")]
            ListArmor,

            [ChoiceDisplay("List-Shapes")]
            ListShapes,     

            [ChoiceDisplay("List-Mods")]
            ListMods,
            
            [ChoiceDisplay("List-Crafts")]
            ListCrafts,

            [ChoiceDisplay("Remove-Variable")]
            Remove
        }

        static Regex                        ValidVar = new Regex(@"^[0-9A-Z_]{1,17}$");
        static Regex                        validExpr = new Regex(@"^[0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,100}$");
        static Dictionary<ulong, string>    lastInputs = new Dictionary<ulong, string>();
        static Dictionary<ulong, ExprRow>   lastRow = new Dictionary<ulong, ExprRow>();
        public static ExprRow               exprRowData = null;
        ulong                               user;
        CommandHandler                      handler;
        IMongoCollection<StatBlock>         collection;
        
        public static byte[]                weapons = null;
        public static byte[]                armor = null;
        public static byte[]                shapes = null;
        public static byte[]                mods = null;

        public Variable(CommandHandler handler) => this.handler = handler;

        public async override void BeforeExecute(ICommandInfo command)
        {
            user        = Context.Interaction.User.Id;
            collection  = Program.database.GetCollection<StatBlock>("statblocks");   
        }

        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "", string value = "")
        {
            if(action == VarAction.ListWeapons)
            {
                if(weapons == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Weapons.Count; i++)
                        sb.AppendLine($"{i,-4} |{DataMap.Weapons[i].Name,-15}");
                    weapons = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(weapons);
                await RespondWithFileAsync(stream, $"WeaponPresets.txt", ephemeral: true);
                return;
            }

            if(action == VarAction.ListArmor)
            {
                if(armor == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Armor.Count; i++)
                        sb.AppendLine($"{i,-4} |{DataMap.Armor[i].Name,-15}");
                    armor = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(armor);
                await RespondWithFileAsync(stream, $"ArmorPresets.txt", ephemeral: true);
                return;
            }

            if(action == VarAction.ListShapes)
            {
                if(shapes == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Shapes.Count; i++)
                        sb.AppendLine($"{i,-4} |{DataMap.Shapes[i].Name,-25} |{DataMap.Shapes[i].Type, -14}");
                    shapes = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(shapes);
                await RespondWithFileAsync(stream, $"Shapes.txt", ephemeral: true);
                return;
            }

            if(action == VarAction.ListMods)
            {              
                if(mods == null)
                {
                    var sb = new StringBuilder();
                    foreach(var mod in DataMap.Modifiers)
                        sb.AppendLine(mod.Key);
                    mods = Encoding.ASCII.GetBytes(sb.ToString());                   
                }
                using var stream = new MemoryStream(mods);
                await RespondWithFileAsync(stream, "Mods.txt", ephemeral: true);
                return;
            }


            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == VarAction.ListVars)
            {
                var sb = new StringBuilder();

                sb.AppendLine("*STATS*");
                foreach(var stat in Characters.Active[user].Stats)
                    sb.AppendLine($"|{stat.Key,-14} |{stat.Value.Value,-5}");
                sb.AppendLine();
                sb.AppendLine("*EXPRESSIONS*");
                foreach(var expr in Characters.Active[user].Expressions)
                    sb.AppendLine($"|{expr.Key,-15} |{expr.Value.ToString(),-35}");
                sb.AppendLine();
                sb.AppendLine("*ROWS*");
                foreach(var row in Characters.Active[user].ExprRows.Keys)
                    sb.AppendLine($"{row}");
                sb.AppendLine();
                sb.AppendLine("*GRIDS*");
                foreach(var grid in Characters.Active[user].Grids.Keys)
                    sb.AppendLine(grid);

                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
                await RespondWithFileAsync(stream, $"Vars.{Characters.Active[user].CharacterName}.txt", ephemeral: true);
            }
              
            if(action == VarAction.ListBonus)
            {
                var sb = new StringBuilder();
                foreach(var stat in Characters.Active[user].Stats)
                {
                    if(stat.Value.Bonuses.Count > 0 || stat.Value.Override != null)
                    {
                        sb.AppendLine("```");
                        sb.AppendLine(stat.Key);
                        if(stat.Value.Override != null)
                            sb.AppendLine($"  |OVERRIDE: {stat.Value.Override.Name,-9} |{stat.Value.Override.Value,-3}");
                        foreach(var bonus in stat.Value.Bonuses)
                            sb.AppendLine($"  |{bonus.Name,-9} |{bonus.Type,-10} |{bonus.Value, -3}");
                        sb.Append("```");
                    }               
                }

                var eb = new EmbedBuilder()
                    .WithColor(Color.DarkGreen)
                    .WithTitle("List-Bonuses()")
                    .WithDescription(sb.ToString());

                await RespondAsync(embed: eb.Build(), ephemeral: true);
            }         

            var varToUpper = varName.ToUpper().Replace(' ', '_');
            if(!ValidVar.IsMatch(varToUpper))
            {
                await RespondAsync($"Invalid variable `{varToUpper}`. a-Z and underscores/spaces only.", ephemeral: true);
                return;
            }
                        
            if(action == VarAction.Remove)
            {
                if(Characters.Active[user].Stats.ContainsKey(varToUpper))
                {
                    Characters.Active[user].Stats.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Stats, Characters.Active[user].Stats);
                    await Program.UpdateSingleAsync(update, user);
                    await RespondAsync($"`{varToUpper}` removed from stats.", ephemeral: true);
                    return;
                }
                else if(Characters.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    Characters.Active[user].Expressions.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Expressions, Characters.Active[user].Expressions);
                    await Program.UpdateSingleAsync(update, user);
                    await RespondAsync($"`{varToUpper}` removed from expressions.", ephemeral: true);
                    return;
                }
                else if(Characters.Active[user].ExprRows.ContainsKey(varToUpper))
                {
                    Characters.Active[user].ExprRows.Remove(varToUpper);
                    
                    var update = Builders<StatBlock>.Update.Set(x => x.ExprRows, Characters.Active[user].ExprRows);
                    await Program.UpdateSingleAsync(update, user);
                    await RespondAsync($"`{varToUpper}` removed from rows.", ephemeral: true);
                    return;
                }
                else if(Characters.Active[user].Grids.ContainsKey(varToUpper))
                {
                    Characters.Active[user].Grids.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Grids, Characters.Active[user].Grids);
                    await Program.UpdateSingleAsync(update, user);
                    await RespondAsync($"`{varToUpper}` removed from grids.", ephemeral: true);
                    return;
                }

                await RespondAsync($"No variable `{varToUpper}` found.", ephemeral: true);
                return;
            }

            if(action == VarAction.SetExpr)
            {
                if(Characters.Active[user].Stats.ContainsKey(varToUpper))
                {
                    await RespondAsync($"`{varToUpper}` already exists as a stat.", ephemeral: true);
                    return;
                }

                Characters.Active[user].Expressions[varToUpper] = value;

                var update = Builders<StatBlock>.Update.Set(x => x.Expressions[varToUpper], Characters.Active[user].Expressions[varToUpper]);
                await Program.UpdateSingleAsync(update, user);
                await RespondAsync($"Updated expression:`{varToUpper}`", ephemeral: true);
                return;
            }                                            
        
            if(action == VarAction.SetRow)
            {               
                lastInputs[user] = varToUpper;
                await RespondWithModalAsync<ExprRowModal>("set_row");
                return;
            }
            
            if(action == VarAction.SetGrid)
            {          
                lastInputs[user] = varToUpper;                
                await RespondWithModalAsync<GridModal>("set_grid");
                return;
            }
        
            if(action == VarAction.SetCraft)
            {
                await RespondWithModalAsync<CraftingModal>("new_craft");
                return;
            }       
        }

        [SlashCommand("row", "Get a row or rows")]
        public async Task GetRowCommand(string rowOne, string rowTwo = "", string rowThree = "", string rowFour = "", string rowFive = "")
        {
            var rowStrings = new string[5] { rowOne, rowTwo, rowThree, rowFour, rowFive };
            var rows = new List<ActionRowBuilder>();

            for(int i = 0; i < rowStrings.Length; i++)
            {         
                if(rowStrings[i] != "")
                {
                    var toUpper = rowStrings[i].ToUpper().Replace(' ', '_');
                    if(!Characters.Active[user].ExprRows.ContainsKey(toUpper))
                    {
                        await RespondAsync($"`{toUpper}` not found.", ephemeral: true);
                        return;
                    }

                    rows.Add(BuildRow(Characters.Active[user].ExprRows[toUpper]));
                }
            }
            var builder = new ComponentBuilder()
                .WithRows(rows);
            
            await RespondAsync(components: builder.Build(), ephemeral: true);
        }

        [SlashCommand("preset-shape", "Generate attacks based on a creature's shape")]
        public async Task PresetShapeCommand(string numberOrName, AbilityScoreHit hitMod = AbilityScoreHit.STR, bool multiAttack = false)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = numberOrName.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.Shapes.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.Shapes.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.Shapes.Count)
            {
                var shape = DataMap.Shapes[outVal];

                var primary     = new List<(string,string)>();
                var secondary   = new List<(string,string)>();

                if(shape.Bite != "")        primary.Add(("bite",    shape.Bite));
                if(shape.Claws != "")       primary.Add(("claw",    shape.Claws));
                if(shape.Gore != "")        primary.Add(("gore",    shape.Gore));
                if(shape.Slam != "")        primary.Add(("slam",    shape.Slam));
                if(shape.Sting != "")       primary.Add(("sting",   shape.Sting));
                if(shape.Talons != "")      primary.Add(("talon",   shape.Talons));
                

                if(shape.Hoof != "")        secondary.Add(("hoof",      shape.Hoof));
                if(shape.Tentacle != "")    secondary.Add(("tentacle",  shape.Tentacle));
                if(shape.Wing != "")        secondary.Add(("wing",      shape.Wing));
                if(shape.Pincers != "")     secondary.Add(("pincer",    shape.Pincers));
                if(shape.Tail != "")        secondary.Add(("tail",      shape.Tail));
                
                
                if(shape.Other != "")
                {
                    var oSplit = shape.Other.Split('/');
                    for(int i = 0; i < oSplit.Length; i++)
                    {
                        var split = oSplit[i].Split(':');
                        if(split.Length > 2)
                            primary.Add((split[1], split[2]));
                        else if(split.Length > 1)
                            secondary.Add((split[0], split[1]));
                        else
                            secondary.Add(("other", split[0]));
                    }
                }

                var cb = new ComponentBuilder();

                if(primary.Count > 0)
                {
                    var row = new ExprRow();
                    row.Set.Add(new Expr()
                    {
                        Name = "primary",
                        Expression = $"ATK_{Enum.GetName(typeof(AbilityScoreHit), hitMod)}"
                    });
                
                    for(int i = 0; i < primary.Count; i++)
                    {
                        var split = primary[i].Item2.Split('/');
                        for(int j = 0; j < split.Length; j++)
                        {

                            var splitCount = split[j].Split(':');
                            if(splitCount.Length > 1)   row.Set.Add(new Expr() { Name = $"{splitCount[0]} {primary[i].Item1}s ({splitCount[1]})", Expression = splitCount[1] });
                            else                        row.Set.Add(new Expr() { Name = $"{primary[i].Item1} ({splitCount[0]})", Expression = splitCount[0] });
                        }
                    }

                    cb.AddRow(BuildRow(row));
                }
                if(secondary.Count > 0)
                {
                    var row = new ExprRow();
                    var secondaryMod = multiAttack ? "2" : "5";
                    row.Set.Add(new Expr()
                    {
                        Name = "secondary",
                        Expression = $"ATK_{Enum.GetName(typeof(AbilityScoreHit), hitMod)}-{secondaryMod}"
                    });
                
                    for(int i = 0; i < secondary.Count; i++)
                    {
                        var split = secondary[i].Item2.Split('/');
                        for(int j = 0; j < split.Length; j++)
                        {
                            Console.WriteLine(split[j]);
                            var splitCount = split[j].Split(':');                         
                            if(splitCount.Length > 1)   row.Set.Add(new Expr() { Name = $"{splitCount[0]} {secondary[i].Item1}s ({splitCount[1]})", Expression = splitCount[1] });
                            else                        row.Set.Add(new Expr() { Name = $"{secondary[i].Item1} ({splitCount[0]})", Expression = splitCount[0] });
                        }                        
                    }
                    cb.AddRow(BuildRow(row));
                }

                if(shape.Breath != "")
                {
                    var row = new ExprRow();
                    row.Set.Add(new Expr() { Name = $"breath[{shape.Breath}]", Expression = shape.Breath });
                    cb.AddRow(BuildRow(row));
                }

                var sb = new StringBuilder();

                sb.AppendLine($"__**{shape.Name}**__");

                var splits = shape.Speed.Split('/');
                sb.Append("**Speed** ");
                for(int i = 0; i < splits.Length; i++)
                    sb.Append($"{splits[i]}; ");
                sb.AppendLine();
                
                splits = shape.Senses.Split('/');
                sb.Append("**Senses** ");
                for(int i = 0; i < splits.Length; i++)
                    sb.Append($"{splits[i]}; ");
                sb.AppendLine();
                
                if(shape.Specials != "")
                {
                    splits = shape.Specials.Split('/');
                    sb.Append("**Special** ");
                    for(int i = 0; i < splits.Length; i++)
                        sb.Append($"{splits[i]}; ");
                }

                var eb = new EmbedBuilder()
                    .WithDescription($"{sb}");

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
        }

        [SlashCommand("preset-armor", "Apply an armor's stats to an active character")]
        public async Task PresetArmorCommand(string numberOrName, int enhancement = 0)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = numberOrName.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.Armor.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.Armor.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.Armor.Count)
            {
                
                
                var armor = DataMap.Armor[outVal];
                if(armor.Type == "S")
                {
                    Characters.Active[user].ClearBonus("SHIELD");
                    Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus        { Name = "SHIELD", Type = BonusType.Shield, Value = armor.ShieldBonus.Value });
                    Characters.Active[user].Stats["AC_PENALTY"].AddBonus(new Bonus      { Name = "SHIELD", Type = BonusType.Penalty, Value = armor.Penalty.Value });
                    if(enhancement > 0)
                        Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus    { Name = "SHIELD", Type = BonusType.Enhancement, Value = enhancement });
                }
                else
                {
                    Characters.Active[user].ClearBonus("ARMOR");
                    Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus        { Name = "ARMOR", Type = BonusType.Armor, Value = armor.ArmorBonus.Value });
                    Characters.Active[user].Stats["AC_PENALTY"].AddBonus(new Bonus      { Name = "ARMOR", Type = BonusType.Penalty, Value = armor.Penalty.Value });
                    if(armor.MaxDex != null)
                        Characters.Active[user].Stats["AC_MAXDEX"].AddBonus(new Bonus   { Name = "ARMOR", Type = BonusType.Base, Value = armor.MaxDex.Value });
                    if(enhancement > 0)
                        Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus    { Name = "ARMOR", Type = BonusType.Enhancement, Value = enhancement });
                }
                var aBonus = armor.ArmorBonus > 0 ? armor.ArmorBonus : armor.ShieldBonus;
                var maxDex = armor.MaxDex != null ? armor.MaxDex.ToString() : "—";
                var sb = new StringBuilder();
                sb.AppendLine($"__**{armor.Name}**__");
                sb.AppendLine($"**Cost** {armor.Cost}; **Weight** {armor.Weight}");
                sb.AppendLine($"**Armor/Shield Bonus** {aBonus}; **Max Dex** {maxDex}; **Penalty** {armor.Penalty}");
                sb.AppendLine();
                sb.AppendLine($"{armor.Description}");
                Console.WriteLine("Test");
                var eb = new EmbedBuilder()
                    .WithTitle($"Set-Armor()")
                    .WithDescription(sb.ToString());

                var update = Builders<StatBlock>.Update.Set(x => x.Stats, Characters.Active[user].Stats);
                await Program.UpdateSingleAsync(update, user);

                await RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }
            await RespondAsync($"{toUpper} not found", ephemeral: true);
        }

        [SlashCommand("preset-spell", "Get spell info")]
        public async Task PresetSpellCommand(string numberOrName)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = numberOrName.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.Spells.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.Spells.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.Spells.Count)
            {
                var sb = new StringBuilder();
                var spell = DataMap.Spells[outVal];

                sb.AppendLine($"__**{spell.Name}**__");
                sb.AppendLine($"**School** {spell.School} {spell.Subschool} {spell.Descriptor}");
                sb.AppendLine($"**Level** {spell.Levels}");
                sb.AppendLine($"**Casting Time** {spell.CastingTime}");
                sb.AppendLine($"**Components** {spell.Components}");
                sb.AppendLine($"**Range** {spell.Range}");
                sb.AppendLine($"**Target** {spell.Targets}");
                sb.AppendLine($"**Duration** {spell.Duration}");
                sb.AppendLine($"**Saving Throw** {spell.SavingThrow}; **Spell Resistance** {spell.SpellResistance}");
                sb.AppendLine();
                sb.AppendLine(spell.Description);

                var eb = new EmbedBuilder()
                    .WithColor(Color.Purple)
                    .WithDescription(sb.ToString());

                await RespondAsync(embed: eb.Build());
            }


        }
        
        [SlashCommand("preset-weapon", "Generate a preset row with modifiers")]
        public async Task PresetWeaponCommand(string numberOrName, AbilityScoreHit hitMod = AbilityScoreHit.STR, AbilityScoreDmg damageMod = AbilityScoreDmg.BONUS, string hitBonus = "", string dmgBonus = "", SizeOption size = SizeOption.None)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = numberOrName.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.Weapons.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null) 
                outVal = DataMap.Weapons.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }
               
          
            if(outVal >= 0 && outVal < DataMap.Weapons.Count)
            {
                var attack = DataMap.Weapons[outVal];
                string[] split = attack.Medium.Split('/'); 
                if(size != SizeOption.None)
                {
                    switch(size)
                    {
                        case SizeOption.Fine:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Diminutive:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Tiny:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Small:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Medium:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Large:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Huge:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Gargantuan:
                            split = attack.Fine.Split('/');
                            break;
                        case SizeOption.Colossal:
                            split = attack.Fine.Split('/');
                            break;                     
                    }
                }                
                else if(Characters.Active[user].Stats.ContainsKey("SIZE_MOD"))
                {
                    switch(Characters.Active[user]["SIZE_MOD"])
                    {
                        case (int)SizeType.Fine:
                            split = attack.Fine.Split('/');
                            break;
                        case (int)SizeType.Diminutive:
                            split = attack.Diminutive.Split('/');
                            break;
                        case (int)SizeType.Tiny:
                            split = attack.Tiny.Split('/');
                            break;
                        case (int)SizeType.Small:
                            split = attack.Small.Split('/');
                            break;
                        case (int)SizeType.Medium:
                            split = attack.Medium.Split('/');
                            break;
                        case (int)SizeType.Large:
                            split = attack.Large.Split('/');
                            break;
                        case (int)SizeType.Huge:
                            split = attack.Huge.Split('/');
                            break;
                        case (int)SizeType.Gargantuan:
                            split = attack.Gargantuan.Split('/');
                            break;
                        case (int)SizeType.Colossal:
                            split = attack.Colossal.Split('/');
                            break;
                        default:
                            split = attack.Medium.Split('/');
                            break;
                    }
                }

                var bonus = hitBonus != "" ? $"+{hitBonus}" : "";
                var row = new ExprRow()
                {
                    RowName = attack.Name,
                    Set = new List<Expr>()
                    {
                        new Expr()
                        {
                            Name = $"HIT [{Enum.GetName(typeof(AbilityScoreHit), hitMod)}]",
                            Expression = $"ATK_{Enum.GetName(typeof(AbilityScoreHit), hitMod)}{bonus}",
                        }
                    }
                };
                
                bonus = dmgBonus != "" ? $"+{dmgBonus}" : "";

                if(split.Length == 1)
                {
                    row.Set.Add(new Expr()
                    {
                        Name = $"DMG [{split[0]}]",
                        Expression = $"{split[0]}+DMG_{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}{bonus}",
                    });
                }
                else if(split.Length == 2)
                {
                    row.Set.Add(new Expr()
                    {
                        Name = $"DMG [{split[0]}]",
                        Expression = $"{split[0]}+DMG_{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}{bonus}",
                    });
                    row.Set.Add(new Expr()
                    {
                        Name = $"DMG [{split[1]}]",
                        Expression = $"{split[1]}+DMG_{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}",
                    });
                }

                lastRow[user] = row;
                var ar = BuildRow(row);
                               
                var cb = new ComponentBuilder()
                    .AddRow(ar);
                
                var sb = new StringBuilder();

                sb.AppendLine($"__**{attack.Name.ToUpper()}**__");
                sb.Append("**Damage** ");
                for(int i = 0; i < split.Length; i++)
                {
                    if(i > 0) sb.Append("/");
                    sb.Append(split[i]);
                }
                if(attack.Range > 0)
                    sb.AppendLine($" **Range** {attack.Range} **Type** {attack.DmgType}");
                else
                    sb.AppendLine($" **Type** {attack.DmgType}");
                sb.AppendLine($" **Critical** {attack.CritRng}/x{attack.CritMul} ");
                if(attack.Special != "")        sb.AppendLine($"**Special** {attack.Special}");

                sb.AppendLine();
                if(attack.Description != "")    sb.AppendLine(attack.Description);

                var eb = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithTitle($"Weapon-Preset()")
                    .WithDescription($"{sb}");

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
        }

        [SlashCommand("preset-save", "Save the last called preset-weapon with a custom name")]
        public async Task SaveWeaponCommand(string name)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }
            if(!lastRow.ContainsKey(user) || lastRow[user] == null)
            {
                await RespondAsync("No row found");
                return;
            }
            
            var row = lastRow[user];
            Characters.Active[user].ExprRows[name] = row;

            var update = Builders<StatBlock>.Update.Set(x => x.ExprRows[row.RowName], row);
            await Program.UpdateSingleAsync(update, user);
            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle($"Save-Row({name})")
                .WithDescription($"You can call this row by calling `/row` and using `{name}` as the first parameter");

            for(int i = 0; i < row.Set.Count; i++)
                if(!string.IsNullOrEmpty(row.Set[i].Name))
                    eb.AddField(name: row.Set[i].Name, value: row.Set[i].Expression, inline: true);

            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("grid", "Call a saved set of rows")]
        public async Task GridGetCommand(string gridName)
        {
            var toUpper = gridName.ToUpper().Replace(' ', '_');
            if(!Characters.Active[user].Grids.ContainsKey(toUpper))
            {
                await RespondAsync($"{toUpper} not found.", ephemeral: true);
                return;
            }

            var grid = Characters.Active[user].Grids[toUpper];
            var rows = new List<ActionRowBuilder>();
            
            for(int i = 0; i < grid.Length; i++)
            {
                if(!Characters.Active[user].ExprRows.ContainsKey(grid[i]))
                {
                    await RespondAsync($"{grid[i]} not found", ephemeral: true);
                    return;
                } 
                rows.Add(BuildRow(Characters.Active[user].ExprRows[grid[i]]));

            }
            var builder = new ComponentBuilder()
                .WithRows(rows);
            
            await RespondAsync(components: builder.Build(), ephemeral: true);
        }

        [ComponentInteraction("row:*,*,*")]
        public async Task ButtonPressed(ulong user, string expr, string name)
        {

            var sb = new StringBuilder();
            var result = Parser.Parse(expr).Eval(Characters.Active[user], sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription($"{Characters.Active[user].CharacterName}")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"__Events__", $"{sb}");

            await RespondAsync(embed: builder.Build());
        }

        [ModalInteraction("new_craft")]
        public async Task NewCraftModal(CraftingModal modal)
        {
            var craft = new CraftItem()
            {
                Item = modal.ItemName,
                Difficulty = modal.Difficulty,
                Price = modal.SilverPrice
            };

            Characters.Active[user].Crafts[craft.Item] = craft;
            await RespondAsync($"{craft.Item} set for crafting. Use `/craft` to begin rolling.");
        }

        [ModalInteraction("set_row")]
        public async Task NewRow(ExprRowModal modal)
        {
            using var reader = new StringReader(modal.Expressions);
            var exprs = new string[5] { "", "", "", "", "" };
            for(int i = 0; i < 5; i++)
            {
                var line = reader.ReadLine();
                if(line == null)
                    break;
                exprs[i] = line;
            }
                        
            string[] rowExprs = new string[5];
            string[] rowExprNames = new string[5];

            for(int i = 0; i < exprs.Length; i++)
            {
                if(validExpr.IsMatch(exprs[i]))
                {
                    var split = exprs[i].Split(':');
                    if(split.Length == 2)
                    {
                        rowExprNames[i] = split[0];
                        rowExprs[i] = split[1];
                    }
                    else if(split.Length == 1)
                    {
                        rowExprNames[i] = split[0];
                        rowExprs[i] = split[0];
                    }                
                    else
                    {
                        await RespondAsync($"Invalid Input @ Expression {i + 1}", ephemeral: true);
                        return;
                    }                      
                }
                else
                {
                    rowExprNames[i] = "";
                    rowExprs[i]     = "";
                }
            }
            var row = new ExprRow()
            {
                RowName = $"{lastInputs[user]}",
                Set = new List<Expr>()
            };

            for(int i = 0; i < rowExprNames.Length; i++)
                if(!string.IsNullOrEmpty(rowExprNames[i]))
                    row.Set.Add(new Expr(rowExprNames[i], rowExprs[i]));

            Characters.Active[user].ExprRows[row.RowName] = row;           
            var update = Builders<StatBlock>.Update.Set(x => x.ExprRows[row.RowName], row);
            await Program.UpdateSingleAsync(update, user);
            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle($"New-Row({row.RowName})");

            for(int i = 0; i < row.Set.Count; i++)
                if(!string.IsNullOrEmpty(row.Set[i].Name))
                    eb.AddField(name: row.Set[i].Name, value: row.Set[i].Expression, inline: true);
 
            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        [ModalInteraction("set_grid")]
        public async Task SetGridModal(GridModal modal)
        {
            using var reader = new StringReader(modal.Rows);
            var strings = new List<string>();
            for(int i = 0; i < 5; i++)
            {
                var line = reader.ReadLine();
                if(line == null)
                    break;
                strings.Add(line);
            }

            for(int i = 0; i < strings.Count; i++)
                if(strings[i] == "" || !Characters.Active[user].ExprRows.ContainsKey(strings[i]))
                    strings.Remove(strings[i]);
    
            if(strings.Count == 0)
            {
                await RespondAsync("No valid rows found");
                return;
            }

            string name = $"{lastInputs[user]}";

            var exprs = new string[strings.Count];

            for(int i = 0; i < strings.Count; i++)
                exprs[i] = strings[i];

            Characters.Active[user].Grids[name] = exprs;

            var update = Builders<StatBlock>.Update.Set(x => x.Grids[name], exprs);
            await Program.UpdateSingleAsync(update, user);
            await RespondAsync($"Created {name}!", ephemeral: true);
        }


        ActionRowBuilder BuildRow(ExprRow exprRow, string label = "", Emote labelEmote = null)
        {
            var ar = new ActionRowBuilder();

            if(label != "")
                ar.WithButton(label, "weap_name", style: ButtonStyle.Secondary, disabled: true, emote: labelEmote);

            for(int i = 0; i < exprRow.Set.Count; i++)
            {
                if(!string.IsNullOrEmpty(exprRow.Set[i].Expression))
                    ar.WithButton(customId: $"row:{user},{exprRow.Set[i].Expression.Replace(" ", "")},{exprRow.Set[i].Name.Replace(" ", "")}", label: exprRow.Set[i].Name, disabled: (exprRow.Set[i].Expression == "") ? true : false);
            }          
            return ar;
        }
    }
}
