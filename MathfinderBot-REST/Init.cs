using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using System.Text;

namespace MathfinderBot
{
    public class Init
    {
        public class InitObj
        {            
            public ulong        Owner   { get; set; }
            public string       Name    { get; set; } = "";
            public int          Bonus   { get; set; } = 0;
            public int          Rolled  { get; set; } = 0;

            public InitObj() { }
            public InitObj(ulong owner, int value, string bonusStat = "INIT_BONUS")
            {           
                Owner   = owner;
                if(Characters.Active.ContainsKey(owner))
                {
                    Name = Characters.Active[owner].CharacterName;
                    Bonus = Characters.Active[owner].Stats.ContainsKey(bonusStat) ? Characters.Active[owner][bonusStat] : 0;
                }                       
                Rolled  = value;
            }
        }
        
        public List<InitObj>    InitObjs    { get; private set; } = new List<InitObj>();
        public int              Current     { get; private set; } = 0;
        public string           Expr        { get; set; } = "1d20";
        public uint             Round       { get; set; } = 1;

        public bool             isPrivate   { get; set; } = false;
        public ulong            LastMessage { get; set; } = 0;

        public InitObj this[int index]
        {
            get { return InitObjs[index]; }
        }

        public void Add(InitObj iObj) => InitObjs.Add(iObj);        

        public InitObj Remove(int index)
        {
            if(index < 0 || index >= InitObjs.Count) return null;
            
            if(Current == index)
                Next();

            var temp    = this[Current];
            var removed = this[index];
            
            InitObjs.RemoveAt(index);          
            Current = InitObjs.IndexOf(temp);
            return removed;
        }

        public bool Move(int fromIndex)
        {
            if(fromIndex >= 0 && fromIndex < InitObjs.Count)
            {
                var remove = Remove(fromIndex);
                if(remove != null)
                {
                    InitObjs.Insert(Current, remove);
                    return true;
                }
            }
            return false;
        }
            
        
        public void Sort()
        {
            if(InitObjs.Count > 1) 
                InitObjs.Sort((x, y) => y.Rolled.CompareTo(x.Rolled));
        }

        public void Roll()
        {
            foreach(var init in InitObjs)
                init.Rolled = Parser.Parse($"{Expr}+{init.Bonus}").Eval();
        }
        
        public InitObj Next()
        {
            Current++;
            if(Current >= InitObjs.Count)
            {
                Current = 0;
                Round++;
            }
            return InitObjs[Current];
        }
    
        public InitObj Previous()
        {
            Current--;
            return InitObjs[Current];
        }

        public string ToString(int before, int after)
        {
            var sb = new StringBuilder();
            for(int i = Math.Clamp(Current - before, 0, InitObjs.Count); i < Math.Clamp(Current + after + 1, 0,InitObjs.Count); i++)
            {
                if(i == 0 && i == Current)  sb.AppendLine($"ROUND {Round}");
                if(i == Current)            sb.AppendLine($"{i,-3}| >>>|{this[i].Name,-15} +{this[i].Bonus,-6} -> {this[i].Rolled,-4}");
                else                        sb.AppendLine($"{i,-3}| ---|{this[i].Name,-15} +{this[i].Bonus,-6} -> {this[i].Rolled,-4}");
            }      
            return sb.ToString();
        }

        public override string ToString() => ToString(100, 100); 
    }
}
