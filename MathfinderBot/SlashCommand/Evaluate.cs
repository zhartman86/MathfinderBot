using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Expressions;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;

namespace MathfinderBot
{
    public class Evaluate : InteractionModuleBase
    {
        public enum InitOption
        {
            Add,
            Remove,
            List,
            Roll,
            Sort,
            Move,
            Request,
            New,
            Private,
            Load,
            Save,
        }
                
        private CommandHandler  handler;
        private ulong           user;

        private static List<ulong> rolled = new List<ulong>();

        public Evaluate(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            base.BeforeExecute(command);
            user = Context.User.Id;
        }

        [SlashCommand("eval", "Evaluate stats and expressions, modify bonuses")]
        public async Task EvalCommand(string expr, bool isHidden = false, string targets = "")
        {                     
            Console.WriteLine(expr);
            var sbs = new List<StringBuilder>();
            var description = "";
            int result      = 0;

            if(targets != "")
            {
                if(Context.Interaction.User is SocketGuildUser gUser)
                {
                    if(gUser.Roles.Any(x => x.Name == "DM"))
                    {
                        description = "";
                        var targetList = new List<IUser>();
                        var regex = new Regex(@"\D+");
                        var replace = regex.Replace(targets, " ");                      
                        var split = replace.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        for(int i = 0; i < split.Length; i++)
                        {
                            var id = 0ul;
                            ulong.TryParse(split[i], out id);
                            var dUser = await Program.client.GetUserAsync(id);
                            if(dUser != null) 
                                targetList.Add(dUser);
                        }

                        if(targetList.Count > 0)
                            for(int i = 0; i < targetList.Count; i++)
                                if(Characters.Active.ContainsKey(targetList[i].Id))
                                {
                                    var sb = new StringBuilder();
                                    var parse = Parser.Parse(expr);
                                    sb.AppendLine($"{targetList[i].Mention}");
                                    result = parse.Eval(Characters.Active[targetList[i].Id], sb);
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
                        await RespondAsync("You require the `DM` role in order to select targets");
                        return;
                    }
                }
            }
            else
            {
                if(!Characters.Active.ContainsKey(user))
                {
                    await RespondAsync("No active character", ephemeral: true);
                    return;
                }

                description = $"*{Characters.Active[user].CharacterName}*";             
                var sb = new StringBuilder();
                var parser = Parser.Parse(expr);
                result = parser.Eval(Characters.Active[user], sb);
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
        [SlashCommand("init", "Roll initiative")]
        public async Task InitCommand(InitOption option, string expr = "", IAttachment initSave = null)
        {
            if(option == InitOption.New)
            {
                await RespondWithModalAsync<InitModal>("new_init");
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

                        var eb = new EmbedBuilder()
                            .WithColor(Color.DarkRed)
                            .WithTitle($"Load-Init({initSave.Filename})")
                            .WithDescription($"```{init}```");
                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                    await RespondAsync("Failed to load. Make sure you pick a file in the initSave field");
                    return;
                }
            }

            if(!Characters.Inits.ContainsKey(user))
            {
                await RespondAsync("No init readied", ephemeral: true);
                return;
            }

            var outVal = 0;
            if(option == InitOption.Add)
            {                     
                Init.InitObj initObj = null;                
                var split = expr.Split(new char[] { ':', '\t' }, options: StringSplitOptions.RemoveEmptyEntries);
                if(split.Length > 1)
                    initObj = new Init.InitObj() { Name = split[0], Bonus = int.TryParse(split[1], out outVal) ? outVal : 0 };
                else if(split.Length > 0)
                    initObj = new Init.InitObj() { Name = split[0], Bonus = 0 };
                if(initObj != null)
                {
                    initObj.Rolled = Parser.Parse($"{Characters.Inits[user].Expr}+{initObj.Bonus}").Eval(null, null);
                    Characters.Inits[user].Add(initObj);
                    await RespondAsync($"Added {split[0]}", ephemeral: true);
                }
                else await RespondAsync($"Invalid data: {expr}", ephemeral: true);
                return;             
            }

            if(option == InitOption.Remove)
            {                
                if(int.TryParse(expr, out outVal))
                {
                    var remove = Characters.Inits[user].Remove(outVal);
                    if(remove != null) await RespondAsync($"{remove.Name} removed", ephemeral: true);
                    else await RespondAsync("Not found", ephemeral: true);
                    return;
                }
                await RespondAsync("Invalid input. Pick a number to remove", ephemeral: true);   
            }

            if(option == InitOption.Move)
            {
                if(!int.TryParse(expr, out outVal))
                {
                    await RespondAsync("Invalid input. Pick the number of the player you wish to move", ephemeral: true);
                    return;
                }

                if(!Characters.Inits[user].Move(outVal))
                {
                    await RespondAsync("Couldn't move");
                    return;
                }

                var eb = new EmbedBuilder()
                            .WithColor(Color.DarkRed)
                            .WithTitle($"List-Init()")
                            .WithDescription($"```{Characters.Inits[user]}```");

                await RespondAsync(embed: eb.Build());
                return;
            }
            
            if(option == InitOption.Private)
            {
                Characters.Inits[user].isPrivate = !Characters.Inits[user].isPrivate;
                await RespondAsync($"Private init: {Characters.Inits[user].isPrivate}");
                return;
            }

            if(option == InitOption.List)
            {
                if(Characters.Inits[user] == null)
                {
                    await RespondAsync("No init");
                    return;
                }

                var eb = new EmbedBuilder()
                            .WithColor(Color.DarkRed)
                            .WithTitle($"List-Init()")
                            .WithDescription($"```{Characters.Inits[user]}```");

                await RespondAsync(embed: eb.Build(), ephemeral: Characters.Inits[user].isPrivate);
                return;
            }
            
            if(option == InitOption.Sort)
            {
                if(!Characters.Inits.ContainsKey(user))
                {
                    await RespondAsync("Nothing to sort", ephemeral: true);
                    return;
                }

                Characters.Inits[user].Sort();
                await RespondAsync("Sorted", ephemeral: true);
                return;
            }

            if(option == InitOption.Request)
            {
                if(Characters.Inits.ContainsKey(user))
                {
                    var expression = expr != "" ? expr.Replace(" ", "") : "INIT_BONUS";

                    var message = $"Roll Initiative ({expr})";
                    var cb = new ComponentBuilder()
                        .WithButton(customId: $"init:{expr},{user}", label: "Accept");

                    await ReplyAsync(message, components: cb.Build());
                    return;
                }
            }          

            if(option == InitOption.Roll)
            {
                if(!Characters.Inits.ContainsKey(user) || Characters.Inits[user] == null)
                {
                    await RespondAsync("No init to roll", ephemeral: true);
                    return;
                }
                Characters.Inits[user].Roll();
                await RespondAsync("Rolled!", ephemeral: true);
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
                    await RespondAsync("Use the expr field to give your save a expr");
                    return;
                }

                var json = JsonConvert.SerializeObject(Characters.Inits[user], Newtonsoft.Json.Formatting.Indented);
                using var ms = new MemoryStream(Encoding.ASCII.GetBytes(json));
                await RespondWithFileAsync(ms, $"{expr}.txt", ephemeral: true);
                return;
            }           
        }

        [ModalInteraction("new_init")]
        public async Task NewInitCommand(InitModal modal)
        {
            if(modal.InitList == "")
            {
                await RespondAsync("Init list empty. Add at least one entry. (Example: WIZARDBRO:4)");
                return;
            }

            var init = new Init();
            var reader = new StringReader(modal.InitList);
            while(true)
            {
                var line = await reader.ReadLineAsync();
                if(line != null)
                {
                    if(string.IsNullOrEmpty(line))
                        continue;

                    int outVal = -100;
                    Init.InitObj initObj = null;
                    var split = line.Split(new char[] { ':', '\t' }, options: StringSplitOptions.RemoveEmptyEntries);
                    if(split.Length > 1)
                        initObj = new Init.InitObj() { Name = split[0], Bonus = int.TryParse(split[1], out outVal) ? outVal : 0 };
                    else if(split.Length > 0)
                        initObj = new Init.InitObj() { Name = split[0], Bonus = 0 };
                    if(initObj != null)
                        init.Add(initObj);
                }
                else break;             
            }
            
            if(init.InitObjs.Count > 0)
            {
                Characters.Inits[user] = init;
                await RespondAsync($"Init created. COUNT: {init.InitObjs.Count}", ephemeral: true);
                return;
            }
            await RespondAsync("No valid entries found", ephemeral: true);
        }
        
        [ComponentInteraction("init:*,*")]
        public async Task InitPressed(string expr, ulong id)
        {
            if(!Characters.Inits.ContainsKey(id) || Characters.Inits[id] == null)
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
                
            var sb = new StringBuilder();
            var parser = Parser.Parse($"{Characters.Inits[user].Expr}+{expr}");
            var result = parser.Eval(Characters.Active[user], sb);

            Characters.Inits[id].Add(new Init.InitObj(user, result));
        }

        [RequireRole("DM")]
        [SlashCommand("next", "Used during active init")]
        public async Task InitNextCommand()
        {
            if(!Characters.Inits.ContainsKey(user) || Characters.Inits[user] == null)
            {
                await RespondAsync("No active inits", ephemeral: true);
                return;
            }

            if(Characters.Inits[user].LastMessage != 0 && !Characters.Inits[user].isPrivate) await Context.Channel.DeleteMessageAsync(Characters.Inits[user].LastMessage);
            var current = Characters.Inits[user].Next();      
            var eb = new EmbedBuilder()
                            .WithColor(Color.DarkRed)
                            .WithTitle($"Next-Init()")
                            .WithDescription($"```{Characters.Inits[user].ToString(1, 2)}```");

            await RespondAsync(embed: eb.Build(), ephemeral: Characters.Inits[user].isPrivate);
            var msg = await Context.Interaction.GetOriginalResponseAsync();
            Characters.Inits[user].LastMessage = msg.Id;

            if(Characters.Active.ContainsKey(Characters.Inits[user].InitObjs[Characters.Inits[user].Current].Owner))
                await FollowupAsync($"```{Utility.GetPathfinderQuick(Characters.Active[Characters.Inits[user].InitObjs[Characters.Inits[user].Current].Owner])}```");
        }
    }
}
