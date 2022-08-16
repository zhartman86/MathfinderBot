using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using MongoDB.Driver;

namespace MathfinderBot
{
    public class Variable : InteractionModuleBase
    {
        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,
            
            [ChoiceDisplay("Set-Stat")]
            SetStat,

            [ChoiceDisplay("List-Stats")]
            ListStats,

            [ChoiceDisplay("List-Expressions")]
            ListExpr,
           
            [ChoiceDisplay("Remove-Variable")]
            Remove
        }
        
        private static Regex ValidVar = new Regex("^[A-Z_]{1,17}$");

        private CommandHandler handler;

        public Variable(CommandHandler handler) => this.handler = handler;

        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "", string value = "")
        {
            var user = Context.Interaction.User.Id;
            var collection = Program.database.GetCollection<StatBlock>("statblocks");

            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null) 
            { 
                await RespondAsync("No active character", ephemeral: true); 
                return; 
            }

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

            var varToUpper = varName.ToUpper();
            if(!ValidVar.IsMatch(varToUpper))
            {
                await RespondAsync("Invalid variable name. A-Z and underscores only. Values will be automatically capitalized.", ephemeral: true);
                return;
            }
        
            
            if(action == VarAction.Remove)
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Stats.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Stats, Pathfinder.Active[user].Stats);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync("Value removed from stats.", ephemeral: true);
                    return;
                }
                else if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Expressions.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Expressions, Pathfinder.Active[user].Expressions);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync("Value removed from expressions.", ephemeral: true);
                    return;
                }

                await RespondAsync("No variable by that name found.", ephemeral: true);
                return;
            }

            
            
            if(action == VarAction.SetExpr)
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    await RespondAsync("Value already exists as a stat.", ephemeral: true);
                    return;
                }

                Pathfinder.Active[user].Expressions[varToUpper] = value;

                var update = Builders<StatBlock>.Update.Set(x => x.Expressions[varToUpper], Pathfinder.Active[user].Expressions[varToUpper]);                                     
                await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                await RespondAsync("Updated expression", ephemeral: true);
            }
            if(action == VarAction.SetStat)
            {
                if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    await RespondAsync("Value already exists as an expression.", ephemeral: true);
                    return;
                }


                int val = 0;
                Pathfinder.Active[user].Stats[varToUpper] = int.TryParse(value, out val) == true ? val : 0;

                var update = Builders<StatBlock>.Update.Set(x => x.Stats[varToUpper], Pathfinder.Active[user].Stats[varToUpper]);
                await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                await RespondAsync("Updated stat", ephemeral: true);
            }            
        }
    }
}
