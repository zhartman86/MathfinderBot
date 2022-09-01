using System.Text;
using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using Newtonsoft.Json;


namespace MathfinderBot
{
    public class Character : InteractionModuleBase
    {
        public enum CharacterCommand
        {            
            Set,
            New,
            List,
            Update,
            Export,
            Delete
        }

        public enum GameType
        {
            None,
            Pathfinder,
            Starfinder,
            FifthEd
        }

        static Dictionary<ulong, string> lastInputs = new Dictionary<ulong, string>();
        static Regex validName = new Regex(@"^[a-zA-Z' ]{3,50}$");

        ulong user;
        IMongoCollection<StatBlock> collection;

        public  InteractionService  Service { get; set; }
        private CommandHandler      handler;

        public Character(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            collection = Program.database.GetCollection<StatBlock>("statblocks");
        }


        [SlashCommand("char", "Create, set, export/import, delete characters.")]
        public async Task CharCommand(CharacterCommand mode, string charName = "", string options = "", GameType game = GameType.Pathfinder)
        {        
            var nameToUpper = charName.ToUpper();
            lastInputs[user] = charName;
            
            //find all documents that belong to user, load them into dictionary.
            Characters.Database[user] = new Dictionary<string, StatBlock>();
            var chars = await collection.FindAsync(x => x.Owner == user);
            var characters = chars.ToList();
            
            foreach(var statblock in characters)
                Characters.Database[user][statblock.CharacterName] = statblock;
        
            
            
            if(mode == CharacterCommand.Export)
            {
                if(Characters.Database[user].ContainsKey(charName))
                {
                    var json = JsonConvert.SerializeObject(Characters.Database[user][charName], Formatting.Indented);
                    using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));
                  
                    await RespondWithFileAsync(stream, $"{charName}.txt", ephemeral: true);
                    return;
                }
                await RespondAsync($"{charName} not found.", ephemeral: true);
                return;
            }

            if(mode == CharacterCommand.List)
            {
                if(Characters.Database[user].Count == 0)
                {
                    await RespondAsync("You don't have any characters.", ephemeral: true);
                    return;
                }

                var sb = new StringBuilder();
                
                foreach(var character in Characters.Database[user].Keys)
                    sb.AppendLine($"-> {character}");
                
                await RespondAsync(sb.ToString(), ephemeral: true);
            }
            
            if(mode == CharacterCommand.Set)
            {
                if(!validName.IsMatch(charName))
                {
                    await RespondAsync("Invalid character name.", ephemeral: true);
                    return;
                }

                foreach(var c in Characters.Database[user].Values)
                {
                    if(c.CharacterName.ToUpper() == charName.ToUpper())
                    {
                        Characters.SetActive(user, Characters.Database[user][charName]);
                        await RespondAsync($"{charName} set!", ephemeral: true);
                        return;
                    }
                }
                await RespondAsync("Character not found", ephemeral: true);
                return;               
            }

            if(mode == CharacterCommand.New)
            {
                if(!validName.IsMatch(charName))
                {
                    await RespondAsync("Invalid character name.", ephemeral: true);
                    return;
                }

                if(Characters.Database[user].ContainsKey(charName))
                {
                    await RespondAsync($"{charName} already exists.", ephemeral: true);
                    return;
                }

                if(Characters.Database[user].Count >= 5)
                {
                    await RespondAsync("You have too many characters. Delete one before making another.");
                }

                var split = options.Split(new char[] { ',', ' ', ':', ';', '.', '|' });
                List<int> scores = new List<int>();
                if(split.Length == 6)
                {                   
                    int intOut = 0;
                    for(int i = 0; i < split.Length; i++)
                    {
                        if(int.TryParse(split[i], out intOut))
                        {
                            scores.Add(intOut);
                        }
                    }
                }

                StatBlock statblock = new StatBlock();
                
                switch(game)
                {
                    case GameType.None:
                        statblock = new StatBlock() { CharacterName = charName };
                        break;
                    
                    case GameType.Pathfinder:
                        statblock = StatBlock.DefaultPathfinder(charName);
                        break;

                    case GameType.FifthEd:
                        statblock = StatBlock.DefaultFifthEd(charName);
                        break;

                    case GameType.Starfinder:
                        statblock = StatBlock.DefaultStarfinder(charName);
                        break;
                }
                
                statblock.Owner = user;
                
                var scoreArray = scores.ToArray();
                if(scoreArray.Length == 6)
                {
                    statblock.Stats["STR_SCORE"] = scores[0];
                    statblock.Stats["DEX_SCORE"] = scores[1];
                    statblock.Stats["CON_SCORE"] = scores[2];
                    statblock.Stats["INT_SCORE"] = scores[3];
                    statblock.Stats["WIS_SCORE"] = scores[4];
                    statblock.Stats["CHA_SCORE"] = scores[5];
                }

                await collection.InsertOneAsync(statblock);

                var quote = Quotes.Get(charName); 

                var eb = new EmbedBuilder()
                    .WithColor(Color.DarkPurple)
                    .WithTitle($"New-Character({charName})")
                    .WithDescription($"{Context.User.Mention} has created a new character, {charName}.\n\n “{quote}”");

                Characters.Active[user] = statblock;
                
                await RespondAsync(embed: eb.Build());                 
            }

            if(mode == CharacterCommand.Delete)
            {                             
                
                if(!Characters.Database[user].ContainsKey(charName))
                {
                    await RespondAsync("Character not found.", ephemeral: true);
                    return;
                }

                await RespondWithModalAsync<ConfirmModal>("confirm_delete");           
            }
            
        }

        [SlashCommand("update", "Update an active character")]
        public async Task UpdateCommand(IAttachment file)
        {
            
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }
            
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(file.Url);
            var stream = new MemoryStream(data);

            if(stream != null)
            {
                if(file.Filename.ToUpper().Contains(".PDF") || file.Filename.ToUpper().Contains(".XML"))
                {
                    await RespondAsync("Updating sheet...", ephemeral: true);
                    var stats = Utility.UpdateWithHeroLabs(stream, Characters.Active[user]);                    
                    if(stats == null)
                    {
                        await RespondAsync("Invalid data.");
                        return;
                    }
                    
                    stats.Id = Characters.Active[user].Id;
                    await Program.UpdateStatBlock(stats);
                    await FollowupAsync("Updated!", ephemeral: true);
                    return;
                }              
            }
            await RespondAsync("Invalid data", ephemeral: true);
        }

        [ModalInteraction("confirm_delete")]
        public async Task ConfirmDeleteChar(ConfirmModal modal)
        {       
            if(modal.Confirm != "CONFIRM")
            {
                await RespondAsync("You didn't die. Try again. (make sure type 'CONFIRM' in all caps)", ephemeral: true);
                return;
            }

            string name = lastInputs[user];

            Characters.Database[user].Remove(name);
            await collection.DeleteOneAsync(x => x.Owner == user && x.CharacterName == name);
      
            await RespondAsync($"{lastInputs[user]} removed", ephemeral: true);
        }
    }
}