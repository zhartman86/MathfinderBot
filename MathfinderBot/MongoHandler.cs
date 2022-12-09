using Gellybeans.Pathfinder;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MathfinderBot.Secret;

namespace MathfinderBot
{
    public class MongoHandler
    {
        readonly MongoClient client;
        readonly IMongoDatabase database;
        
        readonly IMongoCollection<StatBlock> statBlocks;
        readonly IMongoCollection<DuelEvent> duelEvents;
        readonly IMongoCollection<SecretCharacter> secrets;

        public IMongoCollection<StatBlock> StatBlocks           { get { return statBlocks; } }
        public IMongoCollection<DuelEvent> Duels                { get { return duelEvents; } }
        public IMongoCollection<SecretCharacter>    Secrets     { get { return secrets; } }

        static readonly List<WriteModel<StatBlock>> writeQueue = new List<WriteModel<StatBlock>>();
        static readonly List<WriteModel<DuelEvent>> duelQueue = new List<WriteModel<DuelEvent>>();
        static readonly List<WriteModel<SecretCharacter>> secretQueue = new List<WriteModel<SecretCharacter>>();

        public MongoHandler(MongoClient client, string dbName)
        {
            //set the `Id` value as index, automatically generate ids when an entry is created
            BsonClassMap.RegisterClassMap<StatBlock>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });

            BsonClassMap.RegisterClassMap<DuelEvent>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });

            BsonClassMap.RegisterClassMap<SecretCharacter>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });

            this.client = client;
            database = client.GetDatabase(dbName);
            statBlocks = database.GetCollection<StatBlock>("statblocks");
            duelEvents = database.GetCollection<DuelEvent>("duels");
            secrets = database.GetCollection<SecretCharacter>("secrets");
        }

        public List<BsonDocument> ListDatabases() { return client.ListDatabases().ToList(); }

        public void AddToQueue(InsertOneModel<SecretCharacter> insertOne) =>
            secretQueue.Add(insertOne);

        public void AddToQueue(ReplaceOneModel<SecretCharacter> replaceOne) =>
            secretQueue.Add(replaceOne);
        
        public void AddToQueue(InsertOneModel<DuelEvent> insertOne) =>
            duelQueue.Add(insertOne);

        public void AddToQueue(InsertOneModel<StatBlock> insertOne) =>
            writeQueue.Add(insertOne);

        public void AddToQueue(ReplaceOneModel<StatBlock> replaceOne) =>
             writeQueue.Add(replaceOne);

        public void AddToQueue(DeleteOneModel<StatBlock> deleteOne) =>
            writeQueue.Add(deleteOne);

        public void AddToQueue(UpdateOneModel<StatBlock> updateOne) =>
            writeQueue.Add(updateOne);

        public async void ProcessQueue()
        {
            if(writeQueue.Count > 0)
            {
                var count = writeQueue.Count;
                await statBlocks.BulkWriteAsync(writeQueue).ConfigureAwait(false);
                writeQueue.Clear();
                Console.WriteLine($"Statblock updates written this cycle: {count}");
            }
            if(duelQueue.Count > 0)
            {
                var count = duelQueue.Count;
                await duelEvents.BulkWriteAsync(duelQueue).ConfigureAwait(false);
                duelQueue.Clear();
                Console.WriteLine($"Duel updates written this cycle: {count}");
            }
            if(secretQueue.Count > 0)
            {
                var count = secretQueue.Count;
                await secrets.BulkWriteAsync(secretQueue).ConfigureAwait(false);
                secretQueue.Clear();
                Console.WriteLine($"Secrets written this cycle: {count}");
            }
        }
    }
}
