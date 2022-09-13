using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using MongoDB.Driver;

namespace MathfinderBot
{
    public class Mod : InteractionModuleBase
    {
       

        public enum TemplateMode
        {
            Add,
            Remove,
            List,
            ListAll
        }
        
        public enum BonusAction
        {
            Add,
            Remove
        }

        private static Dictionary<ulong, List<ulong>> lastTargets = new Dictionary<ulong, List<ulong>>();
        private static Regex ValidVar = new Regex("^[A-Z_0-9]{1,17}$");
        private ulong user;
        private IMongoCollection<StatBlock> collection;
        
        
        public override void BeforeExecute(ICommandInfo command)
        {
            user = Context.User.Id;
            collection = Program.database.GetCollection<StatBlock>("statblocks");
        }

        //[SlashCommand("template", "Templates include classes, as well as other sets of modifiers.")]
        //public async Task TemplateCommand(TemplateMode mode, string templateName = "")
        //{

        //    if(mode == TemplateMode.ListAll)
        //    {
        //        var sb = new StringBuilder();
        //        foreach(var template in Template.Templates.Values)
        //        {
        //            sb.AppendLine($"~{template.Name}~");
        //            sb.AppendLine();
        //            sb.AppendLine("STATS:");
        //            foreach(var stat in template.Stats) sb.AppendLine(@$"|{stat.Key,-15} |{stat.Value.Value,-30}");
        //            sb.AppendLine();
        //            sb.AppendLine("EXPRESSIONS (ADDED):");
        //            foreach(var setExpr in template.SetExpressions) sb.AppendLine(@$"|{setExpr.Key,-15} |{setExpr.Value,-30}");
        //            sb.AppendLine();
        //            sb.AppendLine("EXPRESSIONS (MODIFIED):");
        //            foreach(var modExpr in template.ModExpressions) sb.AppendLine(@$"|{modExpr.Key,-15} |{modExpr.Value,-30}");
        //            sb.AppendLine();
        //            sb.AppendLine();                 
        //        }
                
        //        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
        //        await RespondWithFileAsync(stream, "MF_TEMPLATES.txt", ephemeral: true);
        //    }


        //    if(!Characters.Active.ContainsKey(user))
        //    {
        //        await RespondAsync("No active character", ephemeral: true);
        //        return;
        //    }

        //    if(mode == TemplateMode.List)
        //    {
        //        var sb = new StringBuilder();
        //        foreach(var template in Characters.Active[user].Templates.Values)
        //        {
        //            sb.Append("```");
        //            sb.AppendLine($"~{template.Name}~");
        //            sb.AppendLine();
        //            sb.AppendLine("STATS:");
        //            foreach(var stat in template.Stats) sb.AppendLine(@$"|{stat.Key, -15} |{stat.Value.Value, -30}");
        //            sb.AppendLine();
        //            sb.AppendLine("EXPRESSIONS (ADDED):");
        //            foreach(var setExpr in template.SetExpressions) sb.AppendLine(@$"|{setExpr.Key, -15} |{setExpr.Value,-30}");
        //            sb.AppendLine();
        //            sb.AppendLine("EXPRESSIONS (MODIFIED):");
        //            foreach(var modExpr in template.ModExpressions) sb.AppendLine(@$"|{modExpr.Key, -15} |{modExpr.Value,-30}");
        //            sb.Append("```");
        //            sb.AppendLine();
        //            sb.AppendLine();
        //        }

        //        var eb = new EmbedBuilder()
        //            .WithColor(Color.DarkMagenta)
        //            .WithTitle($"List-Templates({Characters.Active[user].CharacterName})")
        //            .WithDescription(sb.ToString());

        //        await RespondAsync(embed: eb.Build(), ephemeral: true);

        //    }

        //    if(!Template.Templates.ContainsKey(templateName))
        //    {
        //        await RespondAsync($"{templateName} not found");
        //        return;
        //    }

        //    if(mode == TemplateMode.Add)
        //    {
        //        var sb = new StringBuilder();
        //        if(Characters.Active[user].Templates.ContainsKey(templateName))
        //            sb.AppendLine($"{templateName} already found. Will overwrite!");
                
        //        Characters.Active[user].AddTemplate(templateName, sb);

        //        var eb = new EmbedBuilder()
        //            .WithColor(Color.DarkMagenta)
        //            .WithTitle($"Add-Template({templateName})")
        //            .WithDescription(sb.ToString());

        //        await Program.UpdateStatBlock(Characters.Active[user]);
        //        await RespondAsync(embed: eb.Build());
        //    }
            

        //    if(mode == TemplateMode.Remove)
        //    {
        //        if(!Characters.Active[user].Templates.ContainsKey(templateName))
        //        {
        //            await RespondAsync($"{templateName} not found on active character.", ephemeral: true);
        //            return;
        //        }

        //        var sb = new StringBuilder();
        //        Characters.Active[user].RemoveTemplate(templateName, sb);

        //        var eb = new EmbedBuilder()
        //            .WithColor(Color.DarkMagenta)
        //            .WithTitle($"Remove-Template({templateName})")
        //            .WithDescription(sb.ToString());

        //        await Program.UpdateStatBlock(Characters.Active[user]);
        //        await RespondAsync(embed: eb.Build());
        //    }        
        //}

   
        [SlashCommand("mod", "Apply a specifically defined modifier to one or many targets")]
        public async Task ModifierCommand(BonusAction action, string modName, string targets = "")
        {
            var modToUpper = modName.ToUpper();
            var sb = new StringBuilder();
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == BonusAction.Add)
            {
               
                if(!DataMap.Modifiers.ContainsKey(modToUpper))
                {
                    await RespondAsync("No mod by that name found", ephemeral: true);
                    return;
                }

               
                if(targets != "")
                {
                    var targetList = new List<ulong>();
                    var split = targets.Trim(new char[] { '<', '>', '!', '@' }).Split(' ');
                    Console.WriteLine(split.Length);

                    for(int i = 0; i < split.Length; i++)
                    {
                        var id = 0ul;
                        ulong.TryParse(split[i], out id);
                        var dUser = await Program.client.GetUserAsync(id);
                        if(dUser != null) targetList.Add(dUser.Id);
                    }


                    if(targetList.Count > 0)
                    {
                        lastTargets[user] = targetList;

                        if(DataMap.Modifiers[modToUpper] == null)
                        {
                            for(int i = 0; i < targetList.Count; i++)
                            {
                                if(Characters.Active.ContainsKey(targetList[i]))
                                {
                                    sb.AppendLine(Characters.Active[targetList[i]].CharacterName);
                                    Characters.Active[targetList[i]].AddBonuses(StatModifier.Mods[modToUpper]);
                                    await collection.ReplaceOneAsync(x => x.Id == Characters.Active[targetList[i]].Id, Characters.Active[targetList[i]]);

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
                                eb.AddField(name: bonus.StatName, value: $"{bonus.Bonus.Value} {Enum.GetName(bonus.Bonus.Type)} bonus", inline: true);

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
                 
            if(action == BonusAction.Remove)
            {
                if(targets != "")
                {
                    var targetList = new List<ulong>();
                    var split = targets.Trim(new char[] { '<', '>', '!', '@' }).Split(' ');
                    Console.WriteLine(split.Length);

                    for(int i = 0; i < split.Length; i++)
                    {
                        var id = 0ul;
                        ulong.TryParse(split[i], out id);
                        var dUser = await Program.client.GetUserAsync(id);
                        if(dUser != null) targetList.Add(dUser.Id);
                    }


                    if(targetList.Count > 0)
                    {
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Characters.Active.ContainsKey(targetList[i]))
                            {
                                sb.AppendLine(Characters.Active[targetList[i]].CharacterName);
                                Characters.Active[targetList[i]].ClearBonus(modToUpper);
                                await collection.ReplaceOneAsync(x => x.Id == Characters.Active[targetList[i]].Id, Characters.Active[targetList[i]]);
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

            var sb = new StringBuilder();
            if(lastTargets[user] != null)
            {
                for(int i = 0; i < lastTargets[user].Count; i++)
                {
                    if(Characters.Active.ContainsKey(lastTargets[user][i]))
                    {
                        sb.AppendLine(Characters.Active[lastTargets[user][i]].CharacterName);
                        Characters.Active[lastTargets[user][i]].AddBonuses(StatModifier.Mods[modName]);
                        await collection.ReplaceOneAsync(x => x.Id == Characters.Active[lastTargets[user][i]].Id, Characters.Active[user]);
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
        }




        [SlashCommand("bonus", "Apply or remove bonuses to a particular stat.")]
        public async Task BonusCommand(string statName, string bonusName, int bonusValue, BonusType bonusType = BonusType.Typeless, string targets = "")
        {                  
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var statToUpper = statName.ToUpper();
            var bonusToUpper = bonusName.ToUpper();
            if(!ValidVar.IsMatch(statToUpper) || !ValidVar.IsMatch(bonusToUpper))
            {
                await RespondAsync("Invalid stat or bonus name. A-Z and underscores only. Values will be automatically capitalized.", ephemeral: true);
                return;
            }

            
                      
            if(targets != "")
            {
                var targetList = new List<ulong>();
                var split = targets.Trim( new char[] {'<', '>', '!', '@'}).Split(' ');
                Console.WriteLine(split.Length);

                for(int i = 0; i < split.Length; i++)
                {
                    var id = 0ul;
                    ulong.TryParse(split[i], out id);
                    var dUser = await Program.client.GetUserAsync(id);
                    if(dUser != null) targetList.Add(dUser.Id);
                }
                
                if(targetList.Count > 0)
                {                   
                    for(int i = 0; i < targetList.Count; i++)
                    {
                        if(Characters.Active.ContainsKey(targetList[i]))
                        {
                            if(Characters.Active[targetList[i]].Stats.ContainsKey(statToUpper))
                            {
                                Characters.Active[targetList[i]].Stats[statToUpper] = 0;
                            }

                            Characters.Active[targetList[i]].Stats[statToUpper].AddBonus(new Bonus() { Name = bonusName, Value = bonusValue, Type = bonusType });

                            var updateTarget = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Characters.Active[targetList[i]].Stats[statToUpper]);                           
                            await collection.UpdateOneAsync(x => x.Id == Characters.Active[targetList[i]].Id, updateTarget);
                            await RespondAsync($"Updated {Characters.Active[targetList[i]].CharacterName}!");
                        }
                    }
                }
                await RespondAsync("No targets found :(");
                return;
            }

            if(!Characters.Active[user].Stats.ContainsKey(statToUpper))
            {
                Characters.Active[user].Stats[statToUpper] = 0;
            }

            Characters.Active[user].Stats[statToUpper].AddBonus(new Bonus() { Name = bonusName, Value = bonusValue, Type = bonusType });

            var update = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Characters.Active[user].Stats[statToUpper]);
            await collection.UpdateOneAsync(x => x.Id == Characters.Active[user].Id, update);
            await RespondAsync($"Updated {Characters.Active[user].CharacterName}!", ephemeral: true);

            return;
        }
    }
}
