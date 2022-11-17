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
using System.Net.Mail;

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

        public enum InventoryAction
        {
            [ChoiceDisplay("Add-New")]
            Add,

            [ChoiceDisplay("Add-List")]
            AddList,

            Edit,
            Import,
            Export,
            Remove,
            List
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

        static Dictionary<ulong, StatBlock> lastGive        = new Dictionary<ulong, StatBlock>();
        static Dictionary<ulong, string>    lastInputs      = new Dictionary<ulong, string>();
        static Regex                        validName       = new Regex(@"[a-zA-Z' ]{3,75}");
        static Regex                        validItemInput  = new Regex(@"[a-zA-Z'0-9 :]{3,75}");

        ulong                       user;
        public  InteractionService  Service { get; set; }
        private CommandHandler      handler;

        public Character(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
        }

        //Character
        async Task CharacterAdd(string character, SheetType sheetType, IAttachment file)
        {

            if(Characters.Database[user].Count == 0)
            {
                var global = new StatBlock() { Owner = user, CharacterName = "$GLOBAL" };
                await Program.InsertStatBlock(global);
            }

            if(!validName.IsMatch(character))
            {
                await RespondAsync("Invalid character name.", ephemeral: true);
                return;
            }




            var stats = await UpdateStats(sheetType, file, name: character);
            if(stats != null)
            {
                stats.Owner = user;
                await Program.InsertStatBlock(stats);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented);
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));
                await FollowupWithFileAsync(stream, $"{stats.CharacterName}.txt", ephemeral: true);
                return;
            }
            else
                await FollowupAsync("Failed to create character", ephemeral: true);
        }

        async Task CharacterChangeName(string character)
        {
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(character != "" && validName.IsMatch(character))
            {
                if(!Characters.Database[user].Any(x => x.CharacterName.ToUpper() == character.ToUpper()))
                {
                    var old = Characters.Active[user].CharacterName;
                    Characters.Active[user].CharacterName = character;
                    var update = Builders<StatBlock>.Update.Set(x => x.CharacterName, Characters.Active[user].CharacterName);
                    Program.UpdateSingle(update, user);
                    await RespondAsync($"{old} changed to {Characters.Active[user].CharacterName}", ephemeral: true);
                    return;
                }
                else
                    await RespondAsync("Name already in use", ephemeral: true);
                
            }
        }

        async Task CharacterDelete(string character, ulong user)
        {
            var outVal = 0;
            if(Characters.Database[user].Any(x => x.CharacterName.ToUpper() == character.ToUpper()) || (int.TryParse(character, out outVal) && outVal < Characters.Database[user].Count && outVal >= 0))
                await RespondWithModalAsync<ConfirmModal>("confirm_delete");

            await RespondAsync("Character not found.", ephemeral: true);
            return;
        }

        async Task CharacterExport(string character, List<StatBlock> characters)
        {
            var outVal = 0;
            var toUpper = character.ToUpper();
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
            await RespondAsync($"{character} not found.", ephemeral: true);
            return;
        }
        
        async Task CharacterGive(string character, IUser target, List<StatBlock> characters)
        {
            if(character == "" || target == null)
            {
                await RespondAsync("Please provide both a character name (or index number) and a target to give it to.");
                return;
            }

            var outVal = 0;
            var toUpper = character.ToUpper();
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

        async Task CharacterList(List<StatBlock> chars)
        {
            var task = Task.Run(() =>
            {
                var sb = new StringBuilder();

                if(chars.Count == 0) sb.AppendLine("You don't have any characters.");
                for(int i = 0; i < chars.Count; i++)
                    sb.AppendLine($"{i} — {chars[i].CharacterName}");

                return $"```{sb}```";
            });
            await RespondAsync(await task, ephemeral: true);

        }

        async Task CharacterNew(string character, ulong user)
        {
            if(!validName.IsMatch(character))
            {
                await RespondAsync("Invalid character name.", ephemeral: true);
                return;
            }

            if(Characters.Database[user].Any(x => x.CharacterName.ToUpper() == character.ToUpper()))
            {
                await RespondAsync($"{character} already exists.", ephemeral: true);
                return;
            }

            if(Characters.Database[user].Count >= 30)
            {
                await RespondAsync("You have too many characters. Delete one before making another.");
                return;
            }
            if(Characters.Database[user].Count == 0)
            {
                var global = new StatBlock() { Owner = user, CharacterName = "$GLOBAL" };
                await Program.InsertStatBlock(global);
            }

            StatBlock statblock = StatBlock.DefaultPathfinder(character);

            statblock.Owner = user;
            await Program.InsertStatBlock(statblock);

            var quote = Quotes.Get(character);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithTitle($"New-Character({character})")
                .WithDescription($"{Context.User.Mention} has created a new character, {character}.\n\n “{quote}”");

            Characters.Active[user] = statblock;

            await RespondAsync(embed: eb.Build());
            return;
        }

        async Task CharacterSet(string character, List<StatBlock> chars)
        {
            var index = -1;

            if(int.TryParse(character, out int outVal) && outVal >= 0 && outVal < chars.Count)
                index = outVal;
            else
                index = chars.FindIndex(x => x.CharacterName.ToUpper() == character.ToUpper());

            if(index != -1)
            {
                await Characters.SetActive(user, chars[index]);
                await RespondAsync($"{Characters.Active[user].CharacterName} set", ephemeral: true);
            }
            else
                await RespondAsync("Character not found", ephemeral: true);
        }

        async Task CharacterUpdate(SheetType sheetType, IAttachment file, ulong user)
        {
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var stats = await UpdateStats(sheetType, file, Characters.Active[user]);
            if(stats != null)
            {
                Characters.SetActive(user, stats);
                Program.UpdateStatBlock(stats);
                await FollowupAsync("Updated!", ephemeral: true);
            }
            else
                await FollowupAsync("Failed to update sheet", ephemeral: true);
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

        [SlashCommand("char", "Modify statblocks.")]
        public async Task CharCommand(CharacterCommand action, string character = "", SheetType sheetType = SheetType.Pathbuilder, IAttachment file = null, IUser target = null)
        {
            var nameToUpper = character.ToUpper();
            lastInputs[user] = nameToUpper;

            //find all documents that belong to user, load them into dictionary.
            var coll = await Program.GetStatBlocks().FindAsync(x => x.Owner == user);
            Characters.Database[user] = coll.ToList();
            var chars = Characters.Database[user];


            switch(action)
            {
                case CharacterCommand.Set:
                    await CharacterSet(character, chars)                .ConfigureAwait(false);
                    return;
                case CharacterCommand.Export:
                    await CharacterExport(character, chars)             .ConfigureAwait(false);
                    return;
                case CharacterCommand.Add:
                    await CharacterAdd(character, sheetType, file)      .ConfigureAwait(false);
                    return;
                case CharacterCommand.New:
                    await CharacterNew(character, user)                 .ConfigureAwait(false);
                    return;
                case CharacterCommand.ChangeName:
                    await CharacterChangeName(character)                .ConfigureAwait(false);
                    return;
                case CharacterCommand.Update:
                    await CharacterUpdate(sheetType, file, user)        .ConfigureAwait(false);
                    return;
                case CharacterCommand.Give:
                    await CharacterGive(character, target, chars)       .ConfigureAwait(false);
                    return;
                case CharacterCommand.List:
                    await CharacterList(Characters.Database[user])      .ConfigureAwait(false);
                    return;
                case CharacterCommand.Delete:
                    await CharacterDelete(character, user)              .ConfigureAwait(false);
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
                await Program.InsertStatBlock(copy);
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

            await Program.GetStatBlocks().DeleteOneAsync(x => x.Id == character.Id);
            await RespondAsync($"{character.CharacterName} removed", ephemeral: true);
        }

        //Inventory
        //NAME:QTY:VALUE:WEIGHT:NOTE
        async Task<InvItem> ParseItem(string itemString)
        {
            var task = Task.Run(() =>
            {
                var split = itemString.Split(':');

                var item = new InvItem()
                {
                    Name     = split[0],
                    Quantity = split.Length > 0 ? int.TryParse(split[3], out int outInt) ? outInt : 1 : 1,
                    Value    = split.Length > 1 ? decimal.TryParse(split[2], out decimal outDec) ? outDec : 0m : 0m,
                    Weight   = split.Length > 2 ? decimal.TryParse(split[1], out outDec) ? outDec : 0m : 0m,
                    Note     = split.Length > 3 ? "" : split[4],                                                  
                };
                return item;
            });
            return await task;
        }

        async Task<int> GetItem(string item)
        {
            var task = Task.Run(() =>
            {
                var index = -1;
                if(item != "")
                {
                    var outVal = 0;
                    index = Characters.Active[user].Inventory.FindIndex(x => x.Name == item);
                    if(int.TryParse(item, out outVal) && outVal >= 0 && outVal < Characters.Active[user].Inventory.Count)
                        index = outVal;
                    return index;
                }
                return index;
            });
            return await task;           
        }

        async Task InventoryAdd(string item)
        {
            if(item != "")
            {
                var invItem = await ParseItem(item);
                if(invItem != null)
                    Characters.Active[user].InventoryAdd(invItem);
            }
            else
                await RespondWithModalAsync<AddInvModal>("new_item");
        }

        async Task InventoryEdit(string item)
        {
            var index = await GetItem(item);
            if(index != -1)
            {
                var itm = Characters.Active[user].Inventory[index];
                var mb = new ModalBuilder()
                    .WithCustomId($"edit_item:{index}")
                    .WithTitle($"Edit: {itm.Name}")
                    .AddTextInput("Name", "item_name", value: itm.Name, maxLength: 50)
                    .AddTextInput("Quantity", "item_qty", value: itm.Quantity.ToString())                  
                    .AddTextInput("Weight", "item_weight", value: itm.Weight.ToString(), maxLength: 20)
                    .AddTextInput("Value", "item_value", value: itm.Value.ToString())                    
                    .AddTextInput("Notes", "item_notes", TextInputStyle.Paragraph, required: false, value: itm.Note);

                await RespondWithModalAsync(mb.Build());
            }                   
        }
        
        async Task InventoryRemove(string item)
        {
            var index = await GetItem(item);

            if(index != -1)
            {
                await RespondAsync($"{Characters.Active[user].Inventory[index].Name} removed.");
                Characters.Active[user].Inventory.RemoveAt(index);
            }
            else
                await RespondAsync("Name or index not found", ephemeral: true);
            
        }

        async Task InventoryImport(IAttachment file)
        {
            if(file != null)
            {
                using var client = new HttpClient();
                var data = await client.GetStringAsync(file.Url);

                if(data != null && file.Filename.ToUpper().Contains(".JSON"))
                {
                    var inv = JsonConvert.DeserializeObject<List<InvItem>>(data);
                    if(inv != null)
                        Characters.Active[user].InventorySet(inv);
                    await RespondAsync("Inventory updated", ephemeral: false);
                    return;
                }
            }
            await RespondAsync("File is empty or incorrectly formatted.", ephemeral: true);
        }

        [SlashCommand("inv", "Modify active character's inventory")]
        public async Task CharInventoryCommand(InventoryAction action, string item = "", IAttachment file = null)
        {
            if(!Characters.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            switch(action)
            {              
                case InventoryAction.Add:
                    await InventoryAdd(item);
                    return;
                case InventoryAction.AddList:
                    await RespondWithModalAsync<AddInvListModal>("new_item_list");
                    return;
                case InventoryAction.Edit:
                    await InventoryEdit(item);
                    return;
                case InventoryAction.Remove:
                    await InventoryRemove(item);
                    return;
                case InventoryAction.List:
                    await RespondWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(Characters.Active[user].InventoryOut())), $"{Characters.Active[user].CharacterName}'s Inventory.txt", ephemeral: true);
                    return;
                case InventoryAction.Export:
                    var json = JsonConvert.SerializeObject(Characters.Active[user].Inventory);
                    await RespondWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), $"{Characters.Active[user].CharacterName}'s Inventory.json", ephemeral: true);
                    return;
                case InventoryAction.Import:
                    await InventoryImport(file);
                    return;

            }
        }

        [ModalInteraction("new_item")]
        public async Task NewItemModal(AddInvModal modal)
        {
            var newItem = new InvItem()
            {
                Name = modal.Name,
                Quantity = int.TryParse(modal.Quantity, out int outInt) ? outInt : 0,
                Value = decimal.TryParse(modal.Value, out decimal outDec) ? outDec : 0m,
                Weight = decimal.TryParse(modal.Weight, out outDec) ? outDec : 0m,
                Note = modal.Note
            };
            Characters.Active[user].InventoryAdd(newItem);
            await RespondAsync($"{newItem.Name} added to inventory", ephemeral: true);
        }

        [ModalInteraction("new_item_list")]
        public async Task NewItemListModal(AddInvListModal modal)
        {
            var items = new List<InvItem>();
            
            using var reader = new StringReader(modal.List);
            var str = reader.ReadLine();           
            while(str != null)
            {
                if(validItemInput.IsMatch(str))
                    items.Add(await ParseItem(str));
                str = reader.ReadLine();
            }
            Characters.Active[user].InventoryAdd(items);
            await RespondAsync($"{items.Count} items added", ephemeral: true);
        }

    }
}