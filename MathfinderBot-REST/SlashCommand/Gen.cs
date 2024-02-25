using Discord.Interactions;

namespace MathfinderBot
{
    public class Gen : InteractionModuleBase
    {
        public InteractionService Service { get; set; }


        [SlashCommand("gen", "Generate lists of things")]
        public async Task GenCommand()
        {

        }
    }
}
