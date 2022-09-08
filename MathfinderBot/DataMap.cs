using Gellybeans.Pathfinder;
using Newtonsoft.Json;

namespace MathfinderBot
{
    public static class DataMap
    {
        public static List<Attack> Attacks { get; set; } = new List<Attack>();


        static DataMap()
        {
            Console.Write("Getting weapons...");
            var attacks = File.ReadAllText(@"D:\PFData\Weapons.json");
            Attacks = JsonConvert.DeserializeObject<List<Attack>>(attacks);
            Console.WriteLine($"Attacks => {Attacks.Count}");
        }

    }
}
