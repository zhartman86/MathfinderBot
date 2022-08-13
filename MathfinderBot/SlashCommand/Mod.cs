using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using MongoDB.Driver;
using MongoDB;

namespace MathfinderBot
{
    public class Mod : InteractionModuleBase
    {
        public enum RemovalType
        {
            OneStat,
            AllStats
        }
        
        
        private static Regex ValidVar = new Regex("^[A-Z_]{1,17}$");

        [SlashCommand("buff", "Apply a specifically defined modifier to one or many targets")]
        public async Task BuffCommand(string buffName, string targets = "")
        {
            var user = Context.Interaction.User;
            
            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var buffToUpper = buffName.ToUpper();

            if(!ValidVar.IsMatch(buffToUpper))
            {
                await RespondAsync("Invalid stat or bonus name. A-Z and underscores only. Values will be automatically capitalized.", ephemeral: true);
                return;
            }

            if(Buff.Buffs.ContainsKey(buffToUpper))
            {
                Pathfinder.Active[user].AddBuff(Buff.Buffs[buffToUpper]);

                var collection = Program.database.GetCollection<StatBlock>("statblocks");
                await collection.ReplaceOneAsync(x => x.Id == Pathfinder.Active[user].Id, Pathfinder.Active[user]);
                await RespondAsync($"Updated {Pathfinder.Active[user].CharacterName}!");
            }
            
        }
        [SlashCommand("bonus-remove", "remove bonuses by name")]
        public async Task BonusRemoveCommand(RemovalType type, string bonusName, string statName = "", string targets = "")
        {
            var user = Context.Interaction.User;
            var collection = Program.database.GetCollection<StatBlock>("statblocks");

            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null)
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
                var targetList = new List<IUser>();
                var split = targets.Trim(new char[] { '<', '>', '!', '@' }).Split(' ');
                Console.WriteLine(split.Length);

                for(int i = 0; i < split.Length; i++)
                {
                    var id = 0ul;
                    ulong.TryParse(split[i], out id);
                    var dUser = await Program.client.GetUserAsync(id);
                    if(dUser != null) targetList.Add(dUser);
                }

                if(targetList.Count > 0)
                {
                    if(type == RemovalType.AllStats)
                    {
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Pathfinder.Active.ContainsKey(targetList[i]))
                            {
                                foreach(var stat in Pathfinder.Active[targetList[i]].Stats.Values)
                                {
                                    stat.RemoveBonus(bonusToUpper);
                                }
                                
                            }
                            var update = Builders<StatBlock>.Update.Set(x => x.Stats, Pathfinder.Active[targetList[i]].Stats);
                            await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[targetList[i]].Id, update);
                            await RespondAsync("Stats updated.");
                        }
                    }                   

                    if(type == RemovalType.OneStat)
                    {
                        for(int i = 0; i < targetList.Count; i++)
                        {
                            if(Pathfinder.Active.ContainsKey(targetList[i]))
                            {
                                if(!Pathfinder.Active[targetList[i]].Stats.ContainsKey(statToUpper)) continue;
                                Pathfinder.Active[targetList[i]].Stats[statToUpper].RemoveBonus(bonusName);
                            }
                            var update = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Pathfinder.Active[targetList[i]].Stats[statToUpper]);
                            await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[targetList[i]].Id, update);
                            await RespondAsync("Stats updated.");
                        }                      
                    }                                    
                }
            } 
            
            if(type == RemovalType.AllStats)
            {
                foreach(var stat in Pathfinder.Active[user].Stats.Values)
                {
                    stat.RemoveBonus(bonusToUpper);
                }

                var update = Builders<StatBlock>.Update.Set(x => x.Stats, Pathfinder.Active[user].Stats);
                await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);

                await RespondAsync("Stats updated.");
            }
            
            if(type == RemovalType.OneStat)
            {
                if(!Pathfinder.Active[user].Stats.ContainsKey(statToUpper))
                {
                    await RespondAsync("Stat name not found.");
                    return;
                }

                Pathfinder.Active[user].Stats[statToUpper].RemoveBonus(bonusName);
                var update = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Pathfinder.Active[user].Stats[statToUpper]);
                await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);

                await RespondAsync("Stat updated.");
            }
        }
        
        
        [SlashCommand("bonus", "Apply or remove bonuses to a particular stat.")]
        public async Task BonusCommand(string statName, string bonusName, int bonusValue, BonusType bonusType = BonusType.Typeless, string targets = "")
        {
            
            var user = Context.Interaction.User;
            var collection = Program.database.GetCollection<StatBlock>("statblocks");

            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null)
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
                var targetList = new List<IUser>();
                var split = targets.Trim( new char[] {'<', '>', '!', '@'}).Split(' ');
                Console.WriteLine(split.Length);

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
                        if(Pathfinder.Active.ContainsKey(targetList[i]))
                        {
                            if(Pathfinder.Active[targetList[i]].Stats.ContainsKey(statToUpper))
                            {
                                Pathfinder.Active[targetList[i]].Stats[statToUpper] = 0;
                            }

                            Pathfinder.Active[targetList[i]].Stats[statToUpper].AddBonus(new Bonus() { Name = bonusName, Value = bonusValue, Type = bonusType });

                            var updateTarget = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Pathfinder.Active[targetList[i]].Stats[statToUpper]);                           
                            await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[targetList[i]].Id, updateTarget);
                            await RespondAsync($"Updated {Pathfinder.Active[targetList[i]].CharacterName}!");
                        }
                    }
                }
                await RespondAsync("No targets found :(");
                return;
            }

            if(!Pathfinder.Active[user].Stats.ContainsKey(statToUpper))
            {
                Pathfinder.Active[user].Stats[statToUpper] = 0;
            }

            Pathfinder.Active[user].Stats[statToUpper].AddBonus(new Bonus() { Name = bonusName, Value = bonusValue, Type = bonusType });

            var update = Builders<StatBlock>.Update.Set(x => x.Stats[statToUpper], Pathfinder.Active[user].Stats[statToUpper]);
            await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
            await RespondAsync($"Updated {Pathfinder.Active[user].CharacterName}!", ephemeral: true);

            return;
        }
    }
}
