using Gellybeans.Expressions;
using System.Text;

namespace MathfinderBot
{
    public enum ScriptToken
    {
        EOF,
        Colon,              // :
        DoubleColon,        // ::
        OpenSquiggle,       // {
        CloseSquiggle,      // }
        OpenBracket,        // [
        CloseBracket,       // ]
        Arrow,              // ->
        Word,
        Keyword,
        NewLine,            // \r(\n)
        Comment,            ////
    }

    public class ScriptTokenizer
    {
        TextReader reader;

        public HashSet<string> keywords = new HashSet<string>
        {
            "EVENT",
            "PROMPT",
            "CHOICE",
            "EFFECT",
        };
        
        int line = 1;
        
        ScriptToken currentToken;
        char        currentChar;
        int         number;
        string      word = "";
   
        public ScriptToken  CurrentToken { get { return currentToken; } set { currentToken = value; } }
        public int          Number       { get { return number; } }
        public string       Word         { get { return word; } }
        public char         CurrentChar  { get { return currentChar; } }
        public int          CurrentLine  { get { return line; } }


        public ScriptTokenizer(TextReader textReader)
        {
            reader = textReader;
            NextChar();
            NextToken();
        }

        char NextChar()
        {
            int chr = reader.Read();
            currentChar = chr < 0 ? '\0' : (char)chr;
            return currentChar;
        }

        char Peek()
        {
            int chr = reader.Peek();
            return chr < 0 ? '\0' : (char)chr;
        }

        
        public string ReadLines()
        {
            NextToken();
            var sb = new StringBuilder();
            while(currentToken != ScriptToken.Keyword && currentToken != ScriptToken.CloseSquiggle && currentToken != ScriptToken.Arrow)
            {
                if(currentToken == ScriptToken.Word)
                    sb.Append($"{Word} ");                              
                else if(currentToken == ScriptToken.NewLine)
                    sb.AppendLine();
                NextToken();
            }
            //Console.WriteLine(sb.ToString());
            return sb.ToString().Trim();
        }

        public string ReadBrackets()
        {
            //NextChar();
            var sb = new StringBuilder();
            while(currentChar != ']')
            {
                sb.Append(currentChar);
                NextChar();
            }
            NextToken(); NextToken();
            return sb.ToString();
        }   

        public void NextToken()
        {
            while(char.IsWhiteSpace(currentChar) && (currentChar != '\r')) { NextChar(); }

            switch(currentChar)
            {            
                case '\0':
                    currentToken = ScriptToken.EOF;
                    return;

                case '\r':
                    NextChar();
                    NextChar();
                    line++;
                    currentToken = ScriptToken.NewLine;
                    word = "";
                    return;
                
                case ':':
                    if(NextChar() == ':')
                    {
                        NextChar();
                        currentToken = ScriptToken.DoubleColon;
                    }                       
                    else currentToken = ScriptToken.Colon;
                    return;
                
                case '{':
                    NextChar();
                    currentToken = ScriptToken.OpenSquiggle;
                    return;
                
                case '}':
                    NextChar();
                    currentToken = ScriptToken.CloseSquiggle;
                    return;

                case '[':
                    NextChar();
                    currentToken = ScriptToken.OpenBracket;
                    return;

                case ']':
                    NextChar();
                    currentToken = ScriptToken.CloseBracket;
                    return;

                case '-':
                    if(NextChar() == '>')
                    {
                        NextChar();
                        currentToken = ScriptToken.Arrow;
                    }                       
                    return;
            }

            var sb = new StringBuilder();
            if(char.IsLetter(currentChar) || IsSentencePunctuation(currentChar))
            {
                while(char.IsLetter(currentChar) || char.IsNumber(currentChar) || IsSentencePunctuation(currentChar))
                {
                    sb.Append(currentChar);
                    NextChar();
                }

                word = sb.ToString().Trim();
                if(keywords.Contains(word))
                {
                    currentToken = ScriptToken.Keyword;
                    return;
                }                           
                else currentToken = ScriptToken.Word;
                return;
            }

            if(currentChar == '/')
            {
                while(currentChar != '\r')
                    NextChar();
                NextToken();
                return;
            }
                
           
            
            Console.WriteLine($"Unexpected character {currentChar} or token {currentToken}");
            throw new Exception($"Unexpected character {currentChar} or token {currentToken}");
        }
    
        bool IsSentencePunctuation(char chr) => chr switch
        {
            '.'  => true,
            ';'  => true,
            ','  => true,
            '\'' => true,
            '"'  => true,
            '?'  => true,
            '!'  => true,
            '—'  => true,
            '-'  => true,
            '_'  => true,
            '&'  => true,
            '('  => true,
            ')'  => true,
            '['  => true,
            ']'  => true,
            _    => false,
        };

    }
}
