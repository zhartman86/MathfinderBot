using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Expressions;
using MongoDB.Driver;
using System.Text;
using System.Text.RegularExpressions;
using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public class Evaluate : InteractionModuleBase
    {                
        ulong user;
        StatBlock stats;

        public override async void BeforeExecute(ICommandInfo command)
        {          
            user = Context.User.Id;            
        }


        [SlashCommand("eval", "Evaluate and modify stats and expressions")]
        public async Task EvalCommand(string expr, bool isHidden = false)
        {
            var sb = new StringBuilder();
            stats = await Characters.GetCharacter(user);
            ExpressionNode node = Parser.Parse(expr, stats, sb);

            Console.WriteLine("Getting result");

            var result = node.Eval(stats, sb);

            if(!object.ReferenceEquals(null, result))
            {
                while(true)
                {
                    if(result is IEval e)
                    {
                        result = e.Eval(stats, sb);
                        continue;
                    }
                    break;
                }
                
            }

            var resultField = sb.Length > 0 ? sb.ToString() : @"~";

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            Console.WriteLine(result.ToString());
            Console.WriteLine(stats.CharacterName);
            Console.WriteLine(resultField);

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle(stats.CharacterName)
                .WithDescription($"{result}")
                .WithFooter($"{expr}");

                builder.AddField($"-<>-", resultField, inline: true);

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
