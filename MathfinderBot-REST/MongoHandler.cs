using Gellybeans.Pathfinder;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MathfinderBot
{
    public class MongoHandler
    {
        readonly MongoClient client;
        readonly IMongoDatabase database;
        
        readonly IMongoCollection<StatBlock> statBlocks;
        readonly IMongoCollection<XpObject> xpObjects;

        public IMongoCollection<StatBlock> StatBlocks { get { return statBlocks; } }
        public IMongoCollection<XpObject> XpObjects { get { return xpObjects; } }

        static readonly List<WriteModel<StatBlock>> writeQueue = new List<WriteModel<StatBlock>>();
        static readonly List<WriteModel<XpObject>> xpQueue = new List<WriteModel<XpObject>>();

        public MongoHandler(MongoClient client, string dbName)
        {
            //set the `Id` value as index, automatically generate ids when an entry is created
            BsonClassMap.RegisterClassMap<StatBlock>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });

            BsonClassMap.RegisterClassMap<XpObject>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
            });



            this.client = client;
            database = client.GetDatabase(dbName);
            statBlocks = database.GetCollection<StatBlock>("statblocks");
            xpObjects = database.GetCollection<XpObject>("xp");
        }

        public List<BsonDocument> ListDatabases() { return client.ListDatabases().ToList(); }

        public void AddToQueue(InsertOneModel<StatBlock> insertOne) =>
            writeQueue.Add(insertOne);

        public void AddToQueue(ReplaceOneModel<StatBlock> replaceOne) =>
             writeQueue.Add(replaceOne);

        public void AddToQueue(DeleteOneModel<StatBlock> deleteOne) =>
            writeQueue.Add(deleteOne);

        public void AddToQueue(UpdateOneModel<StatBlock> updateOne) =>
            writeQueue.Add(updateOne);


        public void AddToQueue(InsertOneModel<XpObject> insertOne) =>
            xpQueue.Add(insertOne);

        public void AddToQueue(ReplaceOneModel<XpObject> replaceOne) =>
            xpQueue.Add(replaceOne);

        public void AddToQueue(DeleteOneModel<XpObject> deleteOne) =>
            xpQueue.Add(deleteOne);

        public void AddToQueue(UpdateOneModel<XpObject> updateOne) =>
            xpQueue.Add(updateOne);

        public async void ProcessQueue()
        {
            if(writeQueue.Count > 0)
            {
                var count = writeQueue.Count;
                await statBlocks.BulkWriteAsync(writeQueue).ConfigureAwait(false);
                writeQueue.Clear();
                Console.WriteLine($"Statblock updates written this cycle: {count}");
            }

            if(xpQueue.Count > 0)
            {
                var count = xpQueue.Count;
                await xpObjects.BulkWriteAsync(xpQueue).ConfigureAwait(false);
                xpQueue.Clear();
                Console.WriteLine($"Xp updates written this cycle: {count}");
            }
        }
    }
}
