using Gellybeans.Pathfinder;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text;
using System.Xml.Linq;

namespace MathfinderBot
{
    public class DuelistInfo
    {
        public ulong  Id     { get; init; }
        public int    Total  { get; set; }
        public string Events { get; set; }       
    }

    public class DuelEvent
    {
        public Guid Id { get; set; }
        public DateTime Date { get; init; } = DateTime.Now;

        public int Winner { get; set; } = -2;
        public string Expression { get; set; }
        public DuelistInfo[] Duelists { get; init; }

        public Func<DuelEvent, int> WinCondition;


        public DuelEvent(ulong challenger, ulong challenged, string expr)
        {
            Expression = expr;
            Duelists = new DuelistInfo[2]
            {
                new DuelistInfo { Id = challenger },
                new DuelistInfo { Id = challenged },
            };
            WinCondition = (duel) => { return Duelists[0].Total > Duelists[1].Total ? 0 : Duelists[0].Total < Duelists[1].Total ? 1 : -1; };
        }

        public bool Contains(ulong id) => 
            Duelists.Any(x => x.Id == id);
                  
        //public async Task Eval()
        //{
        //    var sb = new StringBuilder();

        //    for(int i = 0; i < Duelists.Length; i++)
        //    {             
        //        var node = Gellybeans.Expressions.Parser.Parse(Expression);
        //        Duelists[i].Total += node.Eval(null!, sb);
                
        //        if(Characters.SecretCharacters.ContainsKey(Duelists[i].Id))
        //        {
        //            var sChar = Characters.SecretCharacters[Duelists[i].Id];
        //            //var intList = sChar.Current;

        //            for(int j = 0; j < sChar.Current.Count; j++)
        //                await DataMap.Secrets[sChar.Secrets[j].Index].Apply(this, sb,  i);
        //        }
        //        Duelists[i].Events = sb.ToString();
        //    }

        //    Winner = WinCondition(this);

        //}

        public string ToString(ulong combatant)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"-{Date.ToString("u")}-");
            sb.AppendLine($"Challenger ⚔️ {(Duelists[0].Id == combatant ? "You" : "Them")}");         
            sb.AppendLine($"Total: {Duelists[0].Total,-20}");
            sb.AppendLine();
            sb.AppendLine($"{Duelists[0].Events,-20}");

            sb.AppendLine();

            sb.AppendLine($"Challenged 🛡️ {(Duelists[1].Id == combatant ? "You" : "Them")}");
            sb.AppendLine($"Total: {Duelists[1].Total}");
            sb.AppendLine();
            sb.AppendLine($"{Duelists[1].Events}");

            return sb.ToString();
        }

    }
}
