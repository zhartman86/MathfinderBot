using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Pathfinder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;

namespace MathfinderBot
{
    public class Program
    {
        internal static MongoHandler dbClient;
        public static DiscordSocketClient client;
        public static InteractionService interactionService;
        public static LoggingService logger;

        public static PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        static readonly Regex validExpr = new Regex(@"^[-0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,400}$");
        static readonly Regex validName = new Regex(@"[a-zA-z-' ]{1,50}");
        static readonly Regex validRollOff = new Regex(@"^([1-9]{1}[0-9]{0,2})?d(([1-9]{1}[0-9]{1,2})|[2-9])$");

        public static MongoHandler Database { get { return dbClient; } }

        static readonly string[] boringText = new string[]
        {
            "# wants to duel. What say you, $?",
            "$ has been challenged to a duel.",
            "# has thrown their gauntlet down. What say you, $?",
            "Care for a duel, $?",
            "$, you've been challenged.",
            "# would like to duel $.",
            "Wanna go, $?",
            "# wants to fight you, $.",
            "$, Let's go.",
            "A challenge against $ has been made!",
            "The time is nigh, $.",
            "$ has been called out!",
            "Fancy a Roll-off, $?",
            "You've been beckoned, $.",
            "# has taken arms against $.",
            "A Roll-off is requested, $."
        };

        static readonly string[] boringButton = new string[]
        {
            "Sure—why not?",
            "Let's do it.",
            "As good as any.",
            "Sure.",
            "If you insist.",
            "Let's go.",
            "En garde.",
            "Okay.",
            "OK",
            "CONFIRM",
            "Yes.",
            "Cancel #",
            "*Shrug*",
            "Agreed.",
            "Rodger.",           
        };

        string BoringText   { get { return boringText[new Random().Next(0, boringText.Length)]; } }
        string BoringButton { get { return boringButton[new Random().Next(0, boringButton.Length)]; } }

        public static Task Main(string[] args) => new Program().MainAsync();
        public async Task MainAsync()
        {
            var file = File.ReadAllText(@"C:\File.txt");
            var fileTwo = File.ReadAllText(@"C:\FileTwo.txt");

            //db stuff
            
            var settings = MongoClientSettings.FromConnectionString(file);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            dbClient = new MongoHandler(new MongoClient(settings), "Mathfinder");

            var list = dbClient.ListDatabases();
            foreach(var db in list)
                Console.WriteLine(db);

            //discord server stuff
            using var services = CreateServices();
            interactionService = services.GetRequiredService<InteractionService>();
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            client = services.GetRequiredService<DiscordSocketClient>();
            logger = new LoggingService(client);
            client.Ready += ReadyAsync;
            client.ModalSubmitted += ModalSubmitted;

            await client.LoginAsync(TokenType.Bot, fileTwo);
            await client.StartAsync();

            await HandleTimerEvents();
            await Task.Delay(Timeout.Infinite);
        }

        async Task ReadyAsync() =>
            await interactionService.RegisterCommandsGloballyAsync();

        async Task<List<string>> ReadExpressionLines(string s)
        {
            using var reader = new StringReader(s);
            var lines = new List<string>();

            var line = await reader.ReadLineAsync();
            while(line != null)
            {
                if(validExpr.IsMatch(line))
                    lines.Add(line);
                line = await reader.ReadLineAsync();
            }
            return lines;
        }

        async Task<ExprRow> ParseExpressions(string name, List<string> exprs)
        {
            var task = Task.Run(() =>
            {
                var row = new ExprRow() { RowName = name };
                for(int i = 0; i < exprs.Count; i++)
                {
                    var split = exprs[i].Split(':');
                    if(split.Length == 1)
                        row.Set.Add(new Expr() { Name = split[0], Expression = split[0] });
                    else
                        row.Set.Add(new Expr() { Name = split[0], Expression = split[1] });
                }
                return row;
            });
            return await task;
        }

        async Task HandleTimerEvents()
        {
            while(await timer.WaitForNextTickAsync())
                dbClient.ProcessQueue();
        }

        public static ServiceProvider CreateServices()
        {
            var isConfig = new InteractionServiceConfig() { UseCompiledLambda = true };
            var dcConfig = new DiscordSocketConfig() { GatewayIntents = GatewayIntents.AllUnprivileged & (~GatewayIntents.GuildScheduledEvents) & (~GatewayIntents.GuildInvites) };

            return new ServiceCollection()
                .AddSingleton(dcConfig)
                .AddSingleton(isConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandHandler>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))

                .BuildServiceProvider();
        }

        public static async Task<IUser> GetUser(ulong id) { return await client.GetUserAsync(id); }

        public static IMongoCollection<StatBlock> GetStatBlocks() { return dbClient.StatBlocks; }

        public async static Task InsertSecret(SecretCharacter secretChar)  => await Task.Run(() => { dbClient.AddToQueue(new InsertOneModel<SecretCharacter>(secretChar)); }).ConfigureAwait(false);
        public async static Task UpdateSecrets(SecretCharacter secretChar) => await Task.Run(() => { dbClient.AddToQueue(new ReplaceOneModel<SecretCharacter>(Builders<SecretCharacter>.Filter.Eq(x => x.Owner, secretChar.Owner), secretChar)); }).ConfigureAwait(false);
        public async static Task InsertDuel(DuelEvent duel)          => await Task.Run(() => { dbClient.AddToQueue(new InsertOneModel<DuelEvent>(duel)); }).ConfigureAwait(false);
        public async static Task InsertStatBlock(StatBlock stats)    => await Task.Run(() => { dbClient.AddToQueue(new InsertOneModel<StatBlock>(stats)); }).ConfigureAwait(false);
        public async static Task DeleteOneStatBlock(StatBlock stats) => await Task.Run(() => { dbClient.AddToQueue(new DeleteOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, stats.Id))); }).ConfigureAwait(false);
        public async static Task UpdateStatBlock(StatBlock stats)    => await Task.Run(() => { dbClient.AddToQueue(new ReplaceOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, stats.Id), stats)); }).ConfigureAwait(false);
        public async static Task UpdateSingleStat(UpdateDefinition<StatBlock> update, ulong user) =>  await Task.Run(() => { dbClient.AddToQueue(new UpdateOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, Characters.Active[user].Id), update)); }).ConfigureAwait(false);

        public async Task ModalSubmitted(SocketModal modal)
        {
            var user = modal.User.Id;
            var components = modal.Data.Components.ToList();

            switch(modal.Data.CustomId)
            {
                case "new_row":
                    var row = await ParseExpressions(components[0].Value.ToUpper(), await ReadExpressionLines(components[1].Value));
                    Characters.Active[user].AddExprRow(row);
                    await modal.RespondAsync($"{row.RowName} updated", ephemeral: true);
                    return;
                case string newItem when newItem.Contains("set_expr:"):
                    var varName = modal.Data.CustomId.Split(':')[1];
                    Characters.Active[user].AddExpr(varName, components[0].Value);
                    await modal.RespondAsync($"{varName} updated", ephemeral: true);
                    return;
                case string newItem when newItem.Contains("base_item:"):
                    var item = modal.Data.CustomId.Split(':')[1];
                    var invItem = ParseInvItem($"{(components[0].Value != "" && validName.IsMatch(components[0].Value) ? components[0].Value : item)}:{components[1].Value}:{components[2].Value}:{components[3].Value}:{components[4].Value}");
                    Characters.Active[user].InventoryAdd(invItem);
                    await modal.RespondAsync($"{invItem.Name} added", ephemeral: true);
                    return;
                case string newItem when newItem.Contains("edit_item:"):
                    var index = int.Parse(modal.Data.CustomId.Split(':')[1]);
                    var edited = ParseInvItem($"{(components[0].Value != "" && validName.IsMatch(components[0].Value) ? components[0].Value : Characters.Active[user].Inventory[index].Name)}:{components[1].Value}:{components[2].Value}:{components[3].Value}:{components[4].Value}");
                    Characters.Active[user].Inventory[index] = edited;
                    await modal.RespondAsync($"{edited.Name} changed", ephemeral: true);
                    return;
                case string newItem when newItem.Contains("challenge:"):

                    var expr = components[0].Value.Replace(" ", "");
                    if(validRollOff.IsMatch(expr))
                    {
                        var uid = ulong.Parse(modal.Data.CustomId.Split(':')[1]);
                        var challenged = await GetUser(uid);

                        //set dueling text. Use the boring ones more often                        
                        string duelText;
                        string buttonText;
                        if(new Random().Next(0, 100) >= 73)
                        {
                            var d      = Quotes.GetDuel();
                            duelText   = d.Item1.Replace("#", modal.User.Username).Replace("$", challenged.Mention);
                            buttonText = d.Item2.Replace("#", modal.User.Username);
                        }
                        else
                        {
                            buttonText = BoringButton.Replace("#", modal.User.Username);
                            duelText   = BoringText.Replace("#", modal.User.Username).Replace("$", challenged.Mention);
                        }

                        var cb = new ComponentBuilder()
                            .WithButton($"{buttonText}", $"challenge:{user},{uid},{expr}".Replace(" ", ""), ButtonStyle.Danger);

                        await modal.RespondAsync($"*{duelText}*\r\n\r\n`{expr}`", components: cb.Build());
                        var msg = await modal.GetOriginalResponseAsync();
                        Character.challenges[$"{modal.User.Id}{uid}"] = msg.Id;
                    }
                    else
                        await modal.RespondAsync("invalid dice expression. Use `XXXXdYYYY`", ephemeral: true);                  
                    break;
            }
        }

        public static InvItem ParseInvItem(string item, string baseItem = "")
        {
            var split   = item.Split(':');
            var invItem = new InvItem()
            {
                Base     = baseItem,
                Name     = split[0],
                Quantity = split.Length > 0 ? (int.TryParse(split[1], out int outInt) ? outInt : 0) : 0,
                Value    = split.Length > 1 ? (decimal.TryParse(split[2], out decimal outDec) ? outDec : 0m) : 0m,
                Weight   = split.Length > 2 ? (decimal.TryParse(split[3], out outDec) ? outDec : 0m) : 0m,
                Note     = split.Length > 3 ? split[4] : ""
            };
            return invItem;
        }

    }
}

