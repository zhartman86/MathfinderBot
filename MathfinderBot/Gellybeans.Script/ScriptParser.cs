using System.Text;

namespace MathfinderBot
{
    public class ScriptParser
    {
        readonly ScriptTokenizer tokenizer;

        public ScriptParser(ScriptTokenizer tokenizer) =>
            this.tokenizer = tokenizer;

        
       

        public EventGraph ParseScript()
        {

            var name = "";
            var dict = new Dictionary<string, EventNode>();
            while(tokenizer.CurrentToken != ScriptToken.CloseSquiggle && tokenizer.CurrentToken != ScriptToken.EOF)
            {
                var str = ParseEventName();
                if(string.IsNullOrEmpty(str))
                    break;
                   
                    
                tokenizer.NextToken();
                var node = ParseInner(str);
                if(str == null || node == null)
                {
                    Console.WriteLine($"Error on line {tokenizer.CurrentLine}");
                    return null!;
                }
                dict.Add(str, node);                   
                
                if(dict.Count == 1) 
                    name = node.Name;  
                
                tokenizer.NextToken();              
            }

            var graph = new EventGraph { Name = name, Nodes = dict };        
            if(graph.Nodes.Count > 0)
                return graph;

            return null!;
        }


        string? ParseEventName()
        {
            while(tokenizer.CurrentToken != ScriptToken.Keyword)
            {
                if(tokenizer.CurrentToken == ScriptToken.EOF)
                    return null!;
                tokenizer.NextToken();
            }
                
            if(tokenizer.Word == "EVENT")
            {
                tokenizer.NextToken();
                if(tokenizer.CurrentToken == ScriptToken.DoubleColon)
                {
                    tokenizer.NextToken();
                    if(tokenizer.CurrentToken == ScriptToken.Word)
                    {
                        var word = tokenizer.Word;
                        tokenizer.NextToken();
                        return word;
                    }
                }
            }           
            return null!;            
        }
        
        List<EventChoice> ParseChoices()
        {
            var choices = new List<EventChoice>();
            while(tokenizer.CurrentToken != ScriptToken.CloseSquiggle && tokenizer.CurrentToken != ScriptToken.Keyword)
            {
                var choiceText = tokenizer.ReadLines();
                if(tokenizer.CurrentToken == ScriptToken.Arrow)
                {
                    tokenizer.NextToken();
                    if(tokenizer.CurrentToken == ScriptToken.Word)
                    {
                        var choiceLabel = tokenizer.Word;
                        var req = "";
                        
                        tokenizer.NextToken();
                        if(tokenizer.CurrentToken == ScriptToken.OpenBracket)
                            req = tokenizer.ReadBrackets();
                        choices.Add(new EventChoice { Text = choiceText, Next = choiceLabel, Requirements = req });
                    }
                }              
            }
            return choices;         
        }
       

        EventNode ParseInner(string name)
        {

            Prompt prompt = null!;
            List<EventChoice> choices = null!;
            string expr = "";

            if(tokenizer.CurrentToken == ScriptToken.OpenSquiggle)
            {
                tokenizer.NextToken();
                tokenizer.NextToken();
                while(tokenizer.CurrentToken != ScriptToken.CloseSquiggle)
                {

                    if(tokenizer.CurrentToken == ScriptToken.Keyword)
                    {
                        if(tokenizer.Word == "PROMPT")
                        {
                            tokenizer.NextToken();
                            prompt = tokenizer.ReadPrompt();
                        }

                        if(tokenizer.Word == "CHOICE")
                        {
                            tokenizer.NextToken();
                            choices = ParseChoices();
                        }
                            
                        if(tokenizer.Word == "EFFECT")
                        {
                            tokenizer.NextToken();
                            tokenizer.NextToken();
                            var lines = tokenizer.ReadBrackets();
                            expr = lines;
                        }
                    }
                    if(tokenizer.CurrentToken == ScriptToken.CloseSquiggle)
                        continue;
                    tokenizer.NextToken();                   
                }
                
                var node = new EventNode
                {
                    Name = name,
                    Prompt = prompt,
                    Choices = choices != null ? choices.ToArray() : null!,
                    Effect = expr
                };
                Console.WriteLine(node.ToString());
                return node;
            }
            return null!;
                     
        }

        public static EventGraph Parse(string script)
        {
            var result = Parse(new ScriptTokenizer(new StringReader(script)));
            return result;
        }
 
        public static EventGraph Parse(ScriptTokenizer token)
        {
            var result = new ScriptParser(token).ParseScript();
            return result;
        }
            
    }
}
