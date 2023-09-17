using Discord.Interactions;

namespace MathfinderBot
{
    public class InitModal : IModal
    {
        public string Title => "New-Init()";

        [ModalTextInput("init_list", Discord.TextInputStyle.Paragraph, placeholder:"One per line. NAME:INITBONUS")]
        public string InitList { get; set; }
    }
}
