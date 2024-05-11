using Gellybeans.Expressions;

namespace MathfinderBot
{
    public class ChannelScope : IContext
    {
        ulong channelID;
        public Dictionary<string, dynamic> Vars { get; }

        public IContext Parent { get { return null!; } }

        public dynamic this[string identifier]
        {
            get
            {
                if(TryGetVar(identifier, out var value))
                    return value;
                return 0;
            }
            set { return; }
        }


        public ChannelScope(Dictionary<string, dynamic> vars, ulong channelID)
        {
            Vars = vars;
            this.channelID = channelID;
        }
            

        public bool TryGetVar(string varName, out dynamic value)
        {
            if(Vars.TryGetValue(varName, out value))
                return true;

            return false;
        }

        public bool RemoveVar(string varName)
        {
            if(Vars.Remove(varName))
                return true;
            return false;
        }

        public bool TryElevateVar(string varName, dynamic value)
        {
            value = null!;
            return false;
        }


    }
}
