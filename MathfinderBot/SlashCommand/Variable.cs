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

            [ChoiceDisplay("List-Stat")]
            ListStats,

            [ChoiceDisplay("List-Expression")]
            ListExpr,

            [ChoiceDisplay("List-Bonus")]
            ListBonus,

            [ChoiceDisplay("List-Row")]
            ListRow,

            [ChoiceDisplay("List-Presets")]
            ListRowPresets,

            [ChoiceDisplay("List-Grid")]
            ListGrid,

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
        public static byte[]                rowPresets = null;

        public Variable(CommandHandler handler) => this.handler = handler;

        public async override void BeforeExecute(ICommandInfo command)
        {
            user        = Context.Interaction.User.Id;
            collection  = Program.database.GetCollection<StatBlock>("statblocks");   
        }

        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "", string value = "")
        {
            if(action == VarAction.ListRowPresets)
            {
                if(rowPresets == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Attacks.Count; i++)
                        sb.AppendLine($"{i,-4} |{DataMap.Attacks[i].Name,-15}");
                    rowPresets = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(rowPresets);
                await RespondWithFileAsync(stream, $"WeaponPresets.txt", ephemeral: true);
                return;
            }

            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == VarAction.ListStats)
            {
                var builder = new StringBuilder();

                foreach(var stat in Characters.Active[user].Stats)
                    builder.AppendLine($"|{stat.Key, -14} |{stat.Value.Value,-5}");
                
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
                await RespondWithFileAsync(stream, $"Stats.{Characters.Active[user].CharacterName}.txt", ephemeral: true);
                return;
            }

            if(action == VarAction.ListExpr)
            {
                var sb = new StringBuilder();

                foreach(var expr in Characters.Active[user].Expressions)
                    sb.AppendLine($"|{expr.Key, -15} |{expr.Value.ToString(),-35}");                       

                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
                await RespondWithFileAsync(stream, $"Expr.{Characters.Active[user].CharacterName}.txt", ephemeral: true);
                return;
            }
          
            if(action == VarAction.ListRow)
            {
                var eb = new EmbedBuilder();
                if(varName != "")
                {
                    var toUpper = varName.ToUpper();
                    if(Characters.Active[user].ExprRows.ContainsKey(toUpper))
                    {
                        eb = new EmbedBuilder()
                            .WithColor(Color.DarkGreen)
                            .WithTitle($"List-Row({toUpper})")
                            .WithDescription(Characters.Active[user].ExprRows[toUpper].ToString());
                        
                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }

                var sb = new StringBuilder();

                sb.AppendLine("```");
                sb.AppendLine("ROWS");
                foreach(var row in Characters.Active[user].ExprRows.Keys)
                    sb.AppendLine($" {row}");         
                sb.AppendLine("```");

                eb = new EmbedBuilder()
                        .WithColor(Color.DarkGreen)
                        .WithTitle($"List-Rows()")
                        .WithDescription(sb.ToString());

                await RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }
                       
            if(action == VarAction.ListGrid)
            {
                var sb = new StringBuilder();
                var eb = new EmbedBuilder();
                if(varName != "")
                {
                    var toUpper = varName.ToUpper();
                    if(Characters.Active[user].Grids.ContainsKey(toUpper))
                    {
                        sb.AppendLine("```");
                        foreach(var row in Characters.Active[user].Grids[toUpper])
                            sb.AppendLine(row);
                        sb.AppendLine("```");

                        eb = new EmbedBuilder()
                            .WithColor(Color.DarkGreen)
                            .WithTitle($"List-Grid({toUpper})")
                            .WithDescription(sb.ToString());
                    }

                    await RespondAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }
                              
                sb.AppendLine("```");
                foreach(var grid in Characters.Active[user].Grids.Keys)
                    sb.AppendLine(grid);
                sb.AppendLine("```");

                eb = new EmbedBuilder()
                            .WithColor(Color.DarkGreen)
                            .WithTitle($"List-Grids()")
                            .WithDescription(sb.ToString());

                await RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }
            
            if(action == VarAction.ListBonus)
            {
                var sb = new StringBuilder();
                foreach(var stat in Characters.Active[user].Stats)
                {
                    if(stat.Value.Bonuses.Count > 0)
                    {
                        sb.AppendLine("```");
                        sb.AppendLine(stat.Key);
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

            var varToUpper = varName.ToUpper();
            if(!ValidVar.IsMatch(varToUpper))
            {
                await RespondAsync($"Invalid variable `{varToUpper}`. A-Z and underscores only. Values will be automatically capitalized.", ephemeral: true);
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
                    var toUpper = rowStrings[i].ToUpper();
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

        
        [SlashCommand("attack-preset", "Generate a preset row with selected modifiers for attack and damage")]
        public async Task RowPresetCommand(string numberOrName, AbilityScoreHit hitMod, AbilityScoreDmg damageMod = AbilityScoreDmg.BONUS, int hitBonus = 0, string dmgBonus = "", SizeOption size = SizeOption.None)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = numberOrName.ToUpper();
            var outVal = -1;
            var nameVal = DataMap.Attacks.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null) 
                outVal = DataMap.Attacks.IndexOf(nameVal);
            else
                int.TryParse(toUpper, out outVal);

            var attack = DataMap.Attacks[outVal];

            if(outVal >= 0 && outVal < DataMap.Attacks.Count)
            {
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
                
                var row = new ExprRow()
                {
                    RowName = attack.Name,
                    Set = new List<Expr>()
                    {
                        new Expr()
                        {
                            Name = $"HIT [{Enum.GetName(typeof(AbilityScoreHit), hitMod)}]",
                            Expression = $"ATK_{Enum.GetName(typeof(AbilityScoreHit), hitMod)}+{hitBonus}",
                        }
                    }
                };

                if(split.Length == 1)
                {
                    row.Set.Add(new Expr()
                    {
                        Name = $"DMG [{split[0]}+{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}]",
                        Expression = $"{split[0]}+DMG_{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}+{dmgBonus}",
                    });
                }
                else if(split.Length == 2)
                {
                    row.Set.Add(new Expr()
                    {
                        Name = $"DMG [{split[0]}+{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}]",
                        Expression = $"{split[0]}+DMG_{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}+{dmgBonus}",
                    });
                    row.Set.Add(new Expr()
                    {
                        Name = $"DMG [{split[1]}+{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}]",
                        Expression = $"{split[1]}+DMG_{Enum.GetName(typeof(AbilityScoreDmg), damageMod)}",
                    });
                }

                lastRow[user] = row;

                Emote outEmote;                         
                Emote.TryParse("<:magicsword:1017221991025082400>", out outEmote);
                var ar = BuildRow(row, $"{attack.Name}", outEmote);
                               
                var cb = new ComponentBuilder()
                    .AddRow(ar);
                
                var sb = new StringBuilder();

                sb.AppendLine($"{attack.Name.ToUpper()}");
                for(int i = 0; i < split.Length; i++)
                {
                    if(i > 0) sb.Append("/");
                    sb.Append(split[i]);
                }
                   
                sb.AppendLine($"({attack.DmgType})");
                sb.AppendLine($"Crit:{attack.CritRng}(x{attack.CritMul})");
                if(attack.Range != 0) sb.AppendLine($"Range:{attack.Range}");
                if(attack.Special != "") sb.AppendLine($"Special:{attack.Special}");

                var eb = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithTitle($"Weapon-Preset({attack.Name})")
                    .WithDescription($"```{sb}```");

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
        }

        [SlashCommand("attack-save", "Save the last called attack-preset")]
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
            var toUpper = gridName.ToUpper();
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
        public async Task ButtonPressed(ulong user, string expr, int id)
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
                    ar.WithButton(customId: $"row:{user},{exprRow.Set[i].Expression.Replace(" ", "")},{i * i}", label: exprRow.Set[i].Name, disabled: (exprRow.Set[i].Expression == "") ? true : false);
            }          
            return ar;
        }
    }
}
