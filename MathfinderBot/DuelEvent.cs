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
        public Guid Id       { get; set; }
        public DateTime Date { get; init; } = DateTime.Now;

        public Func<int> WinCondition = null!;
        
        public int Win                { get; set; }
        public string Expression      { get; init; }
        public DuelistInfo[] Duelists { get; init; }        

        public DuelEvent(ulong challenger, ulong challenged, string expr)
        {
            Expression = expr;
            Duelists = new DuelistInfo[2]
            {
                new DuelistInfo { Id = challenger },
                new DuelistInfo { Id = challenged },
            };
        }

        public bool Contains(ulong id) => Duelists.Any(x => x.Id == id);

        public async Task Eval()
        {
            var sb = new StringBuilder();         
            for(int i = 0; i < Duelists.Length; i++)
            {
                Duelists[i].Total = await Utility.SecEvaluate(Expression, sb, null!);
                Duelists[i].Events = sb.ToString();
                sb.Clear();
            }

            if(WinCondition == null)
                Win = Duelists[0].Total > Duelists[1].Total ? 0 : Duelists[0].Total < Duelists[1].Total ? 1 : -1; // -1 is a tie
            else 
                Win = WinCondition();           
        }
    }
}
