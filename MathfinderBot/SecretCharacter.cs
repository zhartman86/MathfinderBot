using Gellybeans.Expressions;
using System.Text;

namespace MathfinderBot
{       
    public class SecretCharacter : IContext
    {       
        public Guid Id { get; set; }

        public ulong Owner { get; set; }
        public List<Secret> Secrets { get; set; } = new List<Secret>() { DataMap.Secrets[0] };
        public Secret Current { get; set; }

        Dictionary<string, string> Expressions;
        
        public int Assign(string identifier, string assignment, TokenType assignType, StringBuilder sb) => 0;
        public int Bonus(string identifier, string bonusName, int type, int value, TokenType assignType, StringBuilder sb) => 0;

        public int Resolve(string varName, StringBuilder sb)
        {
            if(Expressions.ContainsKey(varName))
                return Parser.Parse(Expressions[varName]).Eval(this, sb);
            return 0;
        }
    }
}
