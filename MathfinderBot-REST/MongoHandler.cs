using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace MathfinderBot
{
    public class MongoHandler : IDatabase
    {
       
        
        readonly MongoClient client;
        readonly IMongoDatabase database;
        
        readonly IMongoCollection<StatBlock> statBlocks;
        readonly IMongoCollection<XpObject> xpObjects;

        public IMongoCollection<StatBlock> StatBlocks { get { return statBlocks; } }
        public IMongoCollection<XpObject> XpObjects { get { return xpObjects; } }

        static readonly List<WriteModel<StatBlock>> statQueue = new List<WriteModel<StatBlock>>();
        static readonly List<WriteModel<XpObject>> xpQueue = new List<WriteModel<XpObject>>();

        public MongoHandler(MongoClient client, string dbName)
        {
            BsonSerializer.RegisterSerializer(new ObjectSerializer(ObjectSerializer.AllAllowedTypes));
            
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

            BsonClassMap.RegisterClassMap<Stat>();
            BsonClassMap.RegisterClassMap<Bonus>();
            BsonClassMap.RegisterClassMap<StringValue>();
            BsonClassMap.RegisterClassMap<ExpressionValue>();
            BsonClassMap.RegisterClassMap<ArrayValue>();
            BsonClassMap.RegisterClassMap<FunctionValue>();
            BsonClassMap.RegisterClassMap<RangeValue>();
            BsonClassMap.RegisterClassMap<Token>();
            BsonClassMap.RegisterClassMap<TokenType>();
            BsonClassMap.RegisterClassMap<KeyValuePairValue>();
            BsonClassMap.RegisterClassMap<EventValue>();
            BsonClassMap.RegisterClassMap<VarNode>();


            this.client = client;
            

            database = client.GetDatabase(dbName);
            statBlocks = database.GetCollection<StatBlock>("statblocks");
            xpObjects = database.GetCollection<XpObject>("xp");
        }

        public List<BsonDocument> ListDatabases() { return client.ListDatabases().ToList(); }

        public void InsertOne<T>(T item)
        {
            switch(item) 
            {
                case StatBlock st:
                    statQueue.Add(new InsertOneModel<StatBlock>(st));
                    return;
                case XpObject xp:
                    xpQueue.Add(new InsertOneModel<XpObject>(xp));
                    return;
            }
        }

        public void ReplaceOne<T>(T item)
        {
            switch(item)
            {
                case StatBlock st:
                    statQueue.Add(new ReplaceOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, st.Id), st));
                    return;
                case XpObject xp:
                    xpQueue.Add(new ReplaceOneModel<XpObject>(Builders<XpObject>.Filter.Eq(x => x.Id, xp.Id), xp));
                    return;
            }
        }

        public void UpdateOne<T>(T item, UpdateDefinition<T> update)
        {   
            
            switch(item)
            {
                case StatBlock st:
                    statQueue.Add(new UpdateOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, st.Id), update as UpdateDefinition<StatBlock>));
                    return;
                case XpObject xp:
                    xpQueue.Add(new UpdateOneModel<XpObject>(Builders<XpObject>.Filter.Eq(x => x.Id, xp.Id), update as UpdateDefinition<XpObject>));
                    return;
            }
        }

        public void DeleteOne<T>(T item)
        {
            switch(item)
            {
                case StatBlock st:
                    statQueue.Add(new DeleteOneModel<StatBlock>(Builders<StatBlock>.Filter.Eq(x => x.Id, st.Id)));
                    return;
                case XpObject xp:
                    xpQueue.Add(new DeleteOneModel<XpObject>(Builders<XpObject>.Filter.Eq(x => x.Id, xp.Id)));
                    return;
            }
        }

        public void AddToQueue(InsertOneModel<StatBlock> insertOne) =>
            statQueue.Add(insertOne);

        public void AddToQueue(ReplaceOneModel<StatBlock> replaceOne) =>
             statQueue.Add(replaceOne);

        public void AddToQueue(DeleteOneModel<StatBlock> deleteOne) =>
            statQueue.Add(deleteOne);

        public void AddToQueue(UpdateOneModel<StatBlock> updateOne) =>
            statQueue.Add(updateOne);


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
            if(statQueue.Count > 0)
            {
                var count = statQueue.Count;


                try                              { await statBlocks.BulkWriteAsync(statQueue).ConfigureAwait(false); }
                catch(MongoBulkWriteException e) { Console.WriteLine($"There was an error processing a bulkwrite request: {e.Message}"); }
                
                statQueue.Clear();
                Console.WriteLine($"Statblock updates this cycle: {count}");
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
