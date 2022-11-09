using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using Discord;
using System.Runtime.CompilerServices;

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

        public enum ModAction
        {
            List,
            Add,
            Remove
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

        public enum EquipAction
        {
            Add,
            List
        }

        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,

            [ChoiceDisplay("Set-Row")]
            SetRow,

            [ChoiceDisplay("Set-Grid")]
            SetGrid,
            
            [ChoiceDisplay("List-Vars")]
            ListVars,        

            [ChoiceDisplay("List-Bonuses")]
            ListBonus,            

            [ChoiceDisplay("List-Items")]
            ListItems,
            
            [ChoiceDisplay("Remove-Variable")]
            Remove
        }
        
        static Dictionary<string, int> sizes = new Dictionary<string, int>(){
            { "Fine",        0 },
            { "Diminutive",  1 },
            { "Tiny",        2 },
            { "Small",       3 },
            { "Medium",      4 },
            { "Large",       5 },
            { "Huge",        6 },
            { "Gargantuan",  7 },
            { "Colossal",    8 }};

        static Regex validVar       = new Regex(@"^[0-9A-Z_]{1,30}$");
        static Regex validExpr      = new Regex(@"^[-0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,400}$");
        static Regex targetReplace  = new Regex(@"\D+");
        
        CommandHandler                          handler;
        static Dictionary<ulong, List<IUser>>   lastTargets = new Dictionary<ulong, List<IUser>>();               
        public static ExprRow                   exprRowData = null;
        ulong                                   user;       
        IMongoCollection<StatBlock>             collection;
                    
        static byte[] bestiary  = null!;
        static byte[] items     = null!;
        static byte[] mods      = null!;
        static byte[] shapes    = null!;       
        static byte[] spells    = null!;

        public Variable(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            collection  = Program.database.GetCollection<StatBlock>("statblocks");
        }
        
        //Variable
        async Task VarList()
        {
            var sb = new StringBuilder();

            sb.AppendLine("__STATS__");
            foreach(var stat in Characters.Active[user].Stats)
                sb.AppendLine($"|{stat.Key,-15} |{stat.Value,-35}");
            sb.AppendLine();
            sb.AppendLine("__EXPRESSIONS__");
            foreach(var expr in Characters.Active[user].Expressions)
                sb.AppendLine($"|{expr.Key,-15} |{expr.Value.ToString(),-50}");
            sb.AppendLine();
            sb.AppendLine("__ROWS__");
            foreach(var row in Characters.Active[user].ExprRows.Keys)
                sb.AppendLine($"{row}");
            sb.AppendLine();
            sb.AppendLine("__GRIDS__");
            foreach(var grid in Characters.Active[user].Grids.Keys)
                sb.AppendLine(grid);

            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
            await RespondWithFileAsync(stream, $"Vars.{Characters.Active[user].CharacterName}.txt", ephemeral: true);
        }

        async Task VarListBonuses()
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
                        sb.AppendLine($"  |{bonus.Name,-9} |{bonus.Type,-10} |{bonus.Value,-3}");
                    sb.Append("```");
                }
            }

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkGreen)
                .WithTitle("List-Bonuses()")
                .WithDescription(sb.ToString());

            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        async Task VarSetExpr(string varName)
        {
            if(Characters.Active[user].Stats.ContainsKey(varName) || Characters.Active[user].Expressions.ContainsKey(varName) || Characters.Active[user].Grids.ContainsKey(varName))
            {
                await RespondAsync($"`{varName}` already exists as another variable.", ephemeral: true);
                return;
            }

            var mValue = "";
            if(Characters.Active[user].Expressions.ContainsKey(varName))
                mValue = Characters.Active[user].Expressions[varName];

            var mb = new ModalBuilder("Set-Expression()", $"set_expr:{varName}")
                .AddTextInput(new TextInputBuilder($"{varName}", "expr", value: mValue));
            await RespondWithModalAsync(mb.Build());
            return;
        }

        async Task VarRemove(string varName)
        {
            if(Characters.Active[user].Stats.ContainsKey(varName))
            {
                Characters.Active[user].RemoveStat(varName);      
                await RespondAsync($"`{varName}` removed from stats.", ephemeral: true);
                return;
            }
            else if(Characters.Active[user].Expressions.ContainsKey(varName))
            {
                Characters.Active[user].RemoveExpr(varName);           
                await RespondAsync($"`{varName}` removed from expressions.", ephemeral: true);
                return;
            }
            else if(Characters.Active[user].ExprRows.ContainsKey(varName))
            {
                Characters.Active[user].RemoveExprRow(varName);
                await RespondAsync($"`{varName}` removed from rows.", ephemeral: true);
                return;
            }
            else if(Characters.Active[user].Grids.ContainsKey(varName))
            {
                Characters.Active[user].RemoveGrid(varName);
                await RespondAsync($"`{varName}` removed from grids.", ephemeral: true);
                return;
            }

            await RespondAsync($"No variable `{varName}` found.", ephemeral: true);
            return;
        }

        [SlashCommand("var", "Create/modify expressions, rows, and grids.")]
        public async Task Var(VarAction action, string varName = "")
        {
            user = Context.Interaction.User.Id;              
        
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var varToUpper = varName.ToUpper().Replace(' ', '_');
            if(varName != "" && !validVar.IsMatch(varToUpper))
            {
                await RespondAsync($"Invalid variable `{varToUpper}`. a-Z and underscores/spaces only. Names must not exceed 30 characters in length.", ephemeral: true);
                return;
            }

            switch(action)
            {
                case VarAction.ListVars:
                    await VarList();
                    return;
                case VarAction.ListBonus:
                    await VarListBonuses();
                    return;
                case VarAction.SetRow:
                    await RespondWithModalAsync<ExprRowModal>("set_row");
                    return;
                case VarAction.SetGrid:
                    await RespondWithModalAsync<GridModal>("set_grid");
                    return;
                case VarAction.SetExpr:
                    await VarSetExpr(varToUpper);
                    return;
                case VarAction.Remove:
                    await VarRemove(varToUpper);
                    return;
            }
        }

        [ModalInteraction("set_row")]
        public async Task NewRow(ExprRowModal modal)
        {
            user = Context.Interaction.User.Id;

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
                }
                else if(exprs[i] == "")
                    break;
                else
                {
                    await RespondAsync($"Invalid Input @ Expression {i + 1}", ephemeral: true);
                    return;
                }
            }
            var row = new ExprRow()
            {
                RowName = modal.Name,
                Set = new List<Expr>()
            };

            for(int i = 0; i < rowExprNames.Length; i++)
                if(!string.IsNullOrEmpty(rowExprNames[i]))
                    row.Set.Add(new Expr(rowExprNames[i], rowExprs[i]));

            Characters.Active[user].AddExprRow(row);
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
            user = Context.Interaction.User.Id;

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

            string name = modal.Name;

            var exprs = new string[strings.Count];

            for(int i = 0; i < strings.Count; i++)
                exprs[i] = strings[i];

            Characters.Active[user].AddGrid(name, exprs.ToList());
            await RespondAsync($"Created {name}!", ephemeral: true);
        }

        [SlashCommand("row", "Get one or many rows (up to 5)")]
        public async Task GetRowCommand(string rowOne, string rowTwo = "", string rowThree = "", string rowFour = "", string rowFive = "")
        {
            user = Context.Interaction.User.Id;
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

        [SlashCommand("grid", "Call a saved set of rows")]
        public async Task GridGetCommand(string gridName)
        {
            user = Context.Interaction.User.Id;

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
      
        [SlashCommand("best", "List creature by name or index number")]
        public async Task PresetBestiaryCommand(string nameOrNumber = "", bool showInfo = false)
        {
            
            if(nameOrNumber == "")
            {
                if(bestiary == null)   
                    bestiary = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListBestiary());
                using var stream = new MemoryStream(bestiary);
                await RespondWithFileAsync(stream, $"Bestiary.txt", ephemeral: true);
                return;
            }      

            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Bestiary.FirstOrDefault(x => x.Name!.ToUpper() == nameOrNumber);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Bestiary.IndexOf(nameVal);
            else if(!int.TryParse(nameOrNumber, out outVal))
            {
                await RespondAsync($"{nameOrNumber} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Bestiary.Count)
            {
                var creature = DataMap.BaseCampaign.Bestiary[outVal];
                Embed[] ebs = null;
                var sa = creature.GetSpecialAbilities();

                if(showInfo)
                {
                    if(sa != null)
                        ebs = new Embed[2] { new EmbedBuilder().WithDescription(creature.ToString()).Build(), new EmbedBuilder().WithDescription(sa).Build() };
                    else
                        ebs = new Embed[1] { new EmbedBuilder().WithDescription(creature.ToString()).Build() };
                }
                else ebs = new Embed[1] { new EmbedBuilder().WithDescription(creature.GetSmallBlock()).Build()};


                var regex = new Regex(@"(^(?:or )?[+]?[0-9a-z ]*)(?:([-+][0-9]{1,2})?[/]?)* \(([0-9]{1,2}d[0-9]{1,2}(?:[-+][0-9]{1,3})?)(?:.*([+][0-9]{1,2}d[0-9]{1,2}).*\))*");
                string[] melee  = new string[5] { creature.MeleeOne!, creature.MeleeTwo!, creature.MeleeThree!, creature.MeleeFour!, creature.MeleeFive! };
                string[] ranged = new string[2] { creature.RangedOne!, creature.RangedTwo! };

                
                var cb = new ComponentBuilder();
                if(melee[0] != "")
                    for(int i = 0; i < melee.Length; i++)
                        if(melee[i] != "")
                        {
                            var match = regex.Match(melee[i]);
                            if(match.Success && match.Groups.Count > 3)
                            {
                                var row = new ActionRowBuilder();
                                for(int j = 0; j < match.Groups[2].Captures.Count; j++)
                                {
                                    Console.WriteLine($"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i + i * i}");
                                    if(j == 0)
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: $"{match.Groups[1].Value} {match.Groups[2].Captures[j].Value}");
                                    else if(j < 4)
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: match.Groups[2].Captures[j].Value);
                                }
                                row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")},Damage{i}", label: $"{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")}");
                                cb.AddRow(row);
                            }
                        }
                

                await RespondAsync(embeds: ebs, components: cb.Build(), ephemeral: true);

                if(ranged[0] != "")
                {                    
                    cb = new ComponentBuilder();
                    for(int i = 0; i < ranged.Length; i++)
                        if(ranged[i] != "")
                        {
                            var match = regex.Match(ranged[i]);
                            if(match.Success) Console.WriteLine("match");
                            if(match.Success && match.Groups.Count > 3)
                            {
                                var row = new ActionRowBuilder();
                                for(int j = 0; j < match.Groups[2].Captures.Count; j++)
                                {
                                    if(j == 0)
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: $"{match.Groups[1].Value} {match.Groups[2].Captures[j].Value}");
                                    else
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: match.Groups[2].Captures[j].Value);
                                }
                                row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")},Damage{i}", label: $"{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")}");
                                cb.AddRow(row);
                            }
                        }
                    await FollowupAsync(components: cb.Build(), ephemeral: true);
                }             
                
                return;            
            }
        }
        
        [ComponentInteraction("rowbest:*,*,*")]
        public async Task ButtonPressedBest(string creatureName, string expr, string name)
        {
            user = Context.Interaction.User.Id;

            var sb = new StringBuilder();
            var result = Parser.Parse(expr).Eval(null, sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription($"{creatureName}")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"__Events__", $"{sb}");

            await RespondAsync(embed: builder.Build());
        }
        
        async Task<InvItem> ConvertItem(Item item)
        {
            var task = Task.Run(() =>
            {
                return new InvItem()
                {
                    Base = item.Name!,
                    Name = item.Name!,
                    Quantity = 1,
                    Value = decimal.TryParse(item.Price, out decimal outVal) ? outVal : 0m,
                    Weight = item.Weight!.Value,
                };
            });
            return await task;
        }

        [ComponentInteraction("add_item:*,*")]
        public async Task ButtonPressedAddItem(int index, int custom = 0)
        {
            var item = DataMap.BaseCampaign.Items[index];

            if(custom != 0)
                await RespondWithModalAsync(CreateBaseItemModal(item).Build());
            else
            {
                Characters.Active[user].InventoryAdd(await ConvertItem(item));
                await RespondAsync($"{item.Name} added", ephemeral: true);
            }
        }

        [ComponentInteraction("apply_item:*")]
        public async Task ButtonPressedApplyItem(int index)
        {
            var formulae = DataMap.BaseCampaign.Items[index].Formulae!.Split(';');
            var sb = new StringBuilder();
            sb.AppendLine(); sb.AppendLine();
            for(int i = 0; i < formulae.Length; i++)
                Parser.Parse(formulae[i]).Eval(Characters.Active[user], sb);
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithAuthor(Context.Interaction.User.Username, Context.Interaction.User.GetAvatarUrl())
                .WithDescription($"*{Characters.Active[user].CharacterName}* {sb}");
            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("item", "Item and inventory management")]
        public async Task ItemCommand(string nameOrNumber = "", SizeType size = SizeType.Medium)
        {
            user = Context.User.Id;
            var index = -1;    

            if(nameOrNumber != "")
            {
                if(int.TryParse(nameOrNumber, out int outVal) && outVal >= 0 && outVal < DataMap.BaseCampaign.Items.Count)
                    index = outVal;
                else
                    index = DataMap.BaseCampaign.Items.FindIndex(x => x.Name!.ToUpper() == nameOrNumber.ToUpper())!;              
            }

            if(index == -1)
            {
                if(items == null)
                    items = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListItems());
                using var stream = new MemoryStream(items!);
                await RespondWithFileAsync(stream, $"{Characters.Active[user].CharacterName}_Items.txt", ephemeral: true);
            }
            else
            {
                var item = DataMap.BaseCampaign.Items[index];
                var eb = new EmbedBuilder()
                    .WithDescription(item.ToString());
                var cb = new ComponentBuilder()
                    .WithButton("Add", $"add_item:{index},0")
                    .WithButton("Custom", $"add_item:{index},1");
                if(item.Formulae != "")
                    cb.WithButton("Apply", $"apply_item:{index}");
                if(item.Type == "Weapon")
                    cb.WithButton("Expressions", $"new_row:{index},{(int)size}");
                
                    
                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
            return;
        }    

        [ComponentInteraction("new_row:*,*")]
        async public Task NewRowInteraction(int id, int size)
        {
;           var item = DataMap.BaseCampaign.Items[id];
            await RespondWithModalAsync(CreateRowModal(item.Name!, CreateWeaponExpressions(item, size)).Build());
        }

        ModalBuilder CreateRowModal(string name, string exprs)
        {         
            var mb = new ModalBuilder()
                .WithCustomId($"new_row")
                .WithTitle("New-Row")
                .AddTextInput("Name", "row_name", value: name)
                .AddTextInput("Expressions", "item_exprs", TextInputStyle.Paragraph, value: exprs);
            return mb;
        }

        ModalBuilder CreateBaseItemModal(Item item)
        {
            var mb = new ModalBuilder()
                    .WithCustomId($"base_item:{item.Name}")
                    .WithTitle($"Add-Item: {item.Name}")
                    .AddTextInput("Custom Name", "item_custom", value: item.Name, maxLength: 50, required: false)
                    .AddTextInput("Quantity", "item_qty", value: "1")                    
                    .AddTextInput("Weight", "item_weight", value: item.Weight.ToString(), maxLength: 20)
                    .AddTextInput("Value", "item_value", value: item.Price)                    
                    .AddTextInput("Notes", "item_notes", TextInputStyle.Paragraph, required: false);
            return mb;        
        }

        string CreateWeaponExpressions(Item item, int size)
        {
            var split = item.Offense!.Split('/');            
            var weaponSize = split[sizes[Enum.GetName(typeof(SizeType), size)!]];

            var qualities = split[11].Split('&');
            var damages = weaponSize!.Split('&', options: StringSplitOptions.RemoveEmptyEntries);
            var categories = split[14].Split('&');
            var sb = new StringBuilder();
            for(int i = 0; i < categories.Length; i++)
            {
                for(int j = 0; j < damages.Length; j++)
                {
                    switch(categories[i])
                    {
                        case "Light":
                        case "One-Handed":
                            if(j == 0) sb.AppendLine($"{item.Name}:ATK_STR");
                            if(j == 0 || (j > 0 && damages[j] != damages[j - 1])) sb.AppendLine($"{damages[j]}:{damages[j]}+DMG_STR");
                            break;
                        case "Two-Handed":
                            if(j == 0) sb.AppendLine($"{item.Name}:ATK_STR");
                            if(j == 0 || (j > 0 && damages[j] != damages[j - 1])) sb.AppendLine($"{damages[j]}:{damages[j]}+th(DMG_STR)");
                            Console.WriteLine(damages[j]);
                            break;
                        case "Ranged":
                            if(j == 0) sb.AppendLine($"{item.Name}:ATK_DEX");
                            break;
                        case "Thrown":
                            if(j == 0) sb.AppendLine($"Throw:ATK_DEX");
                            if(i == 0 && (j == 0 || (j > 0 && damages[j] != damages[j - 1]))) sb.AppendLine($"{damages[j]}:{damages[j]}+DMG_STR");
                            break;
                    }
                }              
            }
            
            for(int i = 0; i < qualities.Length; i++)
            {
                switch(qualities[i])
                {
                    case "disarm":
                        sb.AppendLine("Disarm:DISARM + 2");
                        break;
                    case "distracting":
                        sb.AppendLine("Distracting:BLF + 2");
                        break;
                    case "sunder":
                        sb.Append("Sunder:SUNDER + 2");
                        break;
                }
            }
            
            return sb.ToString();                                     
        }

        [ComponentInteraction("mod:*")]
        public async Task ModOptions(string modName)
        {
            user = Context.Interaction.User.Id;
            var sb = new StringBuilder();
            if(lastTargets.ContainsKey(user) && lastTargets[user] != null)
            {
                for(int i = 0; i < lastTargets[user].Count; i++)
                {
                    if(Characters.Active.ContainsKey(lastTargets[user][i].Id))
                    {
                        sb.AppendLine(Characters.Active[lastTargets[user][i].Id].CharacterName);
                        Characters.Active[lastTargets[user][i].Id].AddBonuses(StatModifier.Mods[modName]);                        
                    }
                }
            }
            else
            {
                sb.AppendLine(Characters.Active[user].CharacterName);
                Characters.Active[user].AddBonuses(StatModifier.Mods[modName]);
            }

            var eb = new EmbedBuilder()
                .WithTitle($"Mod({modName})")
                .WithDescription($"```{sb}```");

            foreach(var bonus in StatModifier.Mods[modName])
                eb.AddField(name: bonus.StatName, value: $"{bonus.Bonus.Value} {Enum.GetName(bonus.Bonus.Type)} bonus", inline: true);
            await RespondAsync(embed: eb.Build(), ephemeral: true);
            lastTargets[user] = null!;
        }

        [SlashCommand("mod", "Apply or remove a modifier to one or many targets")]
        public async Task PresetModifierCommand(ModAction action, string modName = "", string targets = "")
        {
            if(action == ModAction.List && modName == "")
            {
                if(mods == null)
                    mods = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListMods());
                using var stream = new MemoryStream(mods);
                await RespondWithFileAsync(stream, "Mods.txt", ephemeral: true);
                return;
            }

            user = Context.Interaction.User.Id;
            var modToUpper = modName.ToUpper().Replace(' ', '_');
            var sb = new StringBuilder();
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == ModAction.Add)
            {

                if(!DataMap.BaseCampaign.Modifiers.ContainsKey(modToUpper))
                {
                    await RespondAsync("No mod by that name found", ephemeral: true);
                    return;
                }

                if(targets != "")
                {
                    var targetList = new List<IUser>();
                    var replace = targetReplace.Replace(targets, " ");
                    var split = replace.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    for(int i = 0; i < split.Length; i++)
                    {
                        var id = 0ul;
                        ulong.TryParse(split[i], out id);
                        var dUser = await Program.client.GetUserAsync(id);
                        if(dUser != null) targetList.Add(dUser);
                    }

                    if(targetList.Count > 0)
                    {
                        lastTargets[user] = targetList;

                        if(DataMap.BaseCampaign.Modifiers[modToUpper] == null)
                        {
                            for(int i = 0; i < targetList.Count; i++)
                            {
                                if(Characters.Active.ContainsKey(targetList[i].Id))
                                {
                                    sb.AppendLine(targetList[i].Mention);
                                    Characters.Active[targetList[i].Id].AddBonuses(StatModifier.Mods[modToUpper]);
                                    await collection.ReplaceOneAsync(x => x.Id == Characters.Active[targetList[i].Id].Id, Characters.Active[targetList[i].Id]);

                                    var eb = new EmbedBuilder()
                                        .WithTitle($"Mod({modToUpper})")
                                        .WithDescription(sb.ToString());

                                    foreach(var bonus in StatModifier.Mods[modToUpper])
                                        eb.AddField(name: bonus.StatName, value: $"{bonus.Bonus.Value} {Enum.GetName(bonus.Bonus.Type)} bonus", inline: true);

                                    await RespondAsync(embed: eb.Build());
                                }
                            }
                        }
                        else
                        {
                            var cb = new ComponentBuilder();
                            for(int i = 0; i < DataMap.BaseCampaign.Modifiers[modToUpper].Count; i++)
                                cb.WithButton(customId: $"mod:{DataMap.BaseCampaign.Modifiers[modToUpper][i].Item1}", label: DataMap.BaseCampaign.Modifiers[modToUpper][i].Item2);
                            await RespondAsync(components: cb.Build(), ephemeral: true);
                        }
                        return;
                    }
                }
                else
                {
                    lastTargets[user] = null;
                    if(DataMap.BaseCampaign.Modifiers[modToUpper] == null)
                    {
                        if(Characters.Active.ContainsKey(user))
                        {
                            sb.AppendLine(Characters.Active[user].CharacterName);
                            Characters.Active[user].AddBonuses(StatModifier.Mods[modToUpper]);
                            await collection.ReplaceOneAsync(x => x.Id == Characters.Active[user].Id, Characters.Active[user]);

                            var eb = new EmbedBuilder()
                                       .WithTitle($"Mod({modToUpper})")
                                       .WithDescription(sb.ToString());

                            foreach(var bonus in StatModifier.Mods[modToUpper])
                                eb.AddField(name: bonus.StatName, value: $"{bonus.Bonus.Value} {Enum.GetName(bonus.Bonus.Type).ToLower()} bonus", inline: true);

                            await RespondAsync(embed: eb.Build(), ephemeral: true);
                        }
                    }
                    else
                    {
                        var cb = new ComponentBuilder();

                        for(int i = 0; i < DataMap.BaseCampaign.Modifiers[modToUpper].Count; i++)
                            cb.WithButton(customId: $"mod:{DataMap.BaseCampaign.Modifiers[modToUpper][i].Item1}", label: DataMap.BaseCampaign.Modifiers[modToUpper][i].Item2);
                        await RespondAsync(components: cb.Build(), ephemeral: true);
                    }
                }
            }

            if(action == ModAction.Remove)
            {
                if(targets != "")
                {
                    var targetList = await Utility.ParseTargets(targets);

                    if(targetList.Count > 0)
                    {
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Characters.Active.ContainsKey(targetList[i].Id))
                            {
                                sb.AppendLine(targetList[i].Mention);
                                Characters.Active[targetList[i].Id].ClearBonus(modToUpper);
                            }
                        }
                        await RespondAsync($"Removed {modToUpper} from: ```{sb}```", ephemeral: true);
                    }
                }
                else
                {
                    Characters.Active[user].ClearBonus(modToUpper);
                    await collection.ReplaceOneAsync(x => x.Id == Characters.Active[user].Id, Characters.Active[user]);
                    await RespondAsync($"{modToUpper} removed from all stats", ephemeral: true);
                }
            }
        }   

        [SlashCommand("shape", "Generate attacks based on a creature's shape")]
        public async Task PresetShapeCommand(string nameOrNumber = "", AbilityScoreHit hitMod = AbilityScoreHit.STR, bool multiAttack = false)
        { 
            if(nameOrNumber == "")
            {
                if(shapes == null)
                    shapes = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListShapes());
                using var stream = new MemoryStream(shapes);
                await RespondWithFileAsync(stream, $"Shapes.txt", ephemeral: true);
                return;
            }
   
            user = Context.Interaction.User.Id;
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = nameOrNumber.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Shapes.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Shapes.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Shapes.Count)
            {
                var shape = DataMap.BaseCampaign.Shapes[outVal];

                var primary = new List<(string, string)>();
                var secondary = new List<(string, string)>();

                if(shape.Bite != "")        primary.Add(("bite", shape.Bite!));
                if(shape.Claws != "")       primary.Add(("claw", shape.Claws!));
                if(shape.Gore != "")        primary.Add(("gore", shape.Gore!));
                if(shape.Slam != "")        primary.Add(("slam", shape.Slam!));
                if(shape.Sting != "")       primary.Add(("sting", shape.Sting!));
                if(shape.Talons != "")      primary.Add(("talon", shape.Talons!));

                if(shape.Hoof != "")        secondary.Add(("hoof", shape.Hoof!));
                if(shape.Tentacle != "")    secondary.Add(("tentacle", shape.Tentacle!));
                if(shape.Wing != "")        secondary.Add(("wing", shape.Wing!));
                if(shape.Pincers != "")     secondary.Add(("pincer", shape.Pincers!));
                if(shape.Tail != "")        secondary.Add(("tail", shape.Tail!));

                if(shape.Other != "")
                {
                    var oSplit = shape.Other!.Split('/');
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
                            if(splitCount.Length > 1) row.Set.Add(new Expr() { Name = $"{splitCount[0]} {primary[i].Item1}s ({splitCount[1]})", Expression = $"{splitCount[1]}+DMG_STR" });
                            else row.Set.Add(new Expr() { Name = $"{primary[i].Item1} ({splitCount[0]})", Expression = $"{splitCount[0]}+DMG_STR" });
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
                            if(splitCount.Length > 1) row.Set.Add(new Expr() { Name = $"{splitCount[0]} {secondary[i].Item1}s ({splitCount[1]})", Expression = splitCount[1] });
                            else row.Set.Add(new Expr() { Name = $"{secondary[i].Item1} ({splitCount[0]})", Expression = splitCount[0] });
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

                var eb = new EmbedBuilder()
                    .WithDescription(shape.ToString());

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
        }

        [SlashCommand("spell", "Get spell info")]
        public async Task PresetSpellCommand(string nameOrNumber = "", string expr = "")
        {     
            if(nameOrNumber == "")
            {
                if(spells == null)
                    spells = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListSpells());
                using var stream = new MemoryStream(spells);
                await RespondWithFileAsync(stream, $"Spells.txt", ephemeral: true);
                return;
            }
            
            user = Context.Interaction.User.Id;
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var toUpper = nameOrNumber.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Spells.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Spells.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Spells.Count)
            {
                var spell = DataMap.BaseCampaign.Spells[outVal];
                
                var eb = new EmbedBuilder()                  
                      .WithDescription(spell.ToString());
                          
                if(expr != "")
                {
                    var split = expr.Split(':');
                    if(split.Length == 2)
                    {
                        var level = 0;
                        if(int.TryParse(split[0], out level))
                        {
                            var sb = new StringBuilder();
                            var stats = Characters.Active[user];
                            var DC = 10 + level + Parser.Parse($"MOD_{split[1].ToUpper()}").Eval(stats, sb);
                            var CL = stats[$"CL_{split[1].ToUpper()}"] + stats["CL_BONUS"];
                            var properties = spell.Properties.Split('/');
                            for(int i = 0; i < properties.Length; i++)
                            {
                                DC += stats[$"DC_{properties[i]}"];
                                CL += stats[$"CL_{properties[i]}"];
                            }
                            eb.WithColor(Color.Magenta)
                              .WithTitle($"DC—{DC} -:- CL—{CL}");
                            if(sb.Length > 0) eb.AddField("**Events**", sb.ToString());
                        }
                    }       
                }
                await RespondAsync(embed: eb.Build());
                return;
            }
        }              

        [ComponentInteraction("row:*,*")]
        public async Task ButtonPressedRow(string expr, string name)
        {
            user = Context.Interaction.User.Id;

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

        ActionRowBuilder BuildRow(ExprRow exprRow)
        {
            var ar = new ActionRowBuilder();

            for(int i = 0; i < exprRow.Set.Count; i++)
            {
                if(!string.IsNullOrEmpty(exprRow.Set[i].Expression))
                    ar.WithButton(customId: $"row:{exprRow.Set[i].Expression.Replace(" ", "")},{exprRow.Set[i].Name.Replace(" ", "")}", label: exprRow.Set[i].Name, disabled: (exprRow.Set[i].Expression == "") ? true : false);
            }          
            return ar;
        }        
    }
}
