using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Gellybeans.Pathfinder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace MathfinderBot
{
    public class Program
    {       
        public static MongoClient           dbClient;
        public static IMongoDatabase        database;
        public static DiscordSocketClient   client;
        public static InteractionService    interactionService;
        public static LoggingService        logger;

        static Regex validExpr = new Regex(@"^[-0-9a-zA-Z_:+*/%=!<>()&|$ ]{1,400}$");

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
            client = services.GetRequiredService<DiscordSocketClient>();
            interactionService = services.GetRequiredService<InteractionService>();

            logger = new LoggingService(client);

            client.Ready += ReadyAsync;

            await client.LoginAsync(TokenType.Bot, fileTwo);
            await client.StartAsync();
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            client.ModalSubmitted += ExprSubmitted;
            

            await Task.Delay(Timeout.Infinite);

            

        }

        private async Task ReadyAsync() => await interactionService.RegisterCommandsGloballyAsync();

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
        
        public async static Task UpdateStatBlock(StatBlock stats)
        {          
            var collection = database.GetCollection<StatBlock>("statblocks");
            await collection.ReplaceOneAsync(x => x.Id == stats.Id, stats);
        }

        public async static Task UpdateSingleAsync(UpdateDefinition<StatBlock> update, ulong user)
        {
            var collection = database.GetCollection<StatBlock>("statblocks");
            await collection.UpdateOneAsync(x => x.Id == Characters.Active[user].Id, update);
        }

        public async Task ExprSubmitted(SocketModal modal)
        {           
            var user = modal.User.Id;            
            var components = modal.Data.Components.ToList();

            switch(modal.Data.CustomId)
            {
                case "set_expr":
                    Characters.Active[user].Expressions[Variable.lastInputs[user]] = components[0].Value;                   
                    await UpdateSingleAsync(Builders<StatBlock>.Update.Set(x => x.Expressions[Variable.lastInputs[user]], Characters.Active[user].Expressions[Variable.lastInputs[user]]), user);
                    await modal.RespondAsync($"{Variable.lastInputs[user]} updated", ephemeral: true);
                    break;
                case "new_row":
                    var row = await ParseExpressions(components[0].Value, await ReadLines(components[1].Value));
                    Characters.Active[user].ExprRows[row.RowName.ToUpper()] = row;
                    await UpdateSingleAsync(Builders<StatBlock>.Update.Set(x => x.ExprRows[row.RowName], Characters.Active[user].ExprRows[row.RowName]), user);
                    await modal.RespondAsync($"{row.RowName} updated", ephemeral: true);
                    break;
                case "edit_item":                   
                    var split = components[2].Value.Split(':');
                    if(split.Length == 3) {
                        var invItem = new InvItem() {
                            Base        = components[0].Value,
                            Name        = components[1].Value != "" ? components[1].Value : components[0].Value,
                            Weight      = decimal.Parse(split[0]),
                            Value       = decimal.Parse(split[1]),
                            Quantity    = int.Parse(split[2]),
                            Note        = components[3].Value };}
                    break;
            }

          
        }
    
        async Task<List<string>> ReadLines(string s)
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
    }
}

