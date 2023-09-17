using Discord;
using Gellybeans.Expressions;
using Gellybeans.Pathfinder;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using static MathfinderBot.EventGraph;

namespace MathfinderBot
{
    public struct EventPayload
    {
        public readonly string Color;
        public readonly string Prompt;
        public readonly Dictionary<string, string> Choices;
        public readonly Dictionary<string, string> Fields;

        public EventPayload(string color, string prompt, Dictionary<string, string> choices, Dictionary<string, string> fields)
        {
            Color = color;
            Prompt = prompt;
            Choices = choices;
            Fields = fields;
        }

        public override string ToString()
        {
            return $@"COLOR {Color}
PROMPT
{Prompt}

CHOICES: {(Choices != null ? Choices.Count : 0)}

FIELDS: {(Fields != null ? Fields.Count : 0)}";
        }
    }

    public class Prompt
    {
        public string[] Prompts  { get; init; }
        public string   Switch   { get; init; }
    }
    
    public class EventChoice
    {
        public string Text { get; init; }        
        public string Next { get; init; }
        public string Requirements { get; init; }
        public string Effect { get; init; }
    }

    public class EventNode
    {
        static readonly Regex brackets = new Regex(@"<.*?>");
        static readonly Regex varReplace = new Regex(@"`.*?`");


        public string        Name    { get; init; }
        public Prompt        Prompt  { get; init; }              
        public EventChoice[] Choices { get; init; }
        public string        Effect  { get; init; }
        public string[]      Fields  { get; init; }


        public async Task<EventPayload> GetPayload(ulong id)
        {
            var sec = new SecretCharacter();
            if(!string.IsNullOrEmpty(Effect))
                Evaluate(Effect.Replace(" ", ""), sec);

            var prompt = Prompt.Prompts[0];
            var promptResult = 1;
            if(!string.IsNullOrEmpty(Prompt.Switch))
            {
                promptResult = Parser.Parse(Prompt.Switch.Replace(" ", "")).Eval(sec);
                prompt = Prompt.Prompts[promptResult - 1];
            }
            else if(Prompt.Prompts.Length > 1) //if more than one prompt, but no switch, randomize.
                promptResult = Random.Shared.Next(1, Prompt.Prompts.Length);
            
            //replace all variables in a prompt with their respective values.
            //wrapping variables in [] will be evaluated as is. if the value found in Properties contains commas, it will be split and randomized.
            prompt = varReplace.Replace(prompt, m =>
            {
                var replacement = "";
                var var = m.Value.Trim('`');

                if(m.Value[0] == '[')
                {
                    replacement = Parser.Parse(m.Value.Trim(new char[] { '[', ']' })).Eval(sec).ToString();
                }
                else
                {
                    replacement = sec.Properties.TryGetValue(var.ToUpper(), out string? outVal) ? outVal : "!@#$*&¡";
                }

                if(replacement.Contains(','))
                {
                    var options = replacement.Split(',');
                    replacement = options[Random.Shared.Next(0, options.Length)];
                }
                
                if(replacement == "$")
                {
                    var bytes = Encoding.Default.GetBytes(id.ToString());
                    replacement = Encoding.UTF8.GetString(bytes);
                }

                
                return replacement;
            });
            var color = "";
            var colorRegex = brackets.Match(prompt);
            if(colorRegex.Success)
            {
                color = colorRegex.Value.Trim(new char[] { '<', '>' });
                prompt = brackets.Replace(prompt, "");
            }
            var actualChoices = new List<EventChoice>();
            if(Choices != null)
            {
                for(int i = 0; i < Choices.Length; i++)
                {
                    if(!string.IsNullOrEmpty(Choices[i].Requirements))
                    {
                        if(Choices[i].Requirements[0] == '^')
                        {
                            var n = int.Parse(Choices[i].Requirements.Trim('^'));
                            if(n == promptResult)
                                actualChoices.Add(Choices[i]);
                            continue;
                        }
                        var result = int.TryParse(Evaluate(Choices[i].Requirements, sec), out int outVal) ? outVal : 0;
                        actualChoices.Add(Choices[i]);
                    }
                    else actualChoices.Add(Choices[i]);
                }
            }
            Dictionary<string, string> fields = null!;
            if(Fields != null)
            {
                fields = new Dictionary<string, string>();
                for(int i = 0; i < Fields.Length; i++)
                {
                    if(Fields[i] != string.Empty)
                    {
                        var split = Fields[i].Split("##", options: StringSplitOptions.RemoveEmptyEntries);
                        if(split.Length == 2)
                            fields.Add(split[0].Trim(), split[1].Trim());
                    }
                }
            }

            return new EventPayload(color, prompt, actualChoices.ToDictionary(x => x.Text, x => x.Next), fields);
        }

        public static string Evaluate(string expr, SecretCharacter sec)
        {
            var sb = new StringBuilder();
            var exprs = expr.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var result = "";
            for(int i = 0; i < exprs.Length; i++)
            {
                var node = Parser.Parse(exprs[i]);
                result += $"{node.Eval(sec, sb)};";
            }
            Console.WriteLine(sb.ToString());
            return result;
        }

        public override string ToString()
        {
            return $@"EVENT NAME: {Name}
PROMPTS: {Prompt.Prompts.Length} SW: {Prompt.Switch}
CHOICES: {(Choices != null ? Choices.Length : 0)}
EFFECT: {Effect}
FIELDS: {(Fields != null ? Fields.Length : 0)}";
        }
    }

    public class EventGraph
    {  
        public static readonly Dictionary<string, EventGraph> Events = new Dictionary<string, EventGraph>();      

        public string Name { get; init; }
        public Dictionary<string, EventNode> Nodes { get; init; }
        
        public EventNode this[string name]
        {
            get { return Nodes.TryGetValue(name, out EventNode? outVal) ? outVal : null!; }
        }

        public event EventHandler<string>? ValueChanged;
        void OnValueChanged(string propertyChanged) { ValueChanged?.Invoke(this, propertyChanged); }

       
        
        //evaluate this event
        

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
