using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using Gellybeans.Pathfinder;



using Microsoft.Extensions.DependencyInjection;
using GroupDocs.Parser.Data;
using GroupDocs.Parser;

namespace MathfinderBot
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        public static MongoClient dbClient;
        public static IMongoDatabase database;

        public static DiscordSocketClient  client;
        public static InteractionService   interactionService;

        public static LoggingService logger;

        public async Task MainAsync()
        {

            var file = File.ReadAllText(@"C:\Users\zach\Documents\File.txt");
            var fileTwo = File.ReadAllText(@"C:\Users\zach\Documents\FileTwo.txt");
            
            var settings = MongoClientSettings.FromConnectionString(file);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            dbClient = new MongoClient(settings);

            database = dbClient.GetDatabase("test");
            var list = dbClient.ListDatabases().ToList();

            foreach(var db in list)
            {
                Console.WriteLine(db);
            }

            var map = BsonClassMap.RegisterClassMap<StatBlock>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });
         

            using(var services = CreateServices())
            {
                client = services.GetRequiredService<DiscordSocketClient>();
                interactionService = services.GetRequiredService<InteractionService>();


                logger = new LoggingService(client);

                client.Ready += ReadyAsync;

                var token = fileTwo;

                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                await Task.Delay(Timeout.Infinite);
            }
        }

        public async static Task UpdateStatBlock(StatBlock statBlock)
        {          
            var collection = database.GetCollection<StatBlock>("statblocks");
            await collection.ReplaceOneAsync(x => x.Id == statBlock.Id, statBlock);
        }

        public async static Task UpdateOneStat(StatBlock statBlock, string statName)
        {
            var collection = database.GetCollection<StatBlock>("statblocks");
            var update = Builders<StatBlock>.Update.Set(x => x.Stats[statName], statBlock.Stats[statName]);
            await collection.UpdateOneAsync(x => x.Id == statBlock.Id, update);
        }




        private async Task ReadyAsync()
        {
            await interactionService.RegisterCommandsGloballyAsync();
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
        
    }
}

