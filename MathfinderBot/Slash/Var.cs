using System.Text.RegularExpressions;
using Discord.Interactions;

namespace MathfinderBot.Slash
{
    public class Variable : InteractionModuleBase
    {
        public enum VarAction
        {
            SetExpr,
            SetStat,
            Remove
        }
        
        public InteractionService Service { get; set; }

        private static Regex ValidVar = new Regex("^[A-Z_]{1,17}$");

        private CommandHandler handler;

        public Variable(CommandHandler handler) => this.handler = handler;

        [SlashCommand("var", "Create, modify, remove a stat or expression.")]
        public async Task Var(VarAction action, string varName, string value)
        {
            var user = Context.Interaction.User;
            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null) 
            { 
                await RespondAsync("No active character", ephemeral: true); 
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
                    await RespondAsync("Value removed from stats.");
                    return;
                }
                else if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Expressions.Remove(varToUpper);
                    await RespondAsync("Value removed from expressions.");
                    return;
                }

                await RespondAsync("No variable by that name found.");
                return;
            }        
            
            if(action == VarAction.SetExpr)
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    await RespondAsync("Value exists as a stat.");
                    return;
                }
                
                Pathfinder.Active[user].Expressions[varToUpper] = value;
            }
            if(action == VarAction.SetStat)
            {
                if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    await RespondAsync("Value exists as an expression.");
                    return;
                }
                    
                int val = 0;
                Pathfinder.Active[user].Stats[varToUpper] = int.TryParse(value, out val) == true ? val : 0;
            }

            await RespondAsync("Value updated.");

            
        }
    }
}
