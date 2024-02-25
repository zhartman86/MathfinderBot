using MongoDB.Driver;

namespace MathfinderBot
{
    public interface IDatabase
    {
        //void Delete(T item );
        //void Update(T item );
        void InsertOne<T>(T item);
        void ReplaceOne<T>(T item);
        void UpdateOne<T>(T item, UpdateDefinition<T> update);
        void DeleteOne<T>(T item);
    }
}
