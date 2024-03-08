using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MongoDB.Driver;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace MathfinderBot
{
    public class Xp : InteractionModuleBase
    {
        public enum XpAction
        {
            List,
            Details,
            New,
            Update,
            Delete
        }

        public enum XpTrack
        {
            Slow,
            Medium,
            Fast
        }

        public static int[] Slow = new int[]
        {  
            3000,
            7500,
            14000,
            23000,
            35000,
            53000,
            77000,
            115000,
            160000,
            235000,
            330000,
            475000,
            665000,
            955000,
            1350000,
            1900000,
            2700000,
            3850000,
            5350000
        };
        public static int[] Medium = new int[]
        {
            2000,
            5000,
            9000,
            15000,
            23000,
            35000,
            51000,
            75000,
            105000,
            155000,
            220000,
            315000,
            445000,
            635000,
            890000,
            1300000,
            1800000,
            2550000,
            3600000
        };
        public static int[] Fast = new int[]
        {
            1300,
            3300,
            6000,
            10000,
            15000,
            23000,
            34000,
            50000,
            71000,
            105000,
            145000,
            210000,
            295000,
            425000,
            600000,
            850000,
            1200000,
            1700000,
            2400000
        };

        ulong user;
        public InteractionService Service { get; set; }
        public static Regex parens = new Regex(@"\[.+\]", RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);


        [SlashCommand("xp", "Experience.")]
        public async Task XpCommand(XpAction action, [Summary("xp_name"), Autocomplete] string name = "")
        {      
            switch(action)
            {
                case XpAction.List:
                    await ListXpCommand(name);
                    return;
                case XpAction.Details:
                    await DetailsXpCommand();
                    return;
                case XpAction.New:
                    await NewXpCommand(name);
                    return;
                case XpAction.Update:                 
                    await UpdateXpCommand(name);
                    return;
                case XpAction.Delete:
                    await DeleteXpCommand(name);
                    return;
            }
        }

        public async Task ListXpCommand(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                var result = await Program.GetXp().Find(_ => true).ToListAsync();
                result.Sort((x, y) => x.Name.CompareTo(y.Name));
                if (result.Count > 0)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine($"|{"NAME",-25} |{"XP",-8} |{"TRACK",-4} |{"LVL"}");
                    sb.AppendLine();
                    foreach(var xpo in result)
                        sb.AppendLine($"|{xpo.Name,-25} |{xpo.Experience,-8} |{(xpo.Track == XpTrack.Slow ? "S" : xpo.Track == XpTrack.Medium ? "M" : "F"),-4}  |{await GetLevel(xpo)}");                  
                    
                    await RespondAsync($"```{sb}```");
                }
            }
            else
            {
                var xp = await Program.GetXp().Find(x => x.Name == name).ToListAsync();
                if(xp.Count > 0)
                {
                    var eb = new EmbedBuilder().WithDescription($"{(xp[0].Details != string.Empty ? $"*{xp[0].Details}*" : "")}\r\n\r\n{await GetLevelInfo(xp[0], 2)}");
                    await RespondAsync(embed: eb.Build());
                }
            }
        }

        public async Task DetailsXpCommand()
        {
            var result = await Program.GetXp().Find(x => x.Name.Contains("[D]") || x.Name.Contains("[F]")).ToListAsync();
                     
            foreach(var xp in result)
            {
                xp.Name = parens.Replace(xp.Name, "").Trim();
            }
            
            result.Sort((x, y) => x.Name.CompareTo(y.Name));

            var sb = new StringBuilder();
            var sbs = new List<StringBuilder>() { sb };
            
            for (int i = 0; i < result.Count; i++)
            {
                if(sb.Length > 3000)
                {
                    sb = new StringBuilder();
                    sbs.Add(sb);
                }
                sb.AppendLine(await GetLevelInfo(result[i],0));
            }


            int count = 0;
            for(int i = 0; i < sbs.Count; i++)
            {
                count += sbs[i].Length;
                var emb = new EmbedBuilder().WithDescription(sbs[i].ToString());
                await Context.Channel.SendMessageAsync(embed: emb.Build());
            }

            Console.WriteLine(count);
        }


        public async Task NewXpCommand(string name)
        {
            if(Context.Interaction.User is SocketGuildUser gUser)
            {
                if(gUser.Roles.Any(x => x.Name == "DM"))
                {
                    if(string.IsNullOrEmpty(name))
                    {
                        await RespondAsync("Please enter a name.", ephemeral: true);
                        return;
                    }

                    var results = await Program.GetXp().FindAsync(x => x.Name == name);
                    var list = results.ToList();
                    if(list.Count > 0)
                    {
                        await RespondAsync("That name already exists.", ephemeral: true);
                        return;
                    }

                    var mb = new ModalBuilder($"New-XP {name}", $"new_xp:{name}")
                        .AddTextInput(new TextInputBuilder($"XP Amount", "add", value: "0"))
                        .AddTextInput(new TextInputBuilder("Track (S, M, or F)", "track", required: true, value: "M"))
                        .AddTextInput(new TextInputBuilder("Max Level", "MaxLevel", required: false, value: "20"))
                        .AddTextInput(new TextInputBuilder($"Details", "details", TextInputStyle.Paragraph, required: false))
                        .AddTextInput(new TextInputBuilder($"Level Info (Separated by semicolons) ", "levelinfo", TextInputStyle.Paragraph, required: false));

                    await RespondWithModalAsync(mb.Build());
                }
            }                                  
        }

        public async Task UpdateXpCommand(string name)
        {
            if(Context.Interaction.User is SocketGuildUser gUser)
            {
                if(gUser.Roles.Any(x => x.Name == "DM"))
                {
                    var results = await Program.GetXp().FindAsync(x => x.Name == name);
                    var xp = results.ToList();

                    if(xp.Count > 0)
                    {
                        var mb = new ModalBuilder("Update-XP", $"set_xp:{name}")
                            .AddTextInput(new TextInputBuilder($"+ or - XP (Current: {xp[0].Experience})", "add", value: "0"))
                            .AddTextInput(new TextInputBuilder("Track (S, M, or F)", "track", required: false,  value: xp[0].Track == XpTrack.Slow ? "S" : xp[0].Track == XpTrack.Medium ? "M" : "F"))
                            .AddTextInput(new TextInputBuilder("Max Level", "MaxLevel", required: false, value: xp[0].MaxLevel.ToString()))
                            .AddTextInput(new TextInputBuilder($"Details", "details", TextInputStyle.Paragraph, required: false, value: xp[0].Details))
                            .AddTextInput(new TextInputBuilder($"Level Info (Separated by semicolons) ", "levelinfo", TextInputStyle.Paragraph, required: false, value: xp[0].LevelInfo));

                        await RespondWithModalAsync(mb.Build());                              
                    }
                    else
                        await RespondAsync($"{name} not found.", ephemeral: true);
                }
            }                   
        }
    
        public async Task DeleteXpCommand(string name)
        {
            if(Context.Interaction.User is SocketGuildUser gUser)
            {
                if(gUser.Roles.Any(x => x.Name == "DM"))
                {
                    var results = await Program.GetXp().FindAsync(x => x.Name == name);
                    var xp = results.ToList();

                    if(xp.Count > 0)
                    {
                        Console.WriteLine("deleting");
                        await Program.GetXp().DeleteOneAsync(x => x.Name == name);
                        await RespondAsync($"{name} removed.");
                        return;
                    }
                    await RespondAsync("Not found.", ephemeral: false);
                }
            }                    
        }

        public static async Task<int[]> GetTrack(XpObject xp)
        {
            return await Task.Run(() =>
            {
                return xp.Track == XpTrack.Slow ? Slow :
                       xp.Track == XpTrack.Medium ? Medium :
                       Fast;
            });
        }

        public static async Task<int> GetLevel(XpObject xp)
        {
            return await Task.Run(async () =>
            {
                var level = 1;
                var xpArray = await GetTrack(xp);
                for(int i = 0; i < xpArray.Length && level < xp.MaxLevel; i++)
                {
                    if(xp.Experience >= xpArray[i])
                        level++;
                    else break;
                }
                return level;
            });                
        }

        public static async Task<int> GetXpToNextLevel(XpObject xp)
        {
            int level = await GetLevel(xp);
            
            if(level == xp.MaxLevel)
                return -1;

            var xpArray = await GetTrack(xp);           
            var xpToLevel = xpArray[level - 1] - xp.Experience;
            return xpToLevel;
        }

        public static async Task<string> GetLevelInfo(XpObject xp, int offset)
        {
            var r = Regex.Replace(xp.LevelInfo, @"\t|\n|\r", "");
            var split = r.Split(';', StringSplitOptions.RemoveEmptyEntries);
            int level = await GetLevel(xp);
            var sb = new StringBuilder();

            var toNext = await GetXpToNextLevel(xp);

            sb.AppendLine($"__**{xp.Name.ToUpper()}**__ **LV:**{level} **XP:**{xp.Experience} **N:**{(toNext != -1 ? toNext : "---" )}");

            for(int i = 0; i < level + offset && i < split.Length && i < xp.MaxLevel; i++)
            {
                if(i == level)
                    sb.AppendLine("**-NEXT-**");
                sb.AppendLine($"* **Lv {i+1}** " + split[i]);
            }

            return sb.ToString();
        }

        [AutocompleteCommand("xp_name", "xp")]
        public async Task AutoCompleteXp()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteXp.Where(x => x.Name.Contains(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }
    }
}
