using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public class Init
    {
        public class InitObj
        {            
            public StatBlock?   Stats   { get; set; } = null;
            public ulong        Owner   { get; set; }
            public string       Name    { get; set; } = "";
            public int          Value   { get; set; } = 0;

            public InitObj(StatBlock stats, int value)
            {
                Stats   = stats;
                Owner   = stats.Owner;
                Name    = stats.CharacterName;
            }

        }
        
        public List<InitObj>    InitObjs    { get; private set; } = new List<InitObj>();
        public int              Current     { get; private set; }

        public void Add(InitObj iObj) 
        {
            var temp = InitObjs[Current];
            
            InitObjs.Add(iObj);
            InitObjs.Sort((x, y) => y.Value.CompareTo(x.Value));

            Current = InitObjs.IndexOf(temp);
        }
    
        public void Remove(InitObj iObj)
        {
            if(InitObjs[Current] == iObj)
                Next();

            var temp = InitObjs[Current];

            InitObjs.Remove(iObj);
            InitObjs.Sort((x, y) => y.Value.CompareTo(x.Value));
            
            Current = InitObjs.IndexOf(temp);
        }

        public InitObj Next()
        {
            if(Current == InitObjs.Count)
                Current = 0;
            else
                Current++;

            return InitObjs[Current];
        }
    
        public InitObj Previous()
        {
            Current--;
            return InitObjs[Current];
        }
    }
}
