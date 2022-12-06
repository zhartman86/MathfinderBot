using System.Text;
using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using Discord.WebSocket;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using System.Linq.Expressions;
using System.Data;

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

            [ChoiceDisplay("Scribe (TXT)")]
            Scribe,

            [ChoiceDisplay("Mathfinder (JSON)")]
            Mathfinder,
        }

        static readonly Dictionary<ulong, ulong> secMessages = new Dictionary<ulong, ulong>();
        public static readonly Dictionary<string, ulong> challenges = new Dictionary<string, ulong>();
        static readonly Dictionary<ulong, ulong> interactionMessages = new Dictionary<ulong, ulong>();
        
        static Regex validName       = new Regex(@"[a-zA-Z' ]{3,75}");
        static Regex validItemInput  = new Regex(@"[a-zA-Z'0-9 :]{3,75}");

        ulong                       user;
        public  InteractionService  Service { get; set; }

        public override async void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            await Characters.GetCharacter(user);
        }

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

        async Task CharacterChangeName(string newName)
        {
            if(Characters.Active[user].CharacterName == "$GLOBAL")
            {
                await RespondAsync("Cannot rename the global space. If you wish to rename another character, first set it to active.", ephemeral: true);
                return;
            }

            if(newName != "" && validName.IsMatch(newName))
            {
                if(!Characters.Database[user].Any(x => x.CharacterName.ToUpper() == newName.ToUpper()))
                {
                    var oldName = Characters.Active[user].CharacterName;
                    Characters.Active[user].CharacterName = newName;
                    var update = Builders<StatBlock>.Update.Set(x => x.CharacterName, Characters.Active[user].CharacterName);
                    await Program.UpdateSingleStat(update, user);
                    await RespondAsync($"{oldName} changed to {Characters.Active[user].CharacterName}", ephemeral: true);
                    return;
                }
                else
                    await RespondAsync("Name already in use", ephemeral: true);

            }
            else
                await RespondAsync("Invalid name", ephemeral: true);
        }

        async Task CharacterDelete(string character, ulong user)
        {
            if(Characters.Database[user].Any(x => x.CharacterName.ToUpper() == character.ToUpper()))
            {
                var interactionId = Context.Interaction.Id;
                var cb = new ComponentBuilder()
                    .WithButton("DELETE", $"delete:{character},{interactionId}", style: ButtonStyle.Danger);
                await RespondAsync($"Press DELETE to permanently remove {character}",components: cb.Build(), ephemeral: true);
                var msg = await GetOriginalResponseAsync();
                interactionMessages[interactionId] = msg.Id;
                return;
            }                           
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
            var stats = await UpdateStats(sheetType, file, Characters.Active[user]);
            if(stats != null)
            {
                await Characters.SetActive(user, stats);
                await Program.UpdateStatBlock(stats);
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
                        stats = await Utility.UpdateWithPathbuilder(stream, stats);
                    if(sheetType == SheetType.HeroLabs)
                        stats = Utility.UpdateWithHeroLabs(stream, stats);
                    if(sheetType == SheetType.PCGen)
                        stats = Utility.UpdateWithPCGen(stream, stats);
                    if(sheetType == SheetType.Mottokrosh)
                        stats = Utility.UpdateWithMotto(data, stats);
                    if(sheetType == SheetType.Scribe)
                        stats = await Utility.UpdateWithScribe(stream, stats);
                    Console.WriteLine("Done!");

                    return stats;
                }
            }
            return null!;
        }

        [SlashCommand("char", "Modify statblocks.")]
        public async Task CharCommand(CharacterCommand action, string character = "", SheetType sheetType = SheetType.Pathbuilder, IAttachment file = null, IUser target = null)
        {
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
                case CharacterCommand.List:
                    await CharacterList(Characters.Database[user])      .ConfigureAwait(false);
                    return;
                case CharacterCommand.Delete:
                    await CharacterDelete(character, user)              .ConfigureAwait(false);
                    return;
            }       
        }

        [ComponentInteraction("delete:*,*")]
        public async Task ButtonPressedDelete(string name, ulong interactionId)
        {
            var user = Context.User.Id;
            Console.WriteLine(name);
           
            var stats = Characters.Database[user].FirstOrDefault(x => x.CharacterName.ToUpper() == name.ToUpper())!;
            if(stats != null)
            {
                Characters.Database[user].Remove(stats);
                await Program.DeleteOneStatBlock(stats);
                if(Characters.Active[user].Id == stats.Id)
                    await Characters.GetCharacter(user);
                await RespondAsync($"`{stats.CharacterName}` removed", ephemeral: true);
                if(interactionMessages.ContainsKey(interactionId))
                {
                    await Context.Channel.DeleteMessageAsync(interactionMessages[interactionId]);
                    interactionMessages.Remove(interactionId);
                }
                return;    
            }              
        }

        [UserCommand("Roll Off")]
        public async Task ContextCommandRollOff(IUser selectedUser)
        {
            if(user == selectedUser.Id)
            {
                var eb = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription($"\"{Quotes.GetDuality()}\"");
                await RespondAsync(embed: eb.Build(), ephemeral: true);
                return;
            }

            var mb = new ModalBuilder()
                .WithCustomId($"challenge:{selectedUser.Id}")
                .WithTitle("Create-an-Expression")
                .AddTextInput("Expression", "expr", value: "d100", maxLength: 50);
            await RespondWithModalAsync(mb.Build());
        }

        [ComponentInteraction("challenge:*,*,*")]
        public async Task ButtonPressedChallenge(ulong challenger, ulong challenged, string expr)
        {
            if(user == challenged)
            {
                var eb = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithFooter(expr);

                var challengerSb = new StringBuilder();
                var resultChallenger = await Utility.SecEvaluate(expr, challengerSb);
                challengerSb.AppendLine($"**Total:** __{resultChallenger}__");
                var challengerUser = await Program.GetUser(challenger);
                eb.AddField($"__Challenger__", challengerSb, inline: true);

                var challengedSb = new StringBuilder();
                var resultChallenged = await Utility.SecEvaluate(expr, challengedSb);
                challengedSb.AppendLine($"**Total:** __{resultChallenged}__");
                var challengedUser = await Program.GetUser(challenged);
                eb.AddField($"__Challenged__", challengedSb, inline: true);

                var challengerTotal = 0;
                var challengedTotal = 0;
                var split = resultChallenger.Split(';', StringSplitOptions.RemoveEmptyEntries);
                for(int i = 0; i < split.Length; i++)
                    challengerTotal += int.Parse(split[i]);
                split = resultChallenged.Split(';', StringSplitOptions.RemoveEmptyEntries);
                for(int i = 0; i < split.Length; i++)
                    challengedTotal += int.Parse(split[i]);

                var dChallenger = new DuelInfo { Duelist = challenger, Events = challengerSb.ToString(), Total = challengerTotal };
                var dChallenged = new DuelInfo { Duelist = challenged, Events = challengedSb.ToString(), Total = challengedTotal };

                var duel = new DuelEvent { Date = DateTime.Now, Expression = expr, Challenger = dChallenger, Challenged = dChallenged };
                Characters.Duels.Add(duel);
                await Program.InsertDuel(duel);

                eb.WithTitle($"{challengerUser.Username}    {(challengerTotal > challengedTotal ? ">" : challengerTotal < challengedTotal ? "<" : "=")}    {challengedUser.Username}");


                await Context.Channel.ModifyMessageAsync(challenges[$"{challenger}{challenged}"], m =>
                {
                    m.Components = new ComponentBuilder().Build();
                    m.Embed = eb.Build();
                });
                challenges.Remove($"{challenger}{challenged}");

                await CheckForSecrets(challenger);
                await CheckForSecrets(challenged);
            }

        }

        async Task SendSecret(int index, ulong duelist)
        {
            var dUser = await Program.GetUser(duelist);
            if(dUser != null)
            {
                var cb = new ComponentBuilder()
                    .WithButton(Secret.Secrets[index].Choices.Item1, $"sec:{index}")
                    .WithButton(Secret.Secrets[index].Choices.Item2, "sec:-1");

                var msg = await dUser.SendMessageAsync(Secret.Secrets[index].EventString, components: cb.Build());
                secMessages[user] = msg.Id;
            }
        }

        async Task CheckForSecrets(ulong duelist)
        {
            var duels = Characters.Duels.Where(x => duelist == x.Challenger.Duelist || duelist == x.Challenged.Duelist);
            var list = duels.ToList();
            Console.WriteLine($"Duels: {list.Count}");
            if(list.Count > 20)
            {
                Console.WriteLine("checking for secrets...");
                var r = new Random();
                if(r.Next(1, 21) == 20)
                    if(r.Next(1, 101) >= 92)
                    {
                        var secrets = await Program.Database.Secrets.FindAsync(x => x.Owner == duelist);
                        if(secrets.Any())
                            await SendSecret(new Random().Next(1, Secret.Secrets.Count + 1), duelist);
                        else
                            await SendSecret(0, duelist);                                    
                    }
            }
        }

        [ComponentInteraction("sec:*")]
        public async Task ButtonPressedSecret(int index)
        {
            if(index != -1)
            {
                Console.WriteLine(user);
                var secrets = await Program.Database.Secrets.FindAsync(x => x.Owner == user);
                if(secrets.Any())
                {
                    var sChar = secrets.ToList()[0];
                    if(!sChar.Secrets.Any(x => x.Name == Secret.Secrets[index].Name))
                    {
                        sChar.Secrets.Add(Secret.Secrets[index]);
                        await Program.UpdateSecrets(sChar);
                    }                
                }
                else
                {
                    var sChar = new SecretCharacter { Owner = user };
                    sChar.Secrets.Add(Secret.Secrets[index]);
                    await Program.InsertSecret(sChar);
                }
            }
            await RespondAsync(Secret.Secrets[index].Take, ephemeral: true);
            if(secMessages.ContainsKey(user))
            {
                await Context.Channel.DeleteMessageAsync(secMessages[user]);
                secMessages.Remove(user);
            }
                
        }



        [UserCommand("All-Time")]
        public async Task ContextCommandRollOffStats(IUser selectedUser)
        {
            var collection = await Program.Database.Duels.FindAsync(x =>  (x.Challenger.Duelist == user || x.Challenged.Duelist == user) && (x.Challenger.Duelist == selectedUser.Id || x.Challenged.Duelist == selectedUser.Id) );
            var duels = collection.ToList();

            var win = 0;
            var lose = 0;
            var draw = 0;
            for(int i = 0; i < duels.Count; i++)
            {
                if(duels[i].Challenger.Duelist == user)
                {
                    if(duels[i].Challenger.Total > duels[i].Challenged.Total) win++;
                    else if(duels[i].Challenger.Total < duels[i].Challenged.Total) lose++;
                    else draw++;
                }
                else
                {
                    if(duels[i].Challenger.Total > duels[i].Challenged.Total) lose++;
                    else if(duels[i].Challenger.Total < duels[i].Challenged.Total) win++;
                    else draw++;
                }
            }

            var eb = new EmbedBuilder()
                .WithTitle($"{Context.User.Username} ⚔️ {selectedUser.Username}")
                .WithColor(win > lose ? Color.Gold : Color.Red)
                .WithDescription($"All-Time: __**{duels.Count}**__")
                .AddField("Wins", win, inline: true)
                .AddField("Losses", lose, inline: true)
                .AddField("Draws", draw, inline: true);

            await RespondAsync(embed: eb.Build());
        }

 
        [SlashCommand("sec", "...")]
        public async Task SecretCommand()
        {
            var collection = await Program.Database.Secrets.FindAsync(x => x.Owner == user);
            if(collection.Any())
            {
                var sChar = collection.ToList()[0];
                var smb = new SelectMenuBuilder();
                for(int i = 1; i < sChar.Secrets.Count; i++)
                    smb.AddOption(sChar.Secrets[i].Name, sChar.Secrets[i].Name);
                var cb = new ComponentBuilder()
                    .WithSelectMenu(smb);
                await RespondAsync(components: cb.Build(), ephemeral:true);
                return;
            }
            await RespondAsync("...", ephemeral: true);
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