using System.Text;
using Discord;
using Discord.Interactions;
using Gellybeans.Expressions;
using Newtonsoft.Json;
using System.Xml;

namespace MathfinderBot
{
    public class Evaluate : InteractionModuleBase
    {
        public enum InitOption
        {
            Add,
            List,
            New,
            Load,
            Save,
        }
        
        
        private CommandHandler handler;

        private ulong user;

        private static List<ulong> rolled = new List<ulong>();

        public Evaluate(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            base.BeforeExecute(command);
            user = Context.User.Id;
        }

        [SlashCommand("eval", "Do math with active character.")]
        public async Task EvalCommand(string expr)
        {
            Console.WriteLine(expr);
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            var parser = Parser.Parse(expr);
            var result = parser.Eval(Characters.Active[user], sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());
            
            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription($"{Characters.Active[user].CharacterName}")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"Events", $"{sb}");

            Console.WriteLine(sb.ToString());

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("craft", "Craft an item!")]
        public async Task CraftCommand(string itemName, int DC, int cost)
        {
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

        }
      
        //DM STUFF
        [RequireRole("DM")]
        [SlashCommand("req", "Calls for an evaluation")]
        public async Task RequestCommand(string expr)
        {
            rolled.Clear();

            expr = expr.Replace(" ", "");
               
            var message = $"{Context.Interaction.User.Mention} has requested a {expr} check";
      
            var cb = new ComponentBuilder()
                .WithButton(customId: $"req:{expr}", label: "Accept");
                          
            await RespondAsync(message, components: cb.Build());
        }

        [ComponentInteraction("req:*")]
        public async Task RequestAccept(string expr)
        {          
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(rolled.Contains(user))
            {
                await RespondAsync("You already rolled.", ephemeral: true);
                return;
            }

            rolled.Add(user);

            var sb = new StringBuilder();
            var parser = Parser.Parse(expr);
            var result = parser.Eval(Characters.Active[user], sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")            
                .WithDescription($"{Characters.Active[user].CharacterName}")   
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"Dice", $"{sb}");

            await RespondAsync(embed: builder.Build());
        }

        [RequireRole("DM")]
        [SlashCommand("dm-sec", "Secret DM rolls. :)")]
        public async Task SecretCommand(string expr, IUser target = null)
        {
            if(target == null)
            {               
                if(!Characters.Active.ContainsKey(user))
                {
                    await RespondAsync("No active character", ephemeral: true);
                    return;
                }

                var sb = new StringBuilder();
                var parser = Parser.Parse(expr);
                var result = parser.Eval(Characters.Active[user], sb);

                var ab = new EmbedAuthorBuilder()
                    .WithName(Context.Interaction.User.Username)
                    .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

                var builder = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithAuthor(ab)
                    .WithTitle($"{result}")
                    .WithDescription($"{Characters.Active[user].CharacterName}")
                    .WithFooter($"{expr}");

                if(sb.Length > 0) builder.AddField($"Events", $"{sb}");

                Console.WriteLine(sb.ToString());

                await RespondAsync(embed: builder.Build(), ephemeral: true);
                return;
            }
            
            if(Characters.Active.ContainsKey(target.Id))
            {
                var sb = new StringBuilder();
                var parser = Parser.Parse(expr);
                var result = parser.Eval(Characters.Active[target.Id], sb);

                var ab = new EmbedAuthorBuilder()
                   .WithName(Context.Interaction.User.Username)
                   .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

                var builder = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithAuthor(ab)
                    .WithTitle($"{result}")
                    .WithDescription($"{Characters.Active[target.Id].CharacterName}")
                    .WithFooter($"{expr}");

                if(sb.Length > 0) builder.AddField($"Events", $"{sb}");

                Console.WriteLine(sb.ToString());

                await RespondAsync(embed: builder.Build(), ephemeral: true);
                return;
            }

        }

        [RequireRole("DM")]
        //[SlashCommand("init", "Roll initiative")]
        public async Task InitCommand(InitOption option, string expr = "", string name = "", IAttachment initSave = null)
        {
            if(option == InitOption.Add)
            {
                if(Characters.Inits.ContainsKey(user))
                    Characters.Inits[user].Add
                        (new Init.InitObj() 
                        { 
                            Name = name, 
                            Value = Parser.Parse(expr).Eval(null, null) 
                        });
            }
                
            
            if(option == InitOption.List)
            {
                if(Characters.Inits[user] == null)
                {
                    await RespondAsync("No init");
                    return;
                }
              
                var sb = new StringBuilder();                 
                sb.Append("```");
                foreach(var entrant in Characters.Inits[user].InitObjs)      
                    sb.AppendLine($"|{entrant.Name,-20} |{entrant.Value,-3}");
                sb.Append("```");

                var eb = new EmbedBuilder()
                            .WithColor(Color.DarkRed)
                            .WithTitle($"List-Init()")
                            .WithDescription(sb.ToString());

            }
            
            if(option == InitOption.New)
            {
                var init = new Init();
                Characters.Inits[user] = init;

                expr = expr == "" ? "1d20+INIT" : expr.Replace(" ", "");

                var message = $"Roll Initiative! ({expr})";
                var cb = new ComponentBuilder()
                    .WithButton(customId: $"init:{expr},{user}", label: "Accept");

                await ReplyAsync(message, components: cb.Build());
                return;
            }

            if(option == InitOption.Save)
            {
                if(!Characters.Inits.ContainsKey(user))
                {
                    await RespondAsync("No initiative found.");
                    return;
                }
                if(expr == "")
                {
                    await RespondAsync("Use the expr field to give your save a name");
                    return;
                }

                var json = JsonConvert.SerializeObject(Characters.Inits[user], Newtonsoft.Json.Formatting.Indented);
                using var ms = new MemoryStream(Encoding.ASCII.GetBytes(json));
                await RespondWithFileAsync(ms, $"{expr}.txt", ephemeral: true);
                return;
            }
            
            if(option == InitOption.Load)
            {
                if(initSave != null)
                {
                    using var client = new HttpClient();
                    var data = await client.GetByteArrayAsync(initSave.Url);
                    var str = Encoding.Default.GetString(data);

                    var init = JsonConvert.DeserializeObject<Init>(str);
                    if(init != null)
                    {
                        Characters.Inits[user] = init;

                        var sb = new StringBuilder();

                        sb.Append("```");
                        foreach(var entrant in init.InitObjs)
                        {
                            sb.AppendLine($"|{entrant.Name, -20} -:- |{entrant.Value,-5}");
                        }
                        sb.Append("```");

                        var eb = new EmbedBuilder()
                            .WithColor(Color.DarkRed)
                            .WithTitle($"Load-Init({initSave.Filename})")
                            .WithDescription(sb.ToString());

                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                    await RespondAsync("Failed to load. Make sure you pick a file in the initSave field");
                    return;
                }
            }
        }
        
        [ComponentInteraction("init:*,*")]
        public async Task InitPressed(string expr, ulong id)
        {
            if(!Characters.Inits.ContainsKey(id) || Characters.Inits[id] != null)
            {
                await RespondAsync("No active inits", ephemeral: true);
                return;
            }                                       

            if(Characters.Inits[id].InitObjs.Any(x => x.Owner == id))
            {
                await RespondAsync("You already exist in this init", ephemeral: true);
                return;
            }
               
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }
                

            rolled.Add(id);

            var sb = new StringBuilder();
            var parser = Parser.Parse(expr);
            var result = parser.Eval(Characters.Active[user], sb);

            Characters.Inits[id].Add(new Init.InitObj(Characters.Active[user], result));

        }
        
    }
}
