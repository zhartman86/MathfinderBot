using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Gellybeans.Pathfinder;


using Microsoft.Extensions.DependencyInjection;

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

            var settings = MongoClientSettings.FromConnectionString("mongodb+srv://Gelton:C6V9Mi259uV9QyT@gelly-1.j7wxlpm.mongodb.net/?retryWrites=true&w=majority");
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

                var token = "MTAwMzg0NDYyODg0MTIzODU4OA.G7kN_9.Q5WgQp222LF52A5_uge958ElOzePLOtNq6TOzo";

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
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }
        
    }
}

