using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MongoDB.Driver;
using System.Text;

namespace MathfinderBot
{
    public class Xp : InteractionModuleBase
    {
        public enum XpAction
        {
            List,
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


        [SlashCommand("xp", "Experience.")]
        public async Task XpCommand(XpAction action, [Summary("xp_name"), Autocomplete] string name = "", int number = 0, XpTrack track = XpTrack.Medium)
        {      
            switch(action)
            {
                case XpAction.List:
                    await ListXpCommand(name);
                    return;
                case XpAction.New:
                    await NewXpCommand(name, number, track);
                    return;
                case XpAction.Update:                 
                    await UpdateXpCommand(name, number);
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
                    await RespondAsync($"{xp[0].Name} is currently level {await GetLevel(xp[0])} with {xp[0].Experience} xp, and needs {GetXpToNextLevel(xp[0])} more xp to level up.\r\n```{xp[0].Details}```");
                }
            }
        }

        public async Task NewXpCommand(string name, int number, XpTrack track)
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


                    var xpo = new XpObject
                    {
                        Name = name,
                        Track = track,
                        Experience = number
                    };

                    await Program.InsertXp(xpo);
                    await RespondAsync($"{xpo.Name} added with {xpo.Experience} xp.");
                    await DataMap.GetXps();
                }
            }                                  
        }

        public async Task UpdateXpCommand(string name, int number)
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
                            .AddTextInput(new TextInputBuilder($"Add (Current:{xp[0].Experience})", "add", value: $"{number}"))
                            .AddTextInput(new TextInputBuilder($"Details", "details", TextInputStyle.Paragraph, required: false, minLength: 0, value: xp[0].Details));

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

        public async Task<int> GetLevel(XpObject xp)
        {
            var level = 1;

            int[] xpArray = xp.Track == XpTrack.Slow ? Slow : 
                            xp.Track == XpTrack.Medium ? Medium :
                            Fast;
            
            for(int i = 0; i < xpArray.Length; i++)
            {
                if(xp.Experience >= xpArray[i])
                    level++;
                else break;
            }
            return level;
        }

        public int GetXpToNextLevel(XpObject xp)
        {
            var xpToLevel = 1;

            int[] xpArray = xp.Track == XpTrack.Slow ? Slow :
                            xp.Track == XpTrack.Medium ? Medium :
                            Fast;

            for(int i = 0; i < xpArray.Length; i++)
            {
                if(xp.Experience > xpArray[i])
                    continue;
                else
                {
                    xpToLevel = xpArray[i] - xp.Experience;
                    break;
                }
                    
            }
            return xpToLevel;
        }

        [AutocompleteCommand("xp_name", "xp")]
        public async Task AutoCompleteXp()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteXp.Where(x => x.Name.StartsWith(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }
    }
}
