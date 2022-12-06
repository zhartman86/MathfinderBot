using Discord.Interactions;

namespace MathfinderBot
{
    public class ExpressionModal : IModal 
    {
        public string Title => "Set-Expression";
        
        public string Expression { get; set; }
    
    }
}
