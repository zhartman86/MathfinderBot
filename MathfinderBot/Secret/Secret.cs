using MongoDB.Bson.Serialization.Attributes;

namespace MathfinderBot
{
    public class Secret
    {
        [BsonIgnore]
        public string EventString { get; init; }

        [BsonIgnore]
        public (string, string) Choices { get; init; }

        [BsonIgnore]
        public string Take { get; init; }

        public int Index { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }

        [BsonIgnore]
        public Func<DuelEvent, int, bool> Apply { get; set; }
    }
}
