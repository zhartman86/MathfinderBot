using Gellybeans.Pathfinder;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MathfinderBot
{
    internal class MongoHandler
    {
        readonly MongoClient                    client;        
        readonly IMongoDatabase                 database;
        readonly IMongoCollection<StatBlock>    statBlocks;
        
        static readonly List<WriteModel<StatBlock>> writeQueue = new List<WriteModel<StatBlock>>();

        public MongoHandler(MongoClient client, string dbName)
        {
            //set the `Id` value as index, automatically generate ids when an entry is created
            BsonClassMap.RegisterClassMap<StatBlock>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });

            this.client = client;
            database    = client.GetDatabase(dbName);
            statBlocks =  database.GetCollection<StatBlock>("statblocks");
        }

        public List<BsonDocument> ListDatabases() { return client.ListDatabases().ToList(); }

        public IMongoCollection<StatBlock> GetStatBlocks() { return statBlocks; }

        public void AddToQueue(InsertOneModel<StatBlock> insertOne) =>
            writeQueue.Add(insertOne);

        public void AddToQueue(ReplaceOneModel<StatBlock> replaceOne) =>
             writeQueue.Add(replaceOne);

        public void AddToQueue(UpdateOneModel<StatBlock> updateOne) =>
            writeQueue.Add(updateOne);

        public async void ProcessQueue()
        {
            if(writeQueue.Count > 0)
            {
                var count = writeQueue.Count;                
                await statBlocks.BulkWriteAsync(writeQueue).ConfigureAwait(false);
                writeQueue.Clear();
                Console.WriteLine($"Updates written this cycle: {count}");
            }            
        }
    }
}
