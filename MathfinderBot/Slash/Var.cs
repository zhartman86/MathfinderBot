using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;

namespace MathfinderBot.Slash
{
    public class Variable : InteractionModuleBase
    {
        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,
            
            [ChoiceDisplay("Set-Stat")]
            SetStat,

            [ChoiceDisplay("List")]
            List,          
           
            [ChoiceDisplay("Remove")]
            Remove
        }
        
        public InteractionService Service { get; set; }

        private static Regex ValidVar = new Regex("^[A-Z_]{1,17}$");

        private CommandHandler handler;

        public Variable(CommandHandler handler) => this.handler = handler;

        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "", string value = "")
        {
            var user = Context.Interaction.User;
            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null) 
            { 
                await RespondAsync("No active character", ephemeral: true); 
                return; 
            }

            

            

            if(action == VarAction.List)
            {
                var builder = new StringBuilder();

                foreach(var stat in Pathfinder.Active[user].Stats)
                {
                    builder.AppendLine(stat.Key + ":" + ((int)stat.Value).ToString());
                }
                foreach(var expr in Pathfinder.Active[user].Expressions)
                {
                    builder.AppendLine(expr.Key + ":" + expr.Value.ToString());
                }
                Console.WriteLine(builder.ToString()); 
                await RespondAsync(builder.ToString(), ephemeral: true);
                
                return;
            }

            var varToUpper = varName.ToUpper();
            if(!ValidVar.IsMatch(varToUpper))
            {
                await RespondAsync("Invalid variable name. A-Z and underscores only. Values will be automatically capitalized.");
                return;
            }
        
            
            if(action == VarAction.Remove)
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Stats.Remove(varToUpper);
                    await RespondAsync("Value removed from stats.", ephemeral: true);
                    return;
                }
                else if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Expressions.Remove(varToUpper);
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
            }

            await RespondAsync("Value updated.", ephemeral: true);

            
        }
    }
}
