using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using System.Xml.Linq;

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

        public static IDatabase Database { get { return dbClient; } }

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

        string BoringText { get { return boringText[new Random().Next(0, boringText.Length)]; } }
        string BoringButton { get { return boringButton[new Random().Next(0, boringButton.Length)]; } }



        public static Task Main(string[] args) => new Program().MainAsync(args);
        public async Task MainAsync(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors();

            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if(app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            var rest = Task.Run(() => { app.RunAsync(); });




            var file = File.ReadAllText(@"C:\File.txt");
            var fileTwo = File.ReadAllText(@"C:\FileTwo.txt");

            //db stuff            
            var settings = MongoClientSettings.FromConnectionString(file);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            dbClient = new MongoHandler(new MongoClient(settings), "Mathfinder");

            var list = dbClient.ListDatabases();
            foreach(var db in list)
                Console.WriteLine(db);


            //DONT DELETE THIS->EXAMPLE OF HOW TO ADD A NEW FIELD TO ALL DOCUMENTS IN A TABLE
            //var b = Builders<StatBlock>.SetFields;
            //var update = b.Set(x => x.Vars.Values, new FunctionValue(Array.Empty<string>(), ""));
            
           
            //var f = Builders<StatBlock>.Filter.Empty;
            //var r = await dbClient.StatBlocks.UpdateManyAsync(f, update);
            //Console.WriteLine(r.MatchedCount);


            //discord server stuff
            using var services = CreateServices();
            interactionService = services.GetRequiredService<InteractionService>();
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            client = services.GetRequiredService<DiscordSocketClient>();
            logger = new LoggingService(client);
            client.Ready += ReadyAsync;
            client.ModalSubmitted += ModalSubmitted;

            await client.LoginAsync(Discord.TokenType.Bot, fileTwo);
            await client.StartAsync();

            await HandleTimerEvents();



            await Task.Delay(-1);

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
        public static IMongoCollection<XpObject> GetXp() { return dbClient.XpObjects; }



        public async static Task InsertStatBlock(StatBlock stats) => await Task.Run(() => { dbClient.AddToQueue(new InsertOneModel<StatBlock>(stats)); }).ConfigureAwait(false);
        public async static Task DeleteOneStatBlock(StatBlock stats) => await Task.Run(() => { dbClient.AddToQueue(new DeleteOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, stats.Id))); }).ConfigureAwait(false);
        public async static Task UpdateStatBlock(StatBlock stats) => await Task.Run(() => { dbClient.AddToQueue(new ReplaceOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, stats.Id), stats)); }).ConfigureAwait(false);


        public async static Task InsertXp(XpObject xp) => await Task.Run(() => { dbClient.AddToQueue(new InsertOneModel<XpObject>(xp)); }).ConfigureAwait(false);
        public async static Task DeleteOneXp(XpObject xp) => await Task.Run(() => { dbClient.AddToQueue(new DeleteOneModel<XpObject>(Builders<XpObject>.Filter.Eq(x => x.Name, xp.Name))); }).ConfigureAwait(false);
        public async static Task UpdateXp(XpObject xp) => await Task.Run(() => { dbClient.AddToQueue(new ReplaceOneModel<XpObject>(Builders<XpObject>.Filter.Eq(x => x.Name, xp.Name), xp)); }).ConfigureAwait(false);
        //public async static Task UpdateSingleXp(UpdateDefinition<XpObject> update, ulong user) => await Task.Run(() => { dbClient.AddToQueue(new UpdateOneModel<XpObject>(Builders<XpObject>.Filter.Eq(x => x.Name), update)); }).ConfigureAwait(false);


        //at the time of writing this code, you cannot add dynamic values to an IModal, so I have to create ModalBuilder and respond to them here.
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
                case string newItem when newItem.Contains("new_xp:"):
                    var newXp = new XpObject()
                    {
                        Name = modal.Data.CustomId.Split(":")[1],
                        Track = components[1].Value == "S" ? Xp.XpTrack.Slow :
                                components[1].Value == "F" ? Xp.XpTrack.Fast :
                                Xp.XpTrack.Medium,
                        MaxLevel = int.TryParse(components[2].Value, out var maxResult) ? maxResult : 999,
                        Details = components[3].Value,
                        LevelInfo = components[4].Value
                    };                   
                    await GetXp().InsertOneAsync(newXp);
                    var ebNew = new EmbedBuilder().WithDescription(await Xp.GetLevelInfo(newXp, 20));
                    await modal.RespondAsync("Added New XP Entry.", embed: ebNew.Build());
                    await DataMap.GetXps();
                    return;
                case string newItem when newItem.Contains("set_xp:"):
                    var xpName = modal.Data.CustomId.Split(":")[1];
                    var results = await GetXp().FindAsync(x => x.Name == xpName);
                    var xp = results.ToList();
                    
                    if(int.TryParse(components[0].Value, out var xpToAdd))
                        xp[0].Experience += xpToAdd;                   
                    xp[0].Track = components[1].Value == "S" ? Xp.XpTrack.Slow : 
                        components[1].Value == "M" ? Xp.XpTrack.Medium : 
                        components[1].Value == "F" ? Xp.XpTrack.Fast : 
                        xp[0].Track;
                    xp[0].MaxLevel = int.TryParse(components[2].Value, out maxResult) ? maxResult : 999;
                    xp[0].Details = components[3].Value;
                    xp[0].LevelInfo = components[4].Value;               
                    GetXp().FindOneAndReplace(x => x.Name == xp[0].Name, xp[0]);
                    var ebSet = new EmbedBuilder().WithDescription($"{(xp[0].Details != string.Empty ? $"*{xp[0].Details}*" : "")}\r\n\r\n{await Xp.GetLevelInfo(xp[0], 2)}");
                    await modal.RespondAsync("Updated.", embed: ebSet.Build());
                    return;
            }
        }

        public static InvItem ParseInvItem(string item, string baseItem = "")
        {
            var split = item.Split(':');
            var invItem = new InvItem()
            {
                Base = baseItem,
                Name = split[0],
                Quantity = split.Length > 0 ? (int.TryParse(split[1], out int outInt) ? outInt : 0) : 0,
                Value = split.Length > 1 ? (decimal.TryParse(split[2], out decimal outDec) ? outDec : 0m) : 0m,
                Weight = split.Length > 2 ? (decimal.TryParse(split[3], out outDec) ? outDec : 0m) : 0m,
                Note = split.Length > 3 ? split[4] : ""
            };
            return invItem;
        }
    }
}




