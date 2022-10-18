using System.Text;
using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.IO;
using System.Runtime.CompilerServices;

namespace MathfinderBot
{
    public class Character : InteractionModuleBase
    {
        public enum CharacterCommand
        {            
            Set,            
            List,
            Export,

            [ChoiceDisplay("Change-Name")]
            ChangeName,

            Update,
            Add,
            New,
            Give,
            Delete
        }

        public enum CampaignAction
        {

        }

        public enum SheetType
        {
            [ChoiceDisplay("Pathbuilder (PDF)")]
            Pathbuilder,
            
            [ChoiceDisplay("HeroLabs (XML)")]
            HeroLabs,
            
            [ChoiceDisplay("PCGen (XML)")]
            PCGen,

            [ChoiceDisplay("Mottokrosh (JSON)")]
            Mottokrosh,

            [ChoiceDisplay("Mathfinder (JSON)")]
            Mathfinder,
        }

        static Dictionary<ulong, StatBlock> lastGive    = new Dictionary<ulong, StatBlock>();
        static Dictionary<ulong, string>    lastInputs  = new Dictionary<ulong, string>();
        static Regex                        validName   = new Regex(@"^[a-zA-Z' ]{3,75}$");

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

        async Task ExportCharacter(string charNameOrNumber, List<StatBlock> characters)
        {
            var outVal = 0;
            var toUpper = charNameOrNumber.ToUpper();
            StatBlock statblock = null;
            if(int.TryParse(toUpper, out outVal) && outVal >= 0 && outVal < characters.Count)
                statblock = characters[outVal];
            else if(validName.IsMatch(toUpper))
                statblock = characters.FirstOrDefault(x => x.CharacterName.ToUpper() == toUpper)!;

            if(statblock != null)
            {
                var json = JsonConvert.SerializeObject(statblock, Formatting.Indented);
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));

                await RespondWithFileAsync(stream, $"{statblock.CharacterName}.txt", ephemeral: true);
                return;
            }
            await RespondAsync($"{charNameOrNumber} not found.", ephemeral: true);
            return;
        }
        
        async Task ChangeName(string charNameOrNumber)
        {
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(charNameOrNumber != "" && validName.IsMatch(charNameOrNumber))
            {
                var old = Characters.Active[user].CharacterName;
                Characters.Active[user].CharacterName = charNameOrNumber;
                var update = Builders<StatBlock>.Update.Set(x => x.CharacterName, Characters.Active[user].CharacterName);
                await Program.UpdateSingleAsync(update, user);

                await RespondAsync($"{old} changed to {Characters.Active[user].CharacterName}", ephemeral: true);
                return;
            }
        }

        async Task Give(string charNameOrNumber, IUser target, List<StatBlock> characters)
        {
            if(charNameOrNumber == "" || target == null)
            {
                await RespondAsync("Please provide both a character name (or index number) and a target to give it to.");
                return;
            }

            var outVal = 0;
            var toUpper = charNameOrNumber.ToUpper();
            StatBlock statblock = null;
            if(int.TryParse(toUpper, out outVal) && outVal >= 0 && outVal < characters.Count)
                statblock = characters[outVal];
            else if(validName.IsMatch(toUpper))
                statblock = characters.FirstOrDefault(x => x.CharacterName.ToUpper() == toUpper)!;

            if(statblock != null)
            {
                lastGive[user] = statblock;
                var cb = new ComponentBuilder()
                    .WithButton("Accept", $"give:{user},{target.Id}")
                    .WithButton("Nope", "nope");

                await target.SendMessageAsync($"{Context.User.Username} would like to give you {statblock.CharacterName}", components: cb.Build());
            }
        }
        
        [SlashCommand("char", "add/remove/modify statblocks.")]
        public async Task CharCommand(CharacterCommand action, string charNameOrNumber = "", SheetType sheetType = SheetType.Pathbuilder, IAttachment attachment = null, IUser target = null)
        {
            var nameToUpper = charNameOrNumber.ToUpper();
            lastInputs[user] = nameToUpper;

            //find all documents that belong to user, load them into dictionary.
            Characters.Database[user] = new List<StatBlock>();
            var chars = await collection.FindAsync(x => x.Owner == user);
            var characters = chars.ToList();
            
            foreach(var statblock in characters)
                Characters.Database[user].Add(statblock);
        
            
            switch(action)
            {
                case CharacterCommand.Export:
                    await ExportCharacter(charNameOrNumber, characters);
                    return;
                case CharacterCommand.ChangeName:
                    await ChangeName(charNameOrNumber);              
                    return;
                case CharacterCommand.Give:
                    await Give(charNameOrNumber, target, characters);
                    return;
            }

           
            if(action == CharacterCommand.List)
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
            
            if(action == CharacterCommand.Set)
            {
                var outVal = 0;
                var toUpper = charNameOrNumber.ToUpper();

                if(int.TryParse(toUpper, out outVal) && outVal >= 0 && outVal < characters.Count)
                {
                    Characters.SetActive(user, characters[outVal]);
                    await RespondAsync($"{characters[outVal].CharacterName} set!", ephemeral: true);
                    return;
                }
                else if(validName.IsMatch(toUpper))
                {
                    var character = characters.FirstOrDefault(x => x.CharacterName.ToUpper() == toUpper);
                    if(character != null)
                    {
                        Characters.SetActive(user, character);
                        await RespondAsync($"{character.CharacterName} set!", ephemeral: true);
                        return;
                    }
                }
              
                await RespondAsync("Character not found", ephemeral: true);
                return;               
            }
            
            if(action == CharacterCommand.Add)
            {
                var stats = await UpdateStats(sheetType, attachment, name: charNameOrNumber);
                if(stats != null)
                {
                    stats.Owner = user;
                    await collection.InsertOneAsync(stats);
                    var json = JsonConvert.SerializeObject(stats, Formatting.Indented);
                    using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));
                    await FollowupWithFileAsync(stream, $"{stats.CharacterName}.txt", ephemeral: true);
                    return;

                }
                await FollowupAsync("Failed to create character", ephemeral: true);
            }

            if(action == CharacterCommand.Update)
            {
                if(!Characters.Active.ContainsKey(user))
                {
                    await RespondAsync("No active character", ephemeral: true);
                    return;
                }

                var stats = await UpdateStats(sheetType, attachment, Characters.Active[user], Characters.Active[user].CharacterName);
                if(stats != null)
                {
                    Characters.Active[user] = stats;
                    await Program.UpdateStatBlock(stats);
                    await FollowupAsync("Updated!", ephemeral: true);
                    return;
                }
                await FollowupAsync("Failed to update sheet", ephemeral: true);
            }

            if(action == CharacterCommand.New)
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

                if(Characters.Database[user].Count >= 35)
                {
                    await RespondAsync("You have too many characters. Delete one before making another.");
                    return;
                }

                StatBlock statblock = StatBlock.DefaultPathfinder(charNameOrNumber);
                
                statblock.Owner = user;         
                
                await collection.InsertOneAsync(statblock);

                var quote = Quotes.Get(charNameOrNumber); 

                var eb = new EmbedBuilder()
                    .WithColor(Color.DarkPurple)
                    .WithTitle($"New-Character({charNameOrNumber})")
                    .WithDescription($"{Context.User.Mention} has created a new character, {charNameOrNumber}.\n\n “{quote}”");
                
                Characters.Active[user] = statblock;
                
                await RespondAsync(embed: eb.Build());
                return;
            }

            if(action == CharacterCommand.Delete)
            {
                var outVal = 0;
                if(Characters.Database[user].Any(x => x.CharacterName.ToUpper() == charNameOrNumber.ToUpper()) || (int.TryParse(charNameOrNumber, out outVal) && outVal < Characters.Database[user].Count && outVal >= 0))
                    await RespondWithModalAsync<ConfirmModal>("confirm_delete");
                
                await RespondAsync("Character not found.", ephemeral: true);
                return;                   
            }           
        }

        [ComponentInteraction("give:*,*")]
        public async Task GiveCommand(ulong giver, ulong receiver)
        {
            if(lastGive.ContainsKey(giver) && lastGive[user] != null)
            {
                StatBlock stats = lastGive[giver];                      
                var original = stats.ToBsonDocument();
                var copy = BsonSerializer.Deserialize<StatBlock>(original);
                copy.Owner = receiver;
                await collection.InsertOneAsync(copy);
                await Context.User.SendMessageAsync($"{copy.CharacterName} copied");
                lastGive[user] = null;
                return;
            }

            await RespondAsync("Not found.", ephemeral: true);
        }

        [ModalInteraction("confirm_delete")]
        public async Task ConfirmDeleteChar(ConfirmModal modal)
        {
            if(modal.Confirm != "CONFIRM")
            {
                await RespondAsync("You didn't die. Try again. (make sure type 'CONFIRM' in all caps)", ephemeral: true);
                return;
            }
            var outVal = 0;
            string name = lastInputs[user];

            StatBlock character = null; 
            if(int.TryParse(name, out outVal))
            {
                character = Characters.Database[user][outVal];
                Characters.Database[user].RemoveAt(outVal);
            }              
            else
            {
                character = Characters.Database[user].FirstOrDefault(Characters.Database[user].FirstOrDefault(x => x.CharacterName == name))!;
                Characters.Database[user].Remove(character);
            }

            await collection.DeleteOneAsync(x => x.Id == character.Id);
            await RespondAsync($"{character.CharacterName} removed", ephemeral: true);
        }
     
        public async Task<StatBlock> UpdateStats(SheetType sheetType, IAttachment file, StatBlock stats = null, string name = "")
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(file.Url);
            var stream = new MemoryStream(data);

            if(stream != null)
            {
                if(file.Filename.ToUpper().Contains(".PDF") || file.Filename.ToUpper().Contains(".XML") || file.Filename.ToUpper().Contains(".TXT") || file.Filename.ToUpper().Contains(".JSON"))
                {
                    await RespondAsync("Updating sheet...", ephemeral: true);

                    if(stats == null) stats = StatBlock.DefaultPathfinder(name);

                    if(sheetType == SheetType.Mathfinder)
                        stats = JsonConvert.DeserializeObject<StatBlock>(Encoding.UTF8.GetString(data))!;
                    if(sheetType == SheetType.Pathbuilder)
                        stats = Utility.UpdateWithPathbuilder(stream, stats);
                    if(sheetType == SheetType.HeroLabs)
                        stats = Utility.UpdateWithHeroLabs(stream, stats);
                    if(sheetType == SheetType.PCGen)
                        stats = Utility.UpdateWithPCGen(stream, stats);
                    if(sheetType == SheetType.Mottokrosh)
                        stats = Utility.UpdateWithMotto(data, stats);
                    Console.WriteLine("Done!");

                    return stats;
                }
            }
            return null!;
        }   
    }
}