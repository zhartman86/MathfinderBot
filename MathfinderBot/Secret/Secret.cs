using MongoDB.Bson.Serialization.Attributes;
using System.Text;

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

        [BsonIgnore]
        public Func<DuelEvent, StringBuilder, int, Task<bool>> Apply { get; set; }

        public int Index { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }

        //use this to store any custom values
        public Dictionary<string, string> Properties { get; init; }

    
        public Secret Copy()
        {
            var s = new Secret
            {
                Index = Index,
                Name = Name,
                Description = Description,  
                Properties = Properties
            };
            s.Properties.Add("Created", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
            return s;
        }
    }
}
