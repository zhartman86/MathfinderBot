using Discord.Interactions;
using Discord;
using Gellybeans.Expressions;
using System.Text;
using Gellybeans.Pathfinder;
using System.Runtime.InteropServices.JavaScript;

namespace MathfinderBot
{
    public class Evaluate : InteractionModuleBase
    {                
        ulong user;
        StatBlock stats;

        static Dictionary<ulong, List<(Guid, Dictionary<string, dynamic>)>> history = new Dictionary<ulong, List<(Guid, Dictionary<string, dynamic>)>>();

        public override async void BeforeExecute(ICommandInfo command)
        {          
            user = Context.User.Id;            
        }


        async Task<dynamic> Eval(string expr, StringBuilder sb, IContext ctx)
        {           
            int depth = 0;

            ExpressionNode node = Parser.Parse(expr, this, sb, ctx);
            Console.WriteLine($"EVALLING {expr}, TYPE: {node.GetType()}");
            
            
            
            var result = node.Eval(depth, this, sb, ctx);
            Console.WriteLine("DONE");

            if(!object.ReferenceEquals(null, result))
            {
                if(result is EventValue e)
                {
                    await HandleEvent(e, ctx);
                    return -99999;
                }
                if(result is IReduce r)
                    result = r.Reduce(depth, this, sb, ctx);
                if(result is IDisplay d)
                    result = d.Display(depth, this, sb, ctx);
                
            }
            return result;
        }

        [SlashCommand("undo", "Undo changes made using /eval.")]
        public async Task UndoCommand(int stepCount = 1)
        {           
            if(stepCount > 20 || stepCount < 1)
            {
                await RespondAsync("Invalid input. (ranges 1-20 allowed)", ephemeral: true);
                return;
            }

            stats = await Characters.GetCharacter(user);
            if(history.TryGetValue(user, out var h))
            {
                if(h.Count >= h.Count - stepCount) 
                {
                    if(h[^stepCount].Item1 != stats.Id)
                    {
                        await RespondAsync("history doesn't match current character.");
                        return;
                    }
                    stats.SetVars(h[^stepCount].Item2);
                    await RespondAsync($"Undo successful. ({stepCount} step.)");                       
                }

            }

            await RespondAsync("No history found.", ephemeral: true);
        }

        [SlashCommand("eval", "Evaluate and modify stats and expressions")]
        public async Task EvalCommand(string expr, bool isHidden = false)
        {
            var sb = new StringBuilder();
            stats = await Characters.GetCharacter(user);

            if(history.TryGetValue(user, out var h))
            {
                if(h.Count > 30)
                    h.RemoveRange(0, 5);

                h.Add((stats.Id, new Dictionary<string, dynamic>(stats.Vars)));
            }
            else
            {
                history.Add(user, new List<(Guid, Dictionary<string, dynamic>)> { (stats.Id, new Dictionary<string, dynamic>(stats.Vars)) });
            }

            //check channel context
            if(DataMap.channelVars.TryGetValue(Context.Channel.Id, out var scope))
            {
                if(stats.Global != null && stats.Global is StatBlock s)
                    s.Global = scope;
                
                else stats.Global = scope;
            }


            var result = await Eval(expr, sb, stats);
            if(result is int i && i == -99999)
            {
                await DeferAsync();
                return;
            }

            var len = result.ToString().Length;
            Console.WriteLine(len);

            if(len > 4000)
            {
                var bytes = Encoding.ASCII.GetBytes(result.ToString());
                using var stream = new MemoryStream(bytes);
                await RespondWithFileAsync(stream, $"eval.txt", ephemeral: isHidden);
                return;
            }

            stats.Vars["__"] = result;

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle(stats.CharacterName == "$GLOBAL" ? "~" : stats.CharacterName)
                .WithDescription($"{result}")
                .WithFooter($"{expr}");

                if(sb.Length > 0) 
                    builder.AddField($"-<>-", sb.ToString(), inline: true);
          

            await RespondAsync(embed: builder.Build(), ephemeral: isHidden);
        }

        public async Task HandleEvent(EventValue e, IContext ctx)
        {
            Console.WriteLine("HANDLING EVENT...");

            var msg = "";
            var cb = new ComponentBuilder();

            if(e.Data["MESSAGE"] != "%")
            {
                var v = e.Data["MESSAGE"];
                if(v is IDisplay d)
                    v = d.Display(0, this, null!, ctx);
                msg = v.ToString();
            }

            if(e.Data["COMPONENTS"] is ArrayValue ca)
            {
                for(int i = 0; i < ca.Values.Length; i++)
                {
                    if(ca[i] is ArrayValue component)
                    {
                        if(component["TYPE"] == "BUTTON")
                        {
                            if(component["DATA"] is ArrayValue bData)
                            {
                                string label = bData["LABEL"];

                                var style = ButtonStyle.Primary;                                
                                if(int.TryParse(bData["STYLE"].ToString(), out int styleInt))
                                    style = (ButtonStyle)styleInt;

                                var value = bData["VALUE"];
                                if(value is StringValue s)
                                    value = $"\"{s}\"";
                                else if(value is ExpressionValue ex)
                                    value = $"`{ex}`";

                                cb.WithButton(label: $"{label}", customId: $"e:{value}", style: style);
                            }
                        }
                    }
                }
            }                                 
            await RespondAsync(text: msg, components: cb.Build());          
        }

        //shortened to 'e' to eek out as many chars as i can with expressions (custom ids have a 100 char limit)
        [ComponentInteraction("e:*")]
        public async Task ButtonPressedExpression(string expr)
        {
            Console.WriteLine($"EVALLING {expr}");
            var stats = await Characters.GetCharacter(user);
            var sb = new StringBuilder();
            var result = await Eval(expr, sb, stats);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle(stats.CharacterName == "$GLOBAL" ? "~" : stats.CharacterName)
                .WithDescription($"{result}")
                .WithFooter($"{expr}");

            if(sb.Length > 0)
                builder.AddField($"-<>-", sb.ToString(), inline: true);

            await RespondAsync(embed: builder.Build());
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
