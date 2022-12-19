using Gellybeans.Expressions;
using Microsoft.VisualBasic;
using System.Text;

namespace MathfinderBot
{
       
    public class SecretCharacter : IContext
    {
        public Guid Id { get; set; }
        public ulong Owner { get; set; }
        
        public List<Secret> Secrets { get; set; } = new List<Secret>() { DataMap.Secrets[0].Copy() };
        public List<Secret> Current { get; set; } = new List<Secret>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();        

        public event EventHandler<string>? ValueChanged;
        void OnValueChanged(string propertyChanged) { ValueChanged?.Invoke(this, propertyChanged); }

        public string this[string propertyName]
        {
            get { return Properties.TryGetValue(propertyName, out string? outVal) ? outVal : string.Empty; }
            set
            {
                Properties[propertyName] = value;
                OnValueChanged(propertyName);
            }
        }
        

        //IContext
        public int Assign(string varName, string assignment, TokenType assignType, StringBuilder sb)
        {
            varName = varName.Replace(' ', '_').ToUpper();

            switch(assignType)
            {
                case TokenType.AssignExpr:
                    this[varName] = assignment;
                    return 1;
                case TokenType.Assign:
                    this[varName] = assignment;
                    return 1;
                case TokenType.AssignAdd: //+=
                    this[varName] = (int.Parse(this[varName]) + int.Parse(assignment)).ToString();
                    return 1;
                case TokenType.AssignSub: //-=
                    this[varName] = (int.Parse(this[varName]) - int.Parse(assignment)).ToString();
                    return 1;
                case TokenType.Flag: //::
                    var val = int.TryParse(assignment, out int outVal) && outVal < 32 && outVal > -32 ? outVal : 0;
                    if(Math.Sign(val) > 0)
                        this[varName] = int.TryParse(this[varName], out int outInt) ? (outInt |= (1 << val)).ToString() : (1 << val).ToString();
                    else if(Math.Sign(val) < 0)
                        this[varName] = int.TryParse(this[varName], out int outInt) ? (outInt &= ~(1 << Math.Abs(val))).ToString() : (1 << Math.Abs(val)).ToString();
                    return 1;
            }           
            return 0;
        }

        public int Bonus(string identifier, string bonusName, int type, int value, TokenType assignType, StringBuilder sb) => 0;

        public int Resolve(string varName, StringBuilder sb)
        {
            if(Properties.ContainsKey(varName))
            {
                if(int.TryParse(Properties[varName], out int outVal))
                    return outVal;
                else
                    sb.AppendLine(Properties[varName]);
            }
            return 0;
        }
    
       
    }
}
