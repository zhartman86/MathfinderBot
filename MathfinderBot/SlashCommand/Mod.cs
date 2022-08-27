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
        public enum RemovalType
        {
            OneStat,
            AllStats
        }

        public enum TemplateMode
        {
            Add,
            Remove,
            List,
            ListAll
        }
        
        private static Regex ValidVar = new Regex("^[A-Z_]{1,17}$");
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

   
        [SlashCommand("buff", "Apply a specifically defined modifier to one or many targets")]
        public async Task BuffCommand(string buffName, string targets = "")
        {
            var user = Context.Interaction.User.Id;
            

            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var buffToUpper = buffName.ToUpper();

           


            if(StatModifier.Buffs.ContainsKey(buffToUpper))
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
                        var characters = "";
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Characters.Active.ContainsKey(targetList[i]))
                            {
                                characters += Characters.Active[targetList[i]].CharacterName + "\n";
                                Characters.Active[targetList[i]].AddBonuses(StatModifier.Buffs[buffToUpper]);
                                await collection.ReplaceOneAsync(x => x.Id == Characters.Active[targetList[i]].Id, Characters.Active[targetList[i]]);
                            }
                        }

                        var eb = new EmbedBuilder()
                            .WithTitle(buffToUpper)
                            .WithDescription(characters);
                        
                        foreach(var bonus in StatModifier.Buffs[buffToUpper])
                        {
                            eb.AddField(name: bonus.StatName, value: $"{bonus.Bonus.Value} {Enum.GetName(bonus.Bonus.Type)} bonus");
                        }

                        await RespondAsync(embed: eb.Build());
                        return;
                    }
                }
                                                                 
                Characters.Active[user].AddBonuses(StatModifier.Buffs[buffToUpper]);

                await collection.ReplaceOneAsync(x => x.Id == Characters.Active[user].Id, Characters.Active[user]);
                await RespondAsync($"Updated {Characters.Active[user].CharacterName}!");
            }
            
        }
        [SlashCommand("bonus-remove", "remove bonuses by name")]
        public async Task BonusRemoveCommand(RemovalType type, string bonusName, string statName = "", string targets = "")
        {   
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var statToUpper = statName.ToUpper();
            var bonusToUpper = bonusName.ToUpper();           

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
                    if(type == RemovalType.AllStats)
                    {
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Characters.Active.ContainsKey(targetList[i]))
                            {
                                foreach(var stat in Characters.Active[targetList[i]].Stats.Values)
                                {
                                    stat.RemoveBonus(bonusToUpper);
                                }
                                
                            }
                            var update = Builders<StatBlock>.Update.Set(x => x.Stats, Characters.Active[targetList[i]].Stats);
                            await collection.UpdateOneAsync(x => x.Id == Characters.Active[targetList[i]].Id, update);
                            await RespondAsync("Stats updated.");
                        }
                    }                   

                    if(type == RemovalType.OneStat)
                    {
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Characters.Active.ContainsKey(targetList[i]))
                            {
                                if(!Characters.Active[targetList[i]].Stats.ContainsKey(statToUpper)) continue;
                                Characters.Active[targetList[i]].Stats[statToUpper].RemoveBonus(bonusName);
                            }
                            var update = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Characters.Active[targetList[i]].Stats[statToUpper]);
                            await collection.UpdateOneAsync(x => x.Id == Characters.Active[targetList[i]].Id, update);
                            await RespondAsync("Stats updated.");
                        }                      
                    }                                    
                }
            } 
            
            if(type == RemovalType.AllStats)
            {
                foreach(var stat in Characters.Active[user].Stats.Values)
                {
                    stat.RemoveBonus(bonusToUpper);
                }

                var update = Builders<StatBlock>.Update.Set(x => x.Stats, Characters.Active[user].Stats);
                await collection.UpdateOneAsync(x => x.Id == Characters.Active[user].Id, update);

                await RespondAsync("Stats updated.");
            }
            
            if(type == RemovalType.OneStat)
            {
                if(!Characters.Active[user].Stats.ContainsKey(statToUpper))
                {
                    await RespondAsync("Stat name not found.");
                    return;
                }

                Characters.Active[user].Stats[statToUpper].RemoveBonus(bonusName);
                var update = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Characters.Active[user].Stats[statToUpper]);
                await collection.UpdateOneAsync(x => x.Id == Characters.Active[user].Id, update);

                await RespondAsync("Stat updated.");
            }
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
