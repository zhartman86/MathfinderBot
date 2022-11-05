using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Pathfinder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace MathfinderBot
{
    public class Program
    {       
        public static MongoClient           dbClient;
        public static IMongoDatabase        database;
        public static DiscordSocketClient   client;
        public static InteractionService    interactionService;
        public static LoggingService        logger;
        
        public static PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        public static List<WriteModel<StatBlock>> writeQueue = new List<WriteModel<StatBlock>>();

        static Regex validExpr = new Regex(@"^[-0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,400}$");
        static Regex validName = new Regex(@"[a-zA-z' ]{1,50}");

        public static Task Main(string[] args) => new Program().MainAsync();
        public async Task MainAsync()
        {
            var file    = File.ReadAllText(@"C:\File.txt");
            var fileTwo = File.ReadAllText(@"C:\FileTwo.txt");

            //db stuff
            var settings = MongoClientSettings.FromConnectionString(file);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            dbClient = new MongoClient(settings);

            var list = dbClient.ListDatabases().ToList();
            foreach(var db in list)
                Console.WriteLine(db);

            database = dbClient.GetDatabase("Mathfinder");

            BsonClassMap.RegisterClassMap<StatBlock>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });

            
            
            //discord server stuff
            using var services = CreateServices();
            interactionService = services.GetRequiredService<InteractionService>();
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            client = services.GetRequiredService<DiscordSocketClient>();
            logger = new LoggingService(client);
            client.Ready += ReadyAsync;
            client.ModalSubmitted += ExprSubmitted;

            await client.LoginAsync(TokenType.Bot, fileTwo);
            await client.StartAsync();           

            await HandleEvents();
            await Task.Delay(Timeout.Infinite);     
        }

        async Task ReadyAsync() => await interactionService.RegisterCommandsGloballyAsync();
        
        async Task<List<string>> ReadExprLines(string s)
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

        async Task HandleEvents()
        {
            while(await timer.WaitForNextTickAsync())
                if(writeQueue.Count > 0)
                {
                    var count = writeQueue.Count;
                    var collection = database.GetCollection<StatBlock>("statblocks");
                    await collection.BulkWriteAsync(writeQueue);
                    Console.WriteLine($"Updates written this cycle: {count}");
                    writeQueue.Clear();
                }
        }

        public static ServiceProvider CreateServices()
        {
            //var isConfig = new InteractionServiceConfig()
            //{
            //    UseCompiledLambda = true,
            //};

            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }
        
        public static void UpdateStatBlock(StatBlock stats) =>       
            writeQueue.Add(new ReplaceOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, stats.Id), stats));

        public static void UpdateSingle(UpdateDefinition<StatBlock> update, ulong user) =>
            writeQueue.Add(new UpdateOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, Characters.Active[user].Id), update));  

        public async Task ExprSubmitted(SocketModal modal)
        {           
            var user = modal.User.Id;            
            var components = modal.Data.Components.ToList();

            switch(modal.Data.CustomId)
            {
                case "new_row":
                    var row = await ParseExpressions(components[0].Value, await ReadExprLines(components[1].Value));
                    Characters.Active[user].AddExprRow(row);
                    await modal.RespondAsync($"{row.RowName} updated", ephemeral: true);
                    return;
                case string newItem when newItem.Contains("set_expr:"):
                    var varName = modal.Data.CustomId.Split(':')[1];
                    Characters.Active[user].AddExpr(varName, components[0].Value);                   
                    await modal.RespondAsync($"{varName} updated", ephemeral: true);
                    return;             
                case string newItem when newItem.Contains("base_item:"):
                    var outDec = 0m;
                    var outInt = 0;
                    var item = modal.Data.CustomId.Split(':')[1];
                    var invItem = new InvItem() {
                        Base        = item,
                        Name        = components[0].Value != "" && validName.IsMatch(components[0].Value) ? components[0].Value : item,
                        Weight      = decimal.TryParse  (components[1].Value, out outDec) ? outDec : 0m,
                        Value       = decimal.TryParse  (components[2].Value, out outDec) ? outDec : 0m,
                        Quantity    = int.TryParse      (components[3].Value, out outInt) ? outInt : 0,
                        Note        = components[4].Value };
                    Characters.Active[user].InventoryAdd(invItem);
                    await modal.RespondAsync($"{invItem.Name} added", ephemeral: true);
                    return;
            }        
        }
    
        
    }
}

