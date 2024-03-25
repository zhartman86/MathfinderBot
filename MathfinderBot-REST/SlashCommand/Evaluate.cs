using Discord.Interactions;
using Discord;
using Gellybeans.Expressions;
using System.Text;
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


        [SlashCommand("eval", "Evaluate and modify stats and expressions")]
        public async Task EvalCommand(string expr, bool isHidden = false)
        {
            var sb = new StringBuilder();
            stats = await Characters.GetCharacter(user);

            var result = await Eval(expr, sb, stats);
            if(result is int i && i == -99999)
            {
                await DeferAsync();
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
            var cb = new ComponentBuilder();
            string msg = "";

            for(int i = 0; i < e.Data.Length; i++)
            {
                Console.WriteLine($"Checking {i}...");
                switch(e.Data[i])
                {
                    case KeyValuePairValue kvMessage when kvMessage.Key == "MESSAGE":
                        Console.WriteLine("Getting Msg..");
                        var v = kvMessage.Value;
                        if(v is IReduce r)
                            v = r.Reduce(0, this, null!, ctx);
                        if(v is IDisplay d)
                            v = d.Display(0, this, null!, ctx);
                        msg = v.ToString();
                        break;
                    case KeyValuePairValue kvComponents when kvComponents.Key == "COMPONENTS" && kvComponents.Value is ArrayValue compArray:
                        Console.WriteLine("Getting Component data..");

                        for(int c = 0; c < compArray.Values.Length; c++)
                        {
                            if(compArray[c][0] is KeyValuePairValue kvCompType && compArray[c][1] is KeyValuePairValue kvCompData)
                            {
                                Console.WriteLine("Parsing Component data..");
                                if(kvCompType.Value == "BUTTON")
                                {
                                    if(kvCompData.Value is ArrayValue buttonData)
                                    {
                                        dynamic label = "LABEL ME";
                                        ButtonStyle style = ButtonStyle.Primary;
                                        dynamic value = label;

                                        Console.WriteLine("GETTING BUTTONS");
                                        for(int b = 0; b < buttonData.Values.Length; b++)
                                        {
                                            if(buttonData[b] is KeyValuePairValue bLabel && bLabel.Key == "LABEL")
                                            {
                                                label = bLabel.Value;
                                                Console.WriteLine($"GOT LABEL {label}");
                                            }


                                            else if(buttonData[b] is KeyValuePairValue bStyle && bStyle.Key == "STYLE")
                                            {
                                                Console.WriteLine("GOT STYLE");
                                                style = (ButtonStyle)int.Parse(bStyle.Value.ToString());
                                            }


                                            else if(buttonData[b] is KeyValuePairValue bValue && bValue.Key == "VALUE")
                                            {
                                                Console.WriteLine($"GOT VALUE {bValue.Value}");
                                                value = bValue.Value;
                                                if(value is StringValue s)
                                                    value = $"\"{s}\"";
                                                else if(value is ExpressionValue ex)
                                                    value = $"`{ex}`";

                                            }


                                        }

                                        cb.WithButton(label: $"{label}", customId: $"e:{value}", style: style);

                                        Console.WriteLine("Done");
                                    }
                                }
                            }
                        }
                        break;
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
