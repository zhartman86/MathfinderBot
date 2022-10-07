using Discord.Interactions;
using Discord;
using Gellybeans.Pathfinder;
using MongoDB.Driver;
using System.Text;
using System.Linq;


namespace MathfinderBot
{
    public class Inventory : InteractionModuleBase
    {
        public enum InventoryAction
        {
            [ChoiceDisplay("Add-Custom")]
            Add,
            
            [ChoiceDisplay("Add-From-Database")]
            AddFromDB,
            
            Import,
            Export,
            Remove,
            List
        }
        
        
        ulong user;
        CommandHandler handler;
        IMongoCollection<StatBlock> collection;

        static Dictionary<ulong, string> lastInputs = new Dictionary<ulong, string>();

        public Inventory(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            collection = Program.database.GetCollection<StatBlock>("statblocks");

           
        }

        [SlashCommand("inv", "Inventory mangement")]
        public async Task InventoryCommand(InventoryAction action, string item = "", int qty = 1, IAttachment attachment = null)
        {
            if(!Characters.Active.ContainsKey(user) || Characters.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(action == InventoryAction.Add)
            {
                await Add(user, item, qty);
                return;
            }
                

            if(action == InventoryAction.AddFromDB)
            {
                await AddFromDB(user, item);
                return;
            }
                
            
            if(action == InventoryAction.Import)
            {
                if(attachment == null)
                {
                    await RespondAsync("No attachment. Add one using the `attachment` field");
                    return;
                }

                using var client    = new HttpClient();
                var data            = await client.GetByteArrayAsync(attachment.Url);
                var stream          = new MemoryStream(data);

                if(stream != null)
                {                    
                    if(attachment.ContentType.Contains("text/plain"))
                    {
                        var str = Encoding.Default.GetString(data);
                        var reader = new StringReader(str);
                        var itemList = new List<Item>();

                        while(true)
                        {
                            var line = await reader.ReadLineAsync();
                            if(line != null)
                            {
                                if(string.IsNullOrEmpty(line))
                                    continue;
                                
                                var split = line.Split(new char[] { ':', ',', '\t' }, options: StringSplitOptions.RemoveEmptyEntries);

                                decimal outVal;

                                itemList.Add(new Item()
                                {
                                    Name    = split[0],
                                    Weight  = split.Length < 1 ? 0 : decimal.TryParse(split[1], out outVal) ? Math.Round(outVal, 5) : 0,
                                    Value   = split.Length < 2 ? 0 : decimal.TryParse(split[2], out outVal) ? Math.Round(outVal, 5) : 0
                                });
                            }
                            else break;
                        }
                        Characters.Active[user].Inventory = itemList;

                        var update = Builders<StatBlock>.Update.Set(x => x.Inventory, Characters.Active[user].Inventory);
                        await Program.UpdateSingleAsync(update, user);

                        await RespondAsync("Inventory updated", ephemeral: true);
                    }
                }                
            }
            
            if(action == InventoryAction.Export)
            {
                var sb = new StringBuilder();

                var itemList = Characters.Active[user].Inventory;
                for(int i = 0; i < itemList.Count; i++)
                    sb.AppendLine($"{itemList[i].Name}\t\t{itemList[i].Weight}\t\t{itemList[i].Value}");

                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
                await RespondWithFileAsync(stream, $"Inventory.{Characters.Active[user].CharacterName}.txt", ephemeral: true);
                return;
            }

            if(action == InventoryAction.Remove)
            {
                await Remove(user, item, qty);
                return;
            }
                
            if(action == InventoryAction.List)
            {
                await List(user);
                return;
            }
                
        }

        [ModalInteraction("add_inv")]
        public async Task AddInv(AddInvModal modal)
        {
            if(modal.List == "")
            {
                await RespondAsync("List is empty", ephemeral: true);
                return;
            }

            var itemList = new List<Item>();
            var reader = new StringReader(modal.List);
            while(true)
            {
                var line = await reader.ReadLineAsync();
                if(line != null)
                {
                    if(string.IsNullOrEmpty(line))
                        continue;

                    var split = line.Split(':', options: StringSplitOptions.RemoveEmptyEntries);

                    decimal outVal;
                    itemList.Add(new Item()
                    {
                        Name    = split[0],
                        Weight  = split.Length < 1 ? 0 : decimal.TryParse(split[1], out outVal) ? Math.Round(outVal, 5) : 0,
                        Value   = split.Length < 2 ? 0 : decimal.TryParse(split[2], out outVal) ? Math.Round(outVal, 5) : 0
                    });
                }
                else break;
            }
            
            Characters.Active[user].Inventory.AddRange(itemList);

            var update = Builders<StatBlock>.Update.Set(x => x.Inventory, Characters.Active[user].Inventory);
            await Program.UpdateSingleAsync(update, user);

            var sb = new StringBuilder();

            sb.AppendLine("```");
            sb.AppendLine($"|{"NAME", -27} |{"WEIGHT", -10} |{"VALUE", -8}");
            for(int i = 0; i < itemList.Count; i++)
                sb.AppendLine($"|{itemList[i].Name,-17} |{itemList[i].Weight, -10} |{itemList[i].Value, -8}");
            sb.AppendLine("```");

            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithDescription(sb.ToString());

            await RespondAsync(embed: eb.Build(), ephemeral: true);         

        }

        [RequireRole("DM")]
        [SlashCommand("dm-inv", "DM inventory management")]
        public async Task InventoryDMCommand(IUser target, InventoryAction action, string item, int qty = 1)
        {
            if(!Characters.Active.ContainsKey(target.Id))
            {
                await RespondAsync("No active character found", ephemeral: true);
                return;
            }
            
            if(action == InventoryAction.Add)
                await Add(target.Id, item, qty);
            if(action == InventoryAction.Remove)
                await Remove(target.Id, item, qty);
            if(action == InventoryAction.List)
                await List(target.Id);     
        }

        async Task List(ulong userId) => await RespondAsync(Characters.Active[userId].InventoryOut(), ephemeral: true);

        async Task Add(ulong userId, string item = "", int qty = 1)
        {
            if(item == "")
            {
                await RespondWithModalAsync<AddInvModal>($"add_inv");
                return;
            }
            Console.WriteLine(item);
            var split = item.Split(':', options: StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(split.Length);
            decimal outVal;
            var newItem = new Item()
            {
                Name = split[0],
                Weight = split.Length < 2 ? 0 : decimal.TryParse(split[1], out outVal) ? Math.Round(outVal, 5) : 0,
                Value = split.Length < 3 ? 0 : decimal.TryParse(split[2], out outVal) ? Math.Round(outVal, 5) : 0
            };

            for(int i = 0; i < qty; i++)
                Characters.Active[userId].Inventory.Add(newItem);


            var update = Builders<StatBlock>.Update.Set(x => x.Inventory, Characters.Active[userId].Inventory);
            await Program.UpdateSingleAsync(update, userId);
            await RespondAsync($"{newItem.Name} added", ephemeral: true);
            return;

        }

        async Task AddFromDB(ulong userId, string index)
        {
            var outVal = -1;
            if(!int.TryParse(index, out outVal))
            {
                await RespondAsync($"Invalid index: {index}", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.Items.Count)
            {
                var item = DataMap.Items[outVal];
                await Add(userId, $"{item.Name}:{item.Weight}:{item.Value}");
            }            
        }

        async Task Remove(ulong userId, string item = "", int qty = 1)
        {
            if(item == "")
            {
                await RespondAsync("No item selected. Use the `itemName` to select a number or name", ephemeral: true);
                return;
            }

            var outVal = 0;
            if(int.TryParse(item, out outVal) && outVal < Characters.Active[userId].Inventory.Count && outVal >= 0)
            {
                var temp = Characters.Active[userId].Inventory[outVal];
                Characters.Active[userId].Inventory.RemoveAt(outVal);

                var update = Builders<StatBlock>.Update.Set(x => x.Inventory, Characters.Active[userId].Inventory);
                await Program.UpdateSingleAsync(update, userId);

                await RespondAsync($"{temp.Name} removed", ephemeral: true);
                return;
            }

            int count = 0;
            for(int i = 0; i < qty; i++)
            {
                if(Characters.Active[userId].Inventory.Any(x => x.Name == item))
                {
                    count++;
                    var temp = Characters.Active[userId].Inventory.First(x => x.Name == item);
                    Characters.Active[userId].Inventory.Remove(temp);
                }
                else break;
            }

            if(count > 0)
            {
                var update = Builders<StatBlock>.Update.Set(x => x.Inventory, Characters.Active[userId].Inventory);
                await Program.UpdateSingleAsync(update, userId);

                await RespondAsync($"{count} {item} removed", ephemeral: true);
                return;
            }

            await RespondAsync("Item name or index not found");
            return;
        }
    }
    
   
}
