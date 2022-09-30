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

namespace MathfinderBot
{
    public class Program
    {       
        public static MongoClient           dbClient;
        public static IMongoDatabase        database;
        public static DiscordSocketClient   client;
        public static InteractionService    interactionService;
        public static LoggingService        logger;

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
    }
}

