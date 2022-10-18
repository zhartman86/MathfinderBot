using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization.Conventions;

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

            [ChoiceDisplay("List-Bonus")]
            ListBonus,            

            [ChoiceDisplay("List-Items")]
            ListItems,
            
            [ChoiceDisplay("Remove-Variable")]
            Remove
        }

        CommandHandler                          handler;
        
        static Regex validVar = new Regex(@"^[0-9A-Z_]{1,30}$");
        static Regex validExpr = new Regex(@"^[-0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,300}$");
        static Regex targetReplace = new Regex(@"\D+");

        static Dictionary<ulong, ExprRow>       lastRow     = new Dictionary<ulong, ExprRow>();
        static Dictionary<ulong, List<IUser>>   lastTargets = new Dictionary<ulong, List<IUser>>();        
        public static Dictionary<ulong, string> lastInputs  = new Dictionary<ulong, string>();  
        public static ExprRow                   exprRowData = null;
        ulong                                   user;       
        IMongoCollection<StatBlock>             collection;
                    
        static byte[] armor     = null;
        static byte[] bestiary  = null;
        static byte[] items     = null;
        static byte[] mods      = null;
        static byte[] shapes    = null;       
        static byte[] spells    = null;
        static byte[] weapons   = null;

        public Variable(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            collection  = Program.database.GetCollection<StatBlock>("statblocks");
        }
        
        [SlashCommand("var", "Create, modify, list, remove different kinds of variables.")]
        public async Task Var(VarAction action, string varName = "")
        {
            user = Context.Interaction.User.Id;              
        
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == VarAction.ListVars)
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

            
            if(action == VarAction.SetRow)
            {
                await RespondWithModalAsync<ExprRowModal>("set_row");
            }
                
            if(action == VarAction.SetGrid)
                await RespondWithModalAsync<GridModal>("set_grid");

            var varToUpper = varName.ToUpper().Replace(' ', '_');
            if(action == VarAction.SetExpr)
            {
                if(Characters.Active[user].Stats.ContainsKey(varToUpper))
                {
                    await RespondAsync($"`{varToUpper}` already exists as a stat.", ephemeral: true);
                    return;
                }

                var mValue = "";
                if(Characters.Active[user].Expressions.ContainsKey(varToUpper))
                    mValue = Characters.Active[user].Expressions[varToUpper];

                //I had to do this because I don't know how to set the value on an IModal upon construction.
                var mb = new ModalBuilder("Set-Expression()", "set_expr")
                    .AddTextInput(new TextInputBuilder($"{varToUpper}", "expr", value: mValue));

                lastInputs[user] = varToUpper;
                await RespondWithModalAsync(mb.Build());
                return;
            }

            
            if(!validVar.IsMatch(varToUpper))
            {
                await RespondAsync($"Invalid variable `{varToUpper}`. a-Z and underscores/spaces only. Names must not exceed 30 characters in length.", ephemeral: true);
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

        [SlashCommand("preset-armor", "List or apply an armor's stats to an active character")]
        public async Task PresetArmorCommand(EquipAction action, string nameOrNumber = "", int enhancement = 0)
        {
            if(action == EquipAction.Add && (!Characters.Active.ContainsKey(user) || Characters.Active[user] == null))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }


            if(action == EquipAction.List && nameOrNumber == "")
            {
                if(armor == null)
                    armor = Encoding.ASCII.GetBytes(DataMap.ListArmor());               
                using var stream = new MemoryStream(armor);
                await RespondWithFileAsync(stream, $"ArmorPresets.txt", ephemeral: true);
                return;
            }          

            var toUpper = nameOrNumber.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Armor.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Armor.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }


            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Armor.Count)
            {           
                if(action == EquipAction.List)
                {                               
                    var eb = new EmbedBuilder()
                            .WithDescription(DataMap.BaseCampaign.Armor[outVal].ToString());
                    await RespondAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }
                
                if(action == EquipAction.Add)
                {
                    var armor = DataMap.BaseCampaign.Armor[outVal];
                    if(armor.Type == "S")
                    {
                        Characters.Active[user].ClearBonus("SHIELD");
                        Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus { Name = "SHIELD", Type = BonusType.Shield, Value = armor.ShieldBonus.Value });
                        Characters.Active[user].Stats["AC_PENALTY"].AddBonus(new Bonus { Name = "SHIELD", Type = BonusType.Penalty, Value = armor.Penalty.Value });
                        if(enhancement > 0)
                            Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus { Name = "SHIELD", Type = BonusType.Enhancement, Value = enhancement });
                    }
                    else
                    {
                        Characters.Active[user].ClearBonus("ARMOR");
                        Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus { Name = "ARMOR", Type = BonusType.Armor, Value = armor.ArmorBonus.Value });
                        Characters.Active[user].Stats["AC_PENALTY"].AddBonus(new Bonus { Name = "ARMOR", Type = BonusType.Penalty, Value = armor.Penalty.Value });
                        if(armor.MaxDex != null)
                            Characters.Active[user].Stats["AC_MAXDEX"].AddBonus(new Bonus { Name = "ARMOR", Type = BonusType.Base, Value = armor.MaxDex.Value });
                        if(enhancement > 0)
                            Characters.Active[user].Stats["AC_BONUS"].AddBonus(new Bonus { Name = "ARMOR", Type = BonusType.Enhancement, Value = enhancement });
                    }

                    var eb = new EmbedBuilder()
                        .WithTitle($"Set-Armor()")
                        .WithDescription(armor.ToString());

                    var update = Builders<StatBlock>.Update.Set(x => x.Stats, Characters.Active[user].Stats);
                    await Program.UpdateSingleAsync(update, user);

                    await RespondAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }             
            }
            await RespondAsync($"{toUpper} not found", ephemeral: true);
        }
        
        [SlashCommand("preset-best", "List creature by name or index number")]
        public async Task PresetBestiaryCommand(string nameOrNumber = "", bool showInfo = false)
        {
            
            if(nameOrNumber == "")
            {
                if(bestiary == null)   
                    bestiary = Encoding.ASCII.GetBytes(DataMap.ListBestiary());
                using var stream = new MemoryStream(bestiary);
                await RespondWithFileAsync(stream, $"Bestiary.txt", ephemeral: true);
                return;
            }      

            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Bestiary.FirstOrDefault(x => x.Name.ToUpper() == nameOrNumber);
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

        [SlashCommand("preset-item", "List or add an item to your inventory")]
        public async Task PresetItemCommand(EquipAction action, string nameOrNumber = "")
        {
            if(action == EquipAction.Add && (!Characters.Active.ContainsKey(user) || Characters.Active[user] == null))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }


            if(action == EquipAction.List && nameOrNumber == "")
            {
                if(items == null)
                    items = Encoding.ASCII.GetBytes(DataMap.ListItems());
                using var stream = new MemoryStream(items!);
                await RespondWithFileAsync(stream, $"ItemPresets.txt", ephemeral: true);
                return;
            }
            var toUpper = nameOrNumber.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Items.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Items.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }
            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Items.Count)
            {
                if(action == EquipAction.List)
                {
                    var eb = new EmbedBuilder()
                            .WithDescription(DataMap.BaseCampaign.Items[outVal].ToString());
                    await RespondAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }

                if(action == EquipAction.Add)
                {
                    var item = DataMap.BaseCampaign.Items[outVal];
                    await AddToInventory(user, $"{item.Name}:{item.Weight}:{item.Value}");
                }

            }
            
        }
        
        [SlashCommand("preset-mod", "Apply or remove a specifically defined modifier to one or many targets")]
        public async Task PresetModifierCommand(ModAction action, string modName = "", string targets = "")
        {

            if(action == ModAction.List && modName == "")
            {
                if(mods == null)
                    mods = Encoding.ASCII.GetBytes(DataMap.ListMods());
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
                    var targetList = new List<IUser>();
                    var regex = new Regex(@"\D+");
                    var replace = regex.Replace(targets, " ");
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
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Characters.Active.ContainsKey(targetList[i].Id))
                            {
                                sb.AppendLine(targetList[i].Mention);
                                Characters.Active[targetList[i].Id].ClearBonus(modToUpper);
                                await collection.ReplaceOneAsync(x => x.Id == Characters.Active[targetList[i].Id].Id, Characters.Active[targetList[i].Id]);
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
                        await collection.ReplaceOneAsync(x => x.Id == Characters.Active[lastTargets[user][i].Id].Id, Characters.Active[lastTargets[user][i].Id]);
                    }
                }
            }
            else
            {
                sb.AppendLine(Characters.Active[user].CharacterName);
                Characters.Active[user].AddBonuses(StatModifier.Mods[modName]);
                await collection.ReplaceOneAsync(x => x.Id == Characters.Active[user].Id, Characters.Active[user]);
            }


            var eb = new EmbedBuilder()
                .WithTitle($"Mod({modName})")
                .WithDescription($"```{sb}```");

            foreach(var bonus in StatModifier.Mods[modName])
                eb.AddField(name: bonus.StatName, value: $"{bonus.Bonus.Value} {Enum.GetName(bonus.Bonus.Type)} bonus", inline: true);
            await RespondAsync(embed: eb.Build(), ephemeral: true);
            lastTargets[user] = null!;
        }

        [SlashCommand("preset-shape", "Generate attacks based on a creature's shape")]
        public async Task PresetShapeCommand(string nameOrNumber = "", AbilityScoreHit hitMod = AbilityScoreHit.STR, bool multiAttack = false)
        { 
            if(nameOrNumber == "")
            {
                if(shapes == null)
                    shapes = Encoding.ASCII.GetBytes(DataMap.ListShapes());
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

                if(shape.Bite != "") primary.Add(("bite", shape.Bite!));
                if(shape.Claws != "") primary.Add(("claw", shape.Claws!));
                if(shape.Gore != "") primary.Add(("gore", shape.Gore!));
                if(shape.Slam != "") primary.Add(("slam", shape.Slam!));
                if(shape.Sting != "") primary.Add(("sting", shape.Sting!));
                if(shape.Talons != "") primary.Add(("talon", shape.Talons!));

                if(shape.Hoof != "") secondary.Add(("hoof", shape.Hoof!));
                if(shape.Tentacle != "") secondary.Add(("tentacle", shape.Tentacle!));
                if(shape.Wing != "") secondary.Add(("wing", shape.Wing!));
                if(shape.Pincers != "") secondary.Add(("pincer", shape.Pincers!));
                if(shape.Tail != "") secondary.Add(("tail", shape.Tail!));

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

        [SlashCommand("preset-spell", "Get spell info")]
        public async Task PresetSpellCommand(string nameOrNumber = "", string expr = "")
        {     
            if(nameOrNumber == "")
            {
                if(spells == null)
                    spells = Encoding.ASCII.GetBytes(DataMap.ListSpells());
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
        
        [SlashCommand("preset-weapon", "Generate a preset row with modifiers")]
        public async Task PresetWeaponCommand(EquipAction action, string nameOrNumber = "", AbilityScoreHit hitMod = AbilityScoreHit.STR, AbilityScoreDmg damageMod = AbilityScoreDmg.STR, string hitBonus = "", string dmgBonus = "", SizeType size = SizeType.None, bool save = false)
        {
            if(action == EquipAction.Add && (!Characters.Active.ContainsKey(user) || Characters.Active[user] == null))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == EquipAction.List && nameOrNumber == "")
            {
                if(weapons == null)    
                    weapons = Encoding.ASCII.GetBytes(DataMap.ListWeapons());
                using var stream = new MemoryStream(weapons);
                await RespondWithFileAsync(stream, $"WeaponPresets.txt", ephemeral: true);
                return;
            }       

            var toUpper = nameOrNumber.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Weapons.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null) 
                outVal = DataMap.BaseCampaign.Weapons.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }
               
          
            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Weapons.Count)
            {
                var sizeType = SizeType.Medium;
                var weapon = DataMap.BaseCampaign.Weapons[outVal];
                var weaponSize = weapon.Medium;
                if(size != SizeType.None)
                {
                    switch(size)
                    {
                        case SizeType.Fine:
                            weaponSize = weapon.Fine;
                            break;
                        case SizeType.Diminutive:
                            weaponSize = weapon.Diminutive;
                            break;
                        case SizeType.Tiny:
                            weaponSize = weapon.Tiny;
                            break;
                        case SizeType.Small:
                            weaponSize = weapon.Small;
                            break;
                        case SizeType.Medium:
                            weaponSize = weapon.Medium;
                            break;
                        case SizeType.Large:
                            weaponSize = weapon.Large;
                            break;
                        case SizeType.Huge:
                            weaponSize = weapon.Huge;
                            break;
                        case SizeType.Gargantuan:
                            weaponSize = weapon.Gargantuan;
                            break;
                        case SizeType.Colossal:
                            weaponSize = weapon.Colossal;
                            break;                     
                    }
                }                
                else if(Characters.Active[user].Stats.ContainsKey("SIZE_MOD"))
                {
                    switch(Characters.Active[user]["SIZE_MOD"])
                    {
                        case (int)SizeType.Fine:
                            weaponSize = weapon.Fine;
                            break;
                        case (int)SizeType.Diminutive:
                            weaponSize = weapon.Diminutive;
                            break;
                        case (int)SizeType.Tiny:
                            weaponSize = weapon.Tiny;
                            break;
                        case (int)SizeType.Small:
                            weaponSize = weapon.Small;
                            break;
                        case (int)SizeType.Medium:
                            weaponSize = weapon.Medium;
                            break;
                        case (int)SizeType.Large:
                            weaponSize = weapon.Large;
                            break;
                        case (int)SizeType.Huge:
                            weaponSize = weapon.Huge;
                            break;
                        case (int)SizeType.Gargantuan:
                            weaponSize = weapon.Gargantuan;
                            break;
                        case (int)SizeType.Colossal:
                            weaponSize = weapon.Colossal;
                            break;
                        default:
                            weaponSize = weapon.Medium;
                            break;
                    }
                }

                var split = weaponSize.Split('/');
                var bonus = hitBonus != "" ? $"+{hitBonus}" : "";
                var row = new ExprRow()
                {
                    RowName = weapon.Name,
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
           
                var eb = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithTitle($"Weapon-Preset()")
                    .WithDescription(weapon.ToString(size != SizeType.None ? size : ((SizeType)Characters.Active[user]["SIZE_MOD"])));

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
        }

        [SlashCommand("preset-weapon-save", "Save the last called preset-weapon with a custom name")]
        public async Task SaveWeaponCommand(string name)
        {
            user = Context.Interaction.User.Id;

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

            var toUpper = name.ToUpper();
            if(!validVar.IsMatch(toUpper))
            {
                await RespondAsync("Invalid name", ephemeral: true);
                return;
            }
            
            var row = lastRow[user];
            Characters.Active[user].ExprRows[toUpper] = row;

            var update = Builders<StatBlock>.Update.Set(x => x.ExprRows[row.RowName], row);
            await Program.UpdateSingleAsync(update, user);
            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle($"Save-Row({toUpper})")
                .WithDescription($"You can call this with the `/row` command, using `{toUpper}` as the name");

            for(int i = 0; i < row.Set.Count; i++)
                if(!string.IsNullOrEmpty(row.Set[i].Name))
                    eb.AddField(name: row.Set[i].Name, value: row.Set[i].Expression, inline: true);

            await RespondAsync(embed: eb.Build(), ephemeral: true);
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
                    ar.WithButton(customId: $"row:{exprRow.Set[i].Expression.Replace(" ", "")},{exprRow.Set[i].Name.Replace(" ", "")}", label: exprRow.Set[i].Name, disabled: (exprRow.Set[i].Expression == "") ? true : false);
            }          
            return ar;
        }

        async Task AddToInventory(ulong userId, string item = "", int qty = 1)
        {
            if(item == "")
            {
                await RespondWithModalAsync<AddInvModal>($"add_inv");
                return;
            }
            Console.WriteLine(item);
            var split = item.Split(':', options: StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(split.Length);
            decimal outVal;
            var newItem = new Item()
            {
                Name = split[0],
                Weight = split.Length < 2 ? 0 : decimal.TryParse(split[1], out outVal) ? Math.Round(outVal, 5) : 0,
                Value = split.Length < 3 ? 0 : decimal.TryParse(split[2], out outVal) ? Math.Round(outVal, 5) : 0
            };

            for(int i = 0; i < qty; i++)
                Characters.Active[userId].Inventory.Add(newItem);

            var update = Builders<StatBlock>.Update.Set(x => x.Inventory, Characters.Active[userId].Inventory);
            await Program.UpdateSingleAsync(update, userId);
            await RespondAsync($"{newItem.Name} added", ephemeral: true);
            return;
        }
    }
}
