using Discord.Interactions;

namespace MathfinderBot
{
    public class ChangeNameModal : IModal
    {
        public string Title => "Change-Name()";

        [ModalTextInput("change_name")]
        public string NewName { get; set; }
    }
}
