using System.Text;
using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using Newtonsoft.Json;
using MathfinderBot.Modal;

namespace MathfinderBot
{
    public class Character : InteractionModuleBase
    {
        public enum CharacterCommand
        {            
            Set,
            New,
            List,
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

        public enum SheetType
        {
            [ChoiceDisplay("Pathbuilder (PF)")]
            Pathbuilder,
            
            [ChoiceDisplay("HeroLabs (PF)")]
            HeroLabs,
            
            [ChoiceDisplay("PCGen (PF)")]
            PCGen,

            [ChoiceDisplay("JSON (PF)")]
            JSON,
        }

        static Dictionary<ulong, string>    lastInputs  = new Dictionary<ulong, string>();
        static Regex                        validName   = new Regex(@"^[a-zA-Z' ]{3,50}$");

        ulong                       user;
        IMongoCollection<StatBlock> collection;
        public  InteractionService  Service { get; set; }
        private CommandHandler      handler;

        public Character(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            collection = Program.database.GetCollection<StatBlock>("statblocks");
        }

        [SlashCommand("char", "Create, set, export, delete characters.")]
        public async Task CharCommand(CharacterCommand mode, string charNameOrNumber = "", GameType game = GameType.Pathfinder)
        {
            var nameToUpper = charNameOrNumber.ToUpper();
            lastInputs[user] = nameToUpper;

            //find all documents that belong to user, load them into dictionary.
            Characters.Database[user] = new List<StatBlock>();
            var chars = await collection.FindAsync(x => x.Owner == user);
            var characters = chars.ToList();
            
            foreach(var statblock in characters)
                Characters.Database[user].Add(statblock);
        
            
            if(mode == CharacterCommand.Export)
            {
                var character = Characters.Database[user].FirstOrDefault(x => x.CharacterName == charNameOrNumber);
                if(character != null)
                {
                    var json = JsonConvert.SerializeObject(character, Formatting.Indented);
                    using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));
                  
                    await RespondWithFileAsync(stream, $"{charNameOrNumber}.txt", ephemeral: true);
                    return;
                }
                await RespondAsync($"{charNameOrNumber} not found.", ephemeral: true);
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
                
                for(int i = 0; i < characters.Count; i++)
                    sb.AppendLine($"{i} — {characters[i].CharacterName}");
                
                await RespondAsync(sb.ToString(), ephemeral: true);
            }
            
            if(mode == CharacterCommand.Set)
            {
                var outVal = 0;

                if(int.TryParse(charNameOrNumber, out outVal) && outVal >= 0 && outVal < characters.Count)
                {
                    Characters.SetActive(user, characters[outVal]);
                    await RespondAsync($"{characters[outVal].CharacterName} set!", ephemeral: true);
                    return;
                }
                else if(validName.IsMatch(charNameOrNumber))
                {
                    var character = characters.FirstOrDefault(x => x.CharacterName == charNameOrNumber);
                    if(character != null)
                    {
                        Characters.SetActive(user, character);
                        await RespondAsync($"{charNameOrNumber} set!", ephemeral: true);
                        return;
                    }
                }
              
                await RespondAsync("Character not found", ephemeral: true);
                return;               
            }
         

            if(mode == CharacterCommand.New)
            {
                if(!validName.IsMatch(charNameOrNumber))
                {
                    await RespondAsync("Invalid character name.", ephemeral: true);
                    return;
                }

                if(Characters.Database[user].Any(x => x.CharacterName.ToUpper() == charNameOrNumber.ToUpper()))
                {
                    await RespondAsync($"{charNameOrNumber} already exists.", ephemeral: true);
                    return;
                }

                if(Characters.Database[user].Count >= 5)
                {
                    await RespondAsync("You have too many characters. Delete one before making another.");
                    return;
                }

                StatBlock statblock = new StatBlock();
                
                switch(game)
                {
                    case GameType.None:
                        statblock = new StatBlock() { CharacterName = charNameOrNumber };
                        break;
                    
                    case GameType.Pathfinder:
                        statblock = StatBlock.DefaultPathfinder(charNameOrNumber);
                        break;

                    case GameType.FifthEd:
                        statblock = StatBlock.DefaultFifthEd(charNameOrNumber);
                        break;

                    case GameType.Starfinder:
                        statblock = StatBlock.DefaultStarfinder(charNameOrNumber);
                        break;
                }
                
                statblock.Owner = user;         
                
                await collection.InsertOneAsync(statblock);

                var quote = Quotes.Get(charNameOrNumber); 

                var eb = new EmbedBuilder()
                    .WithColor(Color.DarkPurple)
                    .WithTitle($"New-Character({charNameOrNumber})")
                    .WithDescription($"{Context.User.Mention} has created a new character, {charNameOrNumber}.\n\n “{quote}”");
                
                Characters.Active[user] = statblock;
                
                await RespondAsync(embed: eb.Build());                 
            }

            if(mode == CharacterCommand.Delete)
            {                                            
                if(!Characters.Database[user].Any(x => x.CharacterName.ToUpper() == charNameOrNumber.ToUpper()))
                {
                    await RespondAsync("Character not found.", ephemeral: true);
                    return;
                }

                await RespondWithModalAsync<ConfirmModal>("confirm_delete");           
            }           
        }     

        [SlashCommand("char-update", "Update an active character")]
        public async Task UpdateCommand(SheetType sheetType, IAttachment file)
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
                if(file.Filename.ToUpper().Contains(".PDF") || file.Filename.ToUpper().Contains(".XML") || file.Filename.ToUpper().Contains(".TXT"))
                {                 
                    await RespondAsync("Updating sheet...", ephemeral: true);
                    
                    var Id = Characters.Active[user].Id;
                    var name = Characters.Active[user].CharacterName;

                    StatBlock stats = null;
                    if(sheetType == SheetType.JSON)
                        stats = JsonConvert.DeserializeObject<StatBlock>(Encoding.UTF8.GetString(data));                
                    if(sheetType == SheetType.Pathbuilder)
                        stats = Utility.UpdateWithPathbuilder(stream, Characters.Active[user]);
                    if(sheetType == SheetType.HeroLabs)
                        stats = Utility.UpdateWithHeroLabs(stream, Characters.Active[user]);
                    if(sheetType == SheetType.PCGen)
                        stats = Utility.UpdateWithPCGen(stream, Characters.Active[user]);
                    Console.WriteLine("Done!");

                    if(stats != null)
                    {
                        stats.Id = Id;
                        stats.CharacterName = name;
                        Characters.Active[user] = stats;
                        await Program.UpdateStatBlock(stats);
                        await FollowupAsync("Updated!", ephemeral: true);
                        return;
                    }
                    await FollowupAsync("Failed to update sheet", ephemeral: true);                   
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

            Characters.Database[user].Remove(Characters.Database[user].FirstOrDefault(x => x.CharacterName == name));
            await collection.DeleteOneAsync(x => x.Owner == user && x.CharacterName.ToUpper() == name);     
            
            await RespondAsync($"{lastInputs[user]} removed", ephemeral: true);
        }        
    }
}