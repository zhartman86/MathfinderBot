using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using MongoDB.Driver;
using Discord;

namespace MathfinderBot
{
    public class Variable : InteractionModuleBase
    {
        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,

            [ChoiceDisplay("Set-Attack")]
            SetAttack,
            
            [ChoiceDisplay("List-Stat")]
            ListStats,

            [ChoiceDisplay("List-Expression")]
            ListExpr,

            [ChoiceDisplay("List-Attack")]
            ListAttacks,

            [ChoiceDisplay("Remove-Variable")]
            Remove
        }
        
        
        static Regex ValidVar = new Regex("^[A-Z_$]{1,17}$");
        CommandHandler handler;
        ulong user;
        IMongoCollection<StatBlock> collection;


        public Variable(CommandHandler handler) => this.handler = handler;

        public async override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;     

            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }
        }


        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "", string value = "")
        {
            collection = Program.database.GetCollection<StatBlock>("statblocks");

            if(action == VarAction.ListStats)
            {
                var builder = new StringBuilder();

                foreach(var stat in Pathfinder.Active[user].Stats)
                {
                    builder.AppendLine(stat.Key + ":" + ((int)stat.Value).ToString());
                }             
                                          
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
                await RespondWithFileAsync(stream, $"Stats.{Pathfinder.Active[user].CharacterName}.txt", ephemeral: true);                    
            }

            if(action == VarAction.ListExpr)
            {
                var builder = new StringBuilder();

                foreach(var expr in Pathfinder.Active[user].Expressions)
                {
                    builder.AppendLine(expr.Key + ":" + expr.Value.ToString());
                }                             

                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
                await RespondWithFileAsync(stream, $"Expressions.{Pathfinder.Active[user].CharacterName}.txt", ephemeral: true);
            }

            
            if(action == VarAction.ListAttacks)
            {
                var eb = new EmbedBuilder();
                if(varName != "")
                {
                    var toUpper = varName.ToUpper();
                    if(Pathfinder.Active[user].Attacks.ContainsKey(toUpper))
                    {
                        eb = new EmbedBuilder()
                            .WithColor(Color.DarkGreen)
                            .WithTitle($"List-Attack({toUpper})")
                            .WithDescription(Pathfinder.Active[user].Attacks[toUpper].ToString());
                        
                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }

                var sb = new StringBuilder();
                foreach(var attack in Pathfinder.Active[user].Attacks.Keys)
                {
                    sb.AppendLine(attack);
                }

                eb = new EmbedBuilder()
                        .WithColor(Color.DarkGreen)
                        .WithTitle($"List-Attacks()")
                        .WithDescription($"```{sb.ToString()}```");

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
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Stats.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Stats, Pathfinder.Active[user].Stats);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync($"`{varToUpper}` removed from stats.", ephemeral: true);
                    return;
                }
                else if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Expressions.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Expressions, Pathfinder.Active[user].Expressions);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync($"`{varToUpper}` removed from expressions.", ephemeral: true);
                    return;
                }
                else if(Pathfinder.Active[user].Attacks.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Attacks.Remove(varToUpper);
                    
                    var update = Builders<StatBlock>.Update.Set(x => x.Attacks, Pathfinder.Active[user].Attacks);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync($"`{varToUpper}` removed from attacks.", ephemeral: true);
                    return;

                }


                await RespondAsync($"No variable `{varToUpper}` found.", ephemeral: true);
                return;
            }          
            
            if(action == VarAction.SetExpr)
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    await RespondAsync($"`{varToUpper}` already exists as a stat.", ephemeral: true);
                    return;
                }

                Pathfinder.Active[user].Expressions[varToUpper] = value;

                var update = Builders<StatBlock>.Update.Set(x => x.Expressions[varToUpper], Pathfinder.Active[user].Expressions[varToUpper]);                                     
                await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                await RespondAsync($"Updated expression:`{varToUpper}`", ephemeral: true);
            }                                 
        
            
        }

        
        [SlashCommand("atk", "Set or modify attacks.")]
        public async Task AttackNewCommand(string atkName, string toHitExpr, string damageExpr, string critExpr, bool confirmCrit = true, int critRange = 20, int diceSides = 20)
        {
            var toUpper = atkName.ToUpper();
            if(!ValidVar.IsMatch(toUpper))
            {
                await RespondAsync($"Invalid variable `{toUpper}`. A-Z and underscores only. Values will be automatically capitalized.");
                return;
            }

            var attack = new Attack()
            {
                AttackName = toUpper,
                ToHitExpr = toHitExpr,
                DamageExpr = damageExpr,
                CritExpr = critExpr,
                Confirm = confirmCrit,
                CritRange = critRange,
                Sides = diceSides
            };

            collection = Program.database.GetCollection<StatBlock>("statblocks");

            var withMoney = $"${toUpper}";
            Pathfinder.Active[user].Attacks[withMoney] = attack;
            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle("ATTACK")
                .WithDescription(attack.ToString());

            var update = Builders<StatBlock>.Update.Set(x => x.Attacks[withMoney], Pathfinder.Active[user].Attacks[withMoney]);
            await collection.FindOneAndUpdateAsync(x => x.Id == Pathfinder.Active[user].Id, update);
            await RespondAsync(embed: eb.Build());
        }
    }
}
