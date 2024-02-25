using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Expressions;
using MongoDB.Driver;
using System.Text;
using System.Text.RegularExpressions;

namespace MathfinderBot
{
    public class Evaluate : InteractionModuleBase
    {                
        ulong user;

        public override async void BeforeExecute(ICommandInfo command)
        {          
            user = Context.User.Id;
            await Characters.GetCharacter(user);
        }


        [SlashCommand("eval", "Evaluate and modify stats and expressions")]
        public async Task EvalCommand(string expr, bool isHidden = false)
        {
            var sb = new StringBuilder();
            var stats = await Characters.GetCharacter(user);
            
            string description;
            int result;

           
            description = stats.CharacterName != "$GLOBAL" ? stats.CharacterName : "";             
                        
            ExpressionNode node = Parser.Parse(expr, stats, sb);
            result = node.Eval();

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var title = result.ToString();

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle(title)
                .WithDescription(description)
                .WithFooter($"{expr}");

                if(sb.Length > 0) builder.AddField($"-<>-", $"{sb}", inline: true);

            await RespondAsync(embed: builder.Build(), ephemeral: isHidden);
        }

        [SlashCommand("req", "Calls for an evaluation")]
        public async Task RequestCommand(string expr)
        {
            var message = $"{Context.Interaction.User.Mention} has made a request: {expr}";

            var cb = new ComponentBuilder()
                .WithButton(customId: $"req:{expr.Replace(" ", "")}", label: "Accept");

            await RespondAsync(message, components: cb.Build());
        }
    }
}
