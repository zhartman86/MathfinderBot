using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

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

            [ChoiceDisplay("List-Mods")]
            ListMods,

            [ChoiceDisplay("List-Armor")]
            ListArmor,

            [ChoiceDisplay("List-Items")]
            ListItems,
            
            [ChoiceDisplay("List-Shapes")]
            ListShape,

            [ChoiceDisplay("List-Spells")]
            ListSpell,

            [ChoiceDisplay("List-Weapons")]
            ListWeapon,

            [ChoiceDisplay("Remove-Variable")]
            Remove
        }

        CommandHandler                          handler;
        
        static Regex                            validVar = new Regex(@"^[0-9A-Z_]{1,17}$");
        static Regex                            validExpr = new Regex(@"^[0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,100}$");
        static Regex                            targetReplace = new Regex(@"\D+");
        
        static Dictionary<ulong, List<IUser>>   lastTargets = new Dictionary<ulong, List<IUser>>();        
        public static Dictionary<ulong, string> lastInputs = new Dictionary<ulong, string>();
        static Dictionary<ulong, ExprRow>       lastRow = new Dictionary<ulong, ExprRow>();
        public static ExprRow                   exprRowData = null;
        ulong                                   user;
        
        IMongoCollection<StatBlock>             collection;
        
        static byte[]                           mods = null;
        
        static byte[]                           armor = null;
        static byte[]                           items = null;
        static byte[]                           shapes = null;       
        static byte[]                           spells = null;
        static byte[]                           weapons = null;

        public Variable(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            collection  = Program.database.GetCollection<StatBlock>("statblocks");
        }

        

        

        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "")
        {
            user = Context.Interaction.User.Id;


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

            if(action == VarAction.ListArmor)
            {
                if(varName != "")
                {
                    var toUpper = varName.ToUpper().Replace(' ', '_');
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
                        var eb = new EmbedBuilder()
                            .WithDescription(DataMap.Armor[outVal].ToString());

                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }
                
                if(armor == null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"{"#",-2} |{"NAME",-30} {"ARMOR/SHIELD",13}| {"PENALTY",8}|");
                    for(int i = 0; i < DataMap.Armor.Count; i++)
                        sb.AppendLine($"{i,-2} |{DataMap.Armor[i].Name,-30} {(DataMap.Armor[i].ArmorBonus > 0 ? $"{DataMap.Armor[i].ArmorBonus} armor" : $"{DataMap.Armor[i].ShieldBonus} shield"),13}| {DataMap.Armor[i].Penalty,8}|");
                    armor = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(armor);
                await RespondWithFileAsync(stream, $"ArmorPresets.txt", ephemeral: true);
                return;
            }

            if(action == VarAction.ListItems)
            {
                if(varName != "")
                {
                    var toUpper = varName.ToUpper().Replace(' ', '_');
                    var outVal = -1;
                    if(!int.TryParse(toUpper, out outVal))
                    {
                        await RespondAsync($"Invalid index: {toUpper}", ephemeral: true);
                        return;
                    }
                    
                    if(outVal >= 0 && outVal < DataMap.Items.Count)
                    {
                        var eb = new EmbedBuilder()
                            .WithDescription(DataMap.Items[outVal].ToString());

                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                    await RespondAsync($"Invalid index: {toUpper}", ephemeral: true);
                    return;

                }

                if(items == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Items.Count; i++)
                        sb.AppendLine($"{i,-4} |{DataMap.Items[i].Name,-45} |{DataMap.Items[i].Type,-20}");
                    items = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(items);
                await RespondWithFileAsync(stream, $"Items.txt", ephemeral: true);
                return;

            }
            
            if(action == VarAction.ListShape)
            {
                if(varName != "")
                {
                    var toUpper = varName.ToUpper().Replace(' ', '_');
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
                        var eb = new EmbedBuilder()
                            .WithDescription(DataMap.Shapes[outVal].ToString());

                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }

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

            if(action == VarAction.ListSpell)
            {
                if(varName != "")
                {
                    var toUpper = varName.ToUpper().Replace(' ', '_');
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
                        var eb = new EmbedBuilder()
                            .WithDescription(DataMap.Spells[outVal].ToString());

                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }

                if(spells == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Spells.Count; i++)
                        sb.AppendLine($"{i,-4} |{DataMap.Spells[i].Name,-25}");
                    spells = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(spells);
                await RespondWithFileAsync(stream, $"Spells.txt", ephemeral: true);
                return;
            }

            if(action == VarAction.ListWeapon)
            {
                if(varName != "")
                {
                    var toUpper = varName.ToUpper().Replace(' ', '_');
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
                        var eb = new EmbedBuilder()
                            .WithDescription(DataMap.Weapons[outVal].ToString());

                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }

                if(weapons == null)
                {
                    var sb = new StringBuilder();
                    for(int i = 0; i < DataMap.Weapons.Count; i++)
                        sb.AppendLine($"{i,-3} |{DataMap.Weapons[i].Name,-15}");
                    weapons = Encoding.ASCII.GetBytes(sb.ToString());
                }
                using var stream = new MemoryStream(weapons);
                await RespondWithFileAsync(stream, $"WeaponPresets.txt", ephemeral: true);
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

            var varToUpper = varName.ToUpper().Replace(' ', '_');
            if(!validVar.IsMatch(varToUpper))
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

                var mValue = "";
                if(Characters.Active[user].Expressions.ContainsKey(varToUpper))
                    mValue = Characters.Active[user].Expressions[varToUpper];

                //I had to do this because I don't know how to set the value on an IModal.
                var mb = new ModalBuilder("Set-Expression()", "set_expr")
                    .AddTextInput(new TextInputBuilder($"{varToUpper}", "expr", value: mValue));

                lastInputs[user] = varToUpper;
                await RespondWithModalAsync(mb.Build());
                return;
            }                                            
        
            if(action == VarAction.SetRow)           
                await RespondWithModalAsync<ExprRowModal>("set_row");
            
            if(action == VarAction.SetGrid)                  
                await RespondWithModalAsync<GridModal>("set_grid");
        }   

        [SlashCommand("row", "Get a row or rows")]
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


        [SlashCommand("preset-mod", "Apply or remove a specifically defined modifier to one or many targets")]
        public async Task ModifierCommand(ModAction action, string modName, string targets = "")
        {
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

                if(!DataMap.Modifiers.ContainsKey(modToUpper))
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

                        if(DataMap.Modifiers[modToUpper] == null)
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
                            for(int i = 0; i < DataMap.Modifiers[modToUpper].Count; i++)
                                cb.WithButton(customId: $"mod:{DataMap.Modifiers[modToUpper][i].Item1}", label: DataMap.Modifiers[modToUpper][i].Item2);
                            await RespondAsync(components: cb.Build(), ephemeral: true);
                        }
                        return;
                    }
                }
                else
                {
                    lastTargets[user] = null;
                    if(DataMap.Modifiers[modToUpper] == null)
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

                        for(int i = 0; i < DataMap.Modifiers[modToUpper].Count; i++)
                            cb.WithButton(customId: $"mod:{DataMap.Modifiers[modToUpper][i].Item1}", label: DataMap.Modifiers[modToUpper][i].Item2);
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
            lastTargets[user] = null;
        }

        [SlashCommand("preset-armor", "Apply an armor's stats to an active character")]
        public async Task PresetArmorCommand(string numberOrName, int enhancement = 0)
        {
            user = Context.Interaction.User.Id;
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
                              
                var eb = new EmbedBuilder()
                    .WithTitle($"Set-Armor()")
                    .WithDescription(armor.ToString());

                var update = Builders<StatBlock>.Update.Set(x => x.Stats, Characters.Active[user].Stats);
                await Program.UpdateSingleAsync(update, user);

                await RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }
            await RespondAsync($"{toUpper} not found", ephemeral: true);
        }

        [SlashCommand("preset-shape", "Generate attacks based on a creature's shape")]
        public async Task PresetShapeCommand(string numberOrName, AbilityScoreHit hitMod = AbilityScoreHit.STR, bool multiAttack = false)
        {
            user = Context.Interaction.User.Id;
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
        public async Task PresetSpellCommand(string numberOrName, string expr)
        {
            user = Context.Interaction.User.Id;
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
                var spell = DataMap.Spells[outVal];
                
                var eb = new EmbedBuilder()                  
                      .WithDescription(spell.ToString());
                          
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
                        
                        await RespondAsync(embed: eb.Build());
                        return;
                    }
                }
                await RespondAsync("Invalid input", ephemeral: true);
            }
        }
        
        [SlashCommand("preset-weapon", "Generate a preset row with modifiers")]
        public async Task PresetWeaponCommand(string numberOrName, AbilityScoreHit hitMod = AbilityScoreHit.STR, AbilityScoreDmg damageMod = AbilityScoreDmg.BONUS, string hitBonus = "", string dmgBonus = "", SizeType size = SizeType.None)
        {
            user = Context.Interaction.User.Id;
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
                var sizeType = SizeType.Medium;
                var weapon = DataMap.Weapons[outVal];
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

        [SlashCommand("preset-save", "Save the last called preset-weapon with a custom name")]
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

        [ComponentInteraction("row:*,*,*")]
        public async Task ButtonPressed(ulong user, string expr, string name)
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
                    ar.WithButton(customId: $"row:{user},{exprRow.Set[i].Expression.Replace(" ", "")},{exprRow.Set[i].Name.Replace(" ", "")}", label: exprRow.Set[i].Name, disabled: (exprRow.Set[i].Expression == "") ? true : false);
            }          
            return ar;
        }
    }
}
