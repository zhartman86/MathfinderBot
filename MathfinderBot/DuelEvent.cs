namespace MathfinderBot
{
    public class DuelInfo
    {
        public ulong  Duelist { get; init; }       
        public string Events  { get; init; }
        public int    Total   { get; init; }
    }
    
    public class DuelEvent
    {
        public Guid Id { get; set; }
        public DateTime Date { get; init; }
        
        public string Expression { get; init; }
        public DuelInfo Challenger { get; init; }
        public DuelInfo Challenged { get; init; }

        public bool Contains(ulong id) =>
            id == Challenger.Duelist || id == Challenged.Duelist;


        public override string ToString()
        {
            return @$"{Challenger.Events,50}|{Challenged.Events,-50}

{Challenger.Total,50}|{Challenged.Total,-50}";
        }
    }
}
