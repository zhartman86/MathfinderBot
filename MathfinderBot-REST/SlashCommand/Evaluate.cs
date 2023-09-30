using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Expressions;
using MongoDB.Driver;
using System.Text;

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


        [SlashCommand("eval", "Evaluate stats and expressions, modify bonuses")]
        public async Task EvalCommand(string expr, bool isHidden = false, string targets = "")
        {                     
            var sbs = new List<StringBuilder>();
            var description = "";
            string result = "";

            var exprs = expr.Split(';', StringSplitOptions.RemoveEmptyEntries);

            if(targets != "" && Context.Interaction.User is SocketGuildUser gUser)
            {               
                if(gUser.Roles.Any(x => x.Name == "DM"))
                {
                    description = "";
                    var targetList = await Utility.ParseTargets(targets);                        
                    if(targetList.Count > 0)
                        for(int i = 0; i < targetList.Count; i++)
                            if(Characters.Active.ContainsKey(targetList[i].Id))
                            {
                                var sb = new StringBuilder();
                                var parse = Parser.Parse(expr);
                                result = parse.Eval(Characters.Active[targetList[i].Id], sb).ToString();
                                sb.AppendLine($"{targetList[i].Mention}");                               
                                sb.AppendLine("—");
                                sb.AppendLine($"**{result}**");
                                sb.AppendLine();
                                sbs.Add(sb);
                            }
                    else
                    {
                        await RespondAsync("No appropriate targets found", ephemeral: true);
                        return;
                    }                                                 
                }    
                else
                {
                    await RespondAsync("You require special permissions to select other plays", ephemeral: true);
                    return;
                }                
            }
            else
            {
                var stats = await Characters.GetCharacter(user);
                description = stats.CharacterName != "$GLOBAL" ? stats.CharacterName : "";             
                var sb = new StringBuilder();
                for(int i = 0; i < exprs.Length; i++)
                {                    
                    if(i > 0 && i < exprs.Length)
                        sb.AppendLine("-:-");
                    var node = Parser.Parse(exprs[i]);
                    result += $"{node.Eval(stats, sb)};";
                }
                result = result.Trim(';');
                sbs.Add(sb);
            }

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var title = sbs.Count > 1 ? "Multi-Target" : result.ToString();

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle(title)
                .WithDescription(description)
                .WithFooter($"{expr}");

            for(int i = 0; i < sbs.Count; i++)
                if(sbs[i].Length > 0)
                    builder.AddField($"__Events__", $"{sbs[i]}", inline: true);

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

        [ComponentInteraction("req:*")]
        public async Task RequestAccept(string expr)
        {
            var sb = new StringBuilder();
            var node = Parser.Parse(expr);
            var result = node.Eval(await Characters.GetCharacter(user), sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Purple)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription(Characters.Active[user].CharacterName != "$GLOBAL" ? Characters.Active[user].CharacterName : "")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"__Events__", $"{sb}");

            await RespondAsync(embed: builder.Build());
        }   
    }
}
