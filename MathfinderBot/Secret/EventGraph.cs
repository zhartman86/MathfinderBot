namespace MathfinderBot
{
    public enum EvalType
    {
        GreaterThanOrEquals,
        LessThanOrEquals,
        Flag,
        Exact
    }


    public struct CharacterRequirement
    {
        public string Property;
        public string Value;
        public EvalType EvalType;

        public CharacterRequirement(string property, string value, EvalType evalType)
        {
            Property = property;
            Value = value;
            EvalType = evalType;
        }
    }
    
    public class EventChoice
    {
        public string Text { get; init; }        
        public string Next { get; init; }
        public CharacterRequirement? Requirement { get; init; } = null!;

    }

    public class EventNode
    {
        public string Id                      { get; init; }
        public string Text                    { get; init; }
        public List<EventChoice> Choices      { get; init; }
        public Func<SecretCharacter, string> Effect { get; init; }   
    }

    public class EventGraph
    {
        public List<EventNode> Nodes { get; set; }

        public EventNode Current { get; set; }

        public static readonly Dictionary<string, EventNode> Empty = new Dictionary<string, EventNode>()
        {
            { "FP_Arduigh", new EventNode
            {
                Id = "FP_Arduigh",
                Effect = (character) => 
                {
                    character.SetFlag("Arduigh_Flags", EventFlag.None);
                    character.AddInt("Arduigh_Heart", 0);
                    return ""; 
                },               
                Text = "The fighting pits were uproarious. A steadily quick drumbeat honed an already sharpened crowd—their eyes pointed downward toward the blood-crusted sandy floor of the arena. Two combatants warily strafe one another, waiting for that one relevant moment, trying as they might to forget everything else.\r\n\r\n\"I've seen you around,\" you hear—shortly before a man drags a chair from a nearby table to share yours. \"You've done quite well for yourself, I see.\"",
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        Text = "\"That's right.\"",
                        Next = "FP_Arduigh_0"
                    },
                    new EventChoice
                    {
                        Text = "\"...\"",
                        Next = "FP_Arduigh_1"
                    },
                    new EventChoice
                    {
                        Text = "\"Piss off before I do even better.\"",
                        Next = "FP_Arduigh_3"
                    }
                }
            }},
                    { "FP_Arduigh_3", new EventNode
                    {
                        Id = "FP_Arduigh_3",
                        Effect = (character) =>
                        {
                            character.AddInt("Arduigh_Heart", 1);
                            return "";
                        },                       
                        Text = "The man's expression flushes. He replies, \"Words or weight, you're a tough bastard, aren't ya?\" He rubs his chin through a greying beard. \"Figured I'd give you some advice—as you seem to be new to these parts—but I can see you aren't interested.\r\n\r\nThe man lifts his mug of ale off the table, standing to his feet.",
                        Choices = new List<EventChoice>
                        {
                            new EventChoice
                            {
                                Text = "Apologize",
                                Next = "FP_Arduigh_3_0"
                            },
                            new EventChoice
                            {
                                Text = "\"So, say it.\"",
                                Next = "FP_Arduigh_3_1"
                            },
                            new EventChoice
                            {
                                Text = "...",
                                Next = "FP_Arduigh_3_2",
                            },
                        }
                    }},
                        { "FP_Arduigh_3_0", new EventNode
                        {
                            Id = "FP_Arduigh_3_0",                           
                            Effect = (character) => //attempting to apologize at this point will not gain you any heart.
                            {                              
                                var r = Random.Shared.Next(1,21);
                                var str = $"d20: **[{r}]**\r\n\r\n";
                                if( r >= 7) //you're harsh, but perhaps interested enough in what he has to say
                                    str += $"You do your best to smooth over your previous dismisal. The man seems to take you as sincere, placing his mug back down on the harshwood.\r\n\r\n\"Arduigh's my name.\"";                       
                                else //you're awkward, unsure of your own actions—maybe just lucky to have done this well. he fully expects you to fail sooner than later
                                {
                                    character.SetFlag("Arduigh_Flags", EventFlag.First);
                                    character.AddInt("Arduigh_Heart", -2);
                                    str += "You trip over your words with every step. The man recoils over a half-smile. \"Worry not, friend. I can see you don't wish to be bothered.\"\r\n\r\nHe turns to escape your prescence.";
                                }                                                            
                                return str;
                            },
                            
                            Choices = new List<EventChoice>
                            {
                                new EventChoice
                                {
                                    Text = "Give your name",
                                    Requirement = new EventRequirement {  }
                                }
                            }
                        }},

            { "SeeingStone_", new EventNode {
                Id = "SeeingStone_",
                Text = "Upon a stone dias, you happen upon a perfectly smooth sphere, unmarked and unmarred. When you place your hand upon it, an orange fire erupts from within.",
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        Text = "Take the stone.",
                        Next = "SeeingStone_Take"
                    },
                    new EventChoice
                    {
                        Text = "Leave it.",
                        Next = "SeeingStone_Leave"
                    }
                }
            }},
                { "SeeingStone_Take", new EventNode {
                    Id = "SeeingStone_Take",
                    Text = "...",

                }},
                { "SeeingStone_Leave", new EventNode {
                    Id = "SeeingStone_Leave",
                    Text = "You decide there is nothing you want from such a thing. No relic worth having would play such a wicked trick.",
                }},
        };
    }

   
}
