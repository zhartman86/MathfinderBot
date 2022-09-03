using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using System.Text;

namespace MathfinderBot
{
    public class Init
    {
        public class InitObj
        {            
            public StatBlock?   Stats   { get; set; } = null;
            public ulong        Owner   { get; set; }
            public string       Name    { get; set; } = "";
            public int          Bonus   { get; set; } = 0;
            public int          Rolled  { get; set; } = 0;

            public InitObj() { }
            public InitObj(StatBlock stats, int value)
            {
                Stats   = stats;
                Owner   = stats.Owner;
                Name    = stats.CharacterName;
            }
        }
        
        public List<InitObj>    InitObjs    { get; private set; } = new List<InitObj>();
        public int              Current     { get; private set; } = 0;
        public string           Expr        { get; set; } = "1d20";
        public ulong            LastMessage { get; set; } = 0;

        public InitObj this[int index]
        {
            get { return InitObjs[index]; }
        }


        public void Add(InitObj iObj)
        {
            InitObjs.Add(iObj);
        }
        
        public void Remove(InitObj iObj)
        {
            if(InitObjs[Current] == iObj)
                Next();

            var temp = InitObjs[Current];

            InitObjs.Remove(iObj);          
            Current = InitObjs.IndexOf(temp);
        }

        public void Sort()
        {
            if(InitObjs.Count > 1) 
                InitObjs.Sort((x, y) => y.Rolled.CompareTo(x.Rolled));
        }

        public void Roll()
        {
            foreach(var init in InitObjs)
                init.Rolled = Parser.Parse($"{Expr}+{init.Bonus}").Eval(null, null);
        }
        
        public InitObj Next()
        {
            Current++;
            if(Current >= InitObjs.Count)
                Current = 0;

            return InitObjs[Current];
        }
    
        public InitObj Previous()
        {
            Current--;
            return InitObjs[Current];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for(int i = 0; i < InitObjs.Count; i++)
            {
                if(i == Current) sb.AppendLine($" >>>|{this[i].Name,-20} |{this[i].Rolled,-3}");
                else sb.AppendLine($" ---|{this[i].Name,-20} |{this[i].Rolled,-3}");
            }
                
            return sb.ToString();
        }
    }
}
