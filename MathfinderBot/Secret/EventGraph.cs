using System.Text;

namespace MathfinderBot
{

    
    public class EventChoice
    {
        public string Text { get; init; }        
        public string Next { get; init; }
        public string Requirements { get; init; }

    }

    public class EventNode
    {
        public string Prompt                { get; init; }       
        public string Effect                { get; init; } 
        public List<EventChoice> Choices    { get; init; }


        public override string ToString()
        {
            return $@"PROMPT: {Prompt}
CHOICES: {Choices.Count}
EFFECT: {Effect}";
        }
    }

    public class EventGraph
    {
        public List<EventNode> Nodes { get; set; }

        public EventNode Current { get; set; }

        //public static readonly Dictionary<string, EventNode> Empty = new Dictionary<string, EventNode>()
        //{
        //    { "FP_Arduigh", new EventNode {
        //        Effect = (character, sb) =>
        //        {
                    
        //            character.AddInt("Arduigh_Heart", 0);
        //        },
        //        Text = "The fighting pits were uproarious. A steadily quick drumbeat honed an already sharpened crowd—their eyes pointed downward toward the blood-crusted sandy floor of the arena. Two combatants warily strafe one another, waiting for that one relevant moment, trying as they might to forget everything else.\r\n\r\n\"I've seen you around,\" you hear, shortly before a man drags a chair from a nearby table to share yours. \"You've done quite well for yourself, I see.\"",
        //        Choices = new List<EventChoice>
        //        {
        //            new EventChoice { Text = @"""That's right.""", Next = "FP_Arduigh_0" },
        //            new EventChoice { Text = @"""Piss off or you'll see me do even better.""", Next = "FP_Arduigh_1" }
        //        }
        //    }},
        //            { "FP_Arduigh_0", new EventNode {
        //                Text = "The man makes himself cozy, taking a drink as some spills down his fiery beard. \"The name's Arduingh.\"\r\n\r\nThe man appears aged, worn, scarred—perhaps a fighter at some point. His portly figure suggests he lives comfortably in a den where many fight—and possibly kill—for rations. He wipes his beard of ale and lets out a belch.",
        //                Choices = new List<EventChoice>
        //                {
        //                    new EventChoice { Text = @"""Greetings, Arduingh. What can I do?""", Next = "FP_Arduingh_0_0" },
        //                    new EventChoice { Text = @"""Give him your name"""},
        //                    new EventChoice { Text = @"""Great. Now, why don't you piss off?""", Next = "FP_Arduigh_1"}
        //                }
        //            }},
        //                { "FP_Arduingh_0_0", new EventNode {
        //                    Text = "\"It's not about what you can do for me,\" he says, placing a second hand on his mug, giving uncomfortable pause.\r\n\r\n\"It's clear you aren't from the city. Why you came here isn't important—and where you're from, it's all the same to me—but let me tell you...\" Arduigh takes a large swig before looking you with an unfamiliar sincerity.\r\n\r\n\"As I've said, you've done well for yourself—but it won't last. No one here stays lucky for long.\"",
        //                    Choices = new List<EventChoice>
        //                    {
        //                        new EventChoice { Text = "You call it luck.", Next = "FP_Arduigh_0_0_0" },
        //                        new EventChoice { Text = "I'll humor you. So, what do you propose?" },
        //                        new EventChoice { Text = "I'll take my chances. Now—go away."}
        //                    }
        //                }},
        //                    { "FP_Arduigh_0_0_0", new EventNode { 
        //                        Text = "\"You may know something your combatants do not. You may have a trick or two up your sleeve, but that won't be enough. It never is. Time will make a fool of you. Unless...\"",
        //                    }},


        //            { "FP_Arduigh_1", new EventNode {
        //                Effect = (character, sb) => { character.AddInt("Arduigh_Heart", 1); }, //this only validates his opinion of you, in a good way
        //                Text = "The man's expression flushes. He replies, \"Words or weight, you're a tough bastard, aren't ya?\" He rubs his chin through a greying beard. \"Figured I'd give you some advice—as you seem to be new to these parts—but I can see you aren't interested.\r\n\r\nThe man lifts his mug of ale off the table, standing to his feet.",
        //                Choices = new List<EventChoice>
        //                {
        //                    new EventChoice { Text = "Apologize", Next = "FP_Arduigh_1_0" },
        //                    new EventChoice { Text = @"""So, say it.""", Next = "FP_Arduigh_1_1" },
        //                    new EventChoice { Text = "...", Next = "FP_Arduigh_1_2", },
        //                }
        //            }},
        //                { "FP_Arduigh_1_0", new EventNode {
        //                    Effect = (character, sb) => //attempting to apologize at this point will not gain you any heart.
        //                    {
        //                        var r = Random.Shared.Next(1,21);
        //                        var text = $"d20: **[{r}]**\r\n\r\n";
        //                        if( r >= 7) //you're harsh, but perhaps interested enough in what he has to say
        //                            sb.AppendLine("You do your best to smooth over your previous dismisal. The man seems to take you as sincere, placing his mug back down on the harshwood table.\r\n\r\n\"Arduigh's my name.\"");
        //                        else //you're awkward, unsure of your own actions—maybe just lucky to have done this well. he fully expects you to fail sooner than later
        //                        {
                                    
        //                            character.AddInt("Arduigh_Heart", -2);
        //                            sb.AppendLine("You trip on an easy walk, muttering too long on excuses about why you are, in fact, the way you are. The bearded man smiles cold and dutifully in your general direction.\r\n\r\n\"Worry not, friend. I can see you don't wish to be bothered.\" He turns to escape your prescence.");
        //                        }
        //                    },
        //                    Choices = new List<EventChoice>
        //                    {
        //                        new EventChoice { Text = "Give Your Name", Next = "FP_Arduigh_",
        //                            Requirements = new CharacterRequirement[] { new CharacterRequirement { EvalType = EvalType.Flag, Property = "Arduigh_Flags", Value = ((long)EventFlag.Fifth).ToString() } } },
        //                        new EventChoice { Text = "\"Greetings, Arduigh.\"", Next = "FP_Arduigh_",
        //                            Requirements = new CharacterRequirement[] { new CharacterRequirement { EvalType = EvalType.Flag, Property = "Arduigh_Flags", Value = ((long)EventFlag.Fifth).ToString() } } }
        //                    }
        //                }},
        //                { "FP_Arduigh_1_2", new EventNode {
                            
        //                    Text = "The man walks back into a busy crowd, disappearing from sight.\r\n\r\nTurning your attention back toward the fight, you see one combatant on the backfoot, bleeding from a gash across their bare chest. Their opponent presses the advantage with the taunting rattle of a serrated-chain nine-tails. Swinging forward with unthinking ferocity, the chained tendrils rip into a wooden shield quickly salvaged from a fresh corpse. They try to pull back, but the sharpened chains get stuck. The injured combatant finds an opening, planting their shortblade into their opponent's open left flank, digging deep into their crucial underbelly.\r\n\r\nThe fight is over, but yours has yet to begin."
        //                }},

        //    { "SeeingStone_", new EventNode {
        //        Text = "Upon a stone dias, you happen upon a perfectly smooth sphere, unmarked and unmarred. When you place your hand upon it, an orange fire erupts from within.",
        //        Choices = new List<EventChoice>
        //        {
        //            new EventChoice { Text = "Take the stone.", Next = "SeeingStone_Take" },
        //            new EventChoice { Text = "Leave it.", Next = "SeeingStone_Leave" }
        //        }
        //    }},
        //        { "SeeingStone_Take", new EventNode {
        //            Text = "...",
        //        }},
        //        { "SeeingStone_Leave", new EventNode {
        //            Text = "You decide there is nothing you want from such a thing. No relic worth having would play such a wicked trick.",
        //        }},
        //};
    }

   
}
