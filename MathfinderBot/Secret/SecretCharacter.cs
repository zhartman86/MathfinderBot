using Gellybeans.Expressions;
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
        
        Dictionary<string, string> Expressions;

        public event EventHandler<string>? ValueChanged;
        void OnValueChanged(string propertyChanged) { ValueChanged?.Invoke(this, propertyChanged); }

        public async Task<List<int>> GetCurrentSecrets()
        {
            return await Task.Run(() =>
            {
                var list = new List<int>();
                for(int i = 0; i < Current.Count; i++)
                    list.Add(Current[i].Index);
                return list;
            });
        }

        public string this[string propertyName]
        {
            get { return Properties.TryGetValue(propertyName, out string? outVal) ? outVal : string.Empty; }
            set
            {
                Properties[propertyName] = value;
                OnValueChanged(propertyName);
            }
        }

        public void SetFlag(string propertyName, EventFlag flag)
        {
            this[propertyName] = Properties.ContainsKey(propertyName) && Enum.TryParse(Properties[propertyName], out EventFlag outVal) ? 
                (outVal |= flag).ToString() : 
                ((long)flag).ToString();
        }

        public void AddInt(string propertyName, int value)
        {
            this[propertyName] = Properties.ContainsKey(propertyName) && int.TryParse(Properties[propertyName], out int outVal) ? 
                (outVal + value).ToString() : 
                value.ToString();
        }

        public bool MeetsRequirements(CharacterRequirement requirement) => requirement.EvalType switch
        {
            EvalType.Flag                => Enum.TryParse(this[requirement.Property], out EvalType outVal) && outVal.HasFlag(Enum.Parse<EvalType>(requirement.Value)) ? true : false,
            EvalType.GreaterThanOrEquals => int.TryParse(this[requirement.Property], out int outVal) && outVal >= int.Parse(requirement.Value) ? true : false,
            EvalType.LessThanOrEquals    => int.TryParse(this[requirement.Property], out int outVal) && outVal <= int.Parse(requirement.Value) ? true : false,
            EvalType.Exact               => int.TryParse(this[requirement.Property], out int outVal) && outVal == int.Parse(requirement.Value) ? true : false,
            _ => false,
        };
            

        //IContext
        public int Assign(string identifier, string assignment, TokenType assignType, StringBuilder sb)
        {
            Properties["AttemptedAssigns"] = long.TryParse(Properties["AttemptedAssigns"], out long outVal) ? outVal++.ToString() : "1";
            return Random.Shared.Next(-100,100);
        }

        public int Bonus(string identifier, string bonusName, int type, int value, TokenType assignType, StringBuilder sb) => 0;

        public int Resolve(string varName, StringBuilder sb)
        {
            if (Expressions.ContainsKey(varName))
                return Parser.Parse(Expressions[varName]).Eval(this, sb);
            return 0;
        }
    
       
    }
}
