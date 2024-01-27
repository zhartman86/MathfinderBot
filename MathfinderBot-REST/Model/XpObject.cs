namespace MathfinderBot
{
    public class XpObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public Xp.XpTrack Track { get; set; } = Xp.XpTrack.Medium;
        public int Experience { get; set; } = 0;
        public string Details { get; set; } = "";
    }
}
