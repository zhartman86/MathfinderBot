using System.Text;

namespace MathfinderBot
{
    public class ScriptParser
    {
        readonly ScriptTokenizer tokenizer;

        public ScriptParser(ScriptTokenizer tokenizer) =>
            this.tokenizer = tokenizer;

        
       

        public Dictionary<string, EventNode> ParseScript()
        {
           
            var dict = new Dictionary<string, EventNode>();
                while(tokenizer.CurrentToken != ScriptToken.CloseSquiggle && tokenizer.CurrentToken != ScriptToken.EOF)
                {
                    var str = ParseEventName();
                    tokenizer.NextToken();
                    var node = ParseInner();
                    if(str == null || node == null)
                    {
                        Console.WriteLine($"Error on line {tokenizer.CurrentLine}");
                        return null!;
                    }
                    dict.Add(str, node);
                    tokenizer.NextToken();              
                }
            
            Console.WriteLine("Done!");
            return dict;
          
            
        }


        string? ParseEventName()
        {
            while(tokenizer.CurrentToken != ScriptToken.Keyword) tokenizer.NextToken();
            
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
       

        EventNode ParseInner()
        {
           
            string prompt = "";
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
                            var lines = tokenizer.ReadLines();
                            prompt = lines;
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
                    Prompt = prompt,
                    Choices = choices != null ? choices : null!,
                    Effect = expr
                };
                //Console.WriteLine(node.ToString());
                return node;
            }
            Console.WriteLine("BNALDA");
            return null!;
                     
        }

        public static Dictionary<string, EventNode> Parse(string script)
        {
            var result = Parse(new ScriptTokenizer(new StringReader(script)));
            return result;
        }
 
        public static Dictionary<string, EventNode> Parse(ScriptTokenizer token)
        {
            var result = new ScriptParser(token).ParseScript();
            return result;
        }
            
    }
}
