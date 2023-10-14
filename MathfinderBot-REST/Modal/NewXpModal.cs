using Discord.Interactions;

namespace MathfinderBot
{
    public class NewXpModal : IModal
    {
        public string Title => "XP(New)";

        [InputLabel("Name")]
        [ModalTextInput("xp_name")]
        public string Name { get; set; }

        [InputLabel("Amount")]
        [ModalTextInput("xp_amount")]
        public int Experience { get; set; }


    }
}
