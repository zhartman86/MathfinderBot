using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public class Mod : InteractionModuleBase
    {
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

            if(Buff.Buffs.ContainsKey(buffName))
            {
                Pathfinder.Active[user].AddBuff(Buff.Buffs[buffName]);
                await RespondAsync("Added buff.", ephemeral: true);
            }
            
        }
        
        
        [SlashCommand("bonus", "Apply or remove bonuses to a particular stat.")]
        public async Task BonusCommand(string statName, string bonusName, int bonusValue, BonusType bonusType = BonusType.Typeless, string targets = "")
        {
            
            var user = Context.Interaction.User;
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
                                await ReplyAsync($"Stat name not found for {targetList[i].Username}, will create!");
                            }

                            Pathfinder.Active[targetList[i]].Stats[statToUpper].Bonuses.Add(new Bonus() { Name = bonusName, Value = bonusValue, Type = bonusType });
                            
                        }
                    }
                }
                return;
            }

            if(!Pathfinder.Active[user].Stats.ContainsKey(statToUpper))
            {
                await ReplyAsync("Stat name not found, will create!");
            }


            Pathfinder.Active[user].Stats[statToUpper].Bonuses.Add(new Bonus() { Name = bonusName, Value = bonusValue, Type = bonusType });
            
            foreach(var bonuses in Pathfinder.Active[user].Stats[statToUpper].Bonuses.bonuses)
            {
                foreach(var bonus in bonuses.Value)
                {
                    Console.Write(bonus.Name + " " + bonus.Value + " " + bonusType.ToString());
                }
            }

            Console.WriteLine(Pathfinder.Active[user].Stats[statToUpper]);

            await RespondAsync("Stat Applied", ephemeral: true);
            return;
        }
    }
}
