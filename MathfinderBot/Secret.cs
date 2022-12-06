using MongoDB.Bson.Serialization.Attributes;

namespace MathfinderBot
{     
    public class Secret
    {
        [BsonIgnore]
        public string EventString { get; init; }

        [BsonIgnore]
        public (string,string) Choices { get; init; }

        [BsonIgnore]
        public string Take { get; init; }

        public string Name { get; init; }
        public string Description { get; init; }
        public string ExprModifier { get; init; }

        public static Dictionary<int, Secret> Secrets = new Dictionary<int, Secret>()
        {
            { 0, new Secret()
                    {
                        Name = "Seeing Stone",
                        EventString = "You see a perfectly smooth sphere, unmarked and unmarred. When you place your hand upon its surface, an orange fire erupts from within.",
                        Choices = ("Take the stone.", "Leave it alone."),
                        Take = "...",
                        Description = "A mysterious orb that glows when touched.",
                        ExprModifier = ""          
                    }},
            
            { 1, new Secret()
                    {
                        Name         = "Old Wooden Sword",
                        EventString  = "You find yourself in a dark cave, a faint noise echoes from within. Suddenly, two flames erupt near the center of the room, a cloaked figure standing between them—his arms reaching out. You see something in the dirt before his outstretched arms.\r\nThe man looks at you and says, \"It's dangerous to go alone! Take this.\"",
                        Choices      = ("Take it", "\"Nah.\""),
                        Take         = "The haft rests uneasy in your grip. If the stars were to align, mayhaps you could deal a lethal strike.",
                        Description  = "A seemingly ancient wooden sword",
                        ExprModifier = "+rand(-1,2)"
                    }},
        };
    }
}
