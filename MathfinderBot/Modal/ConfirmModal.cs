using Discord.Interactions;

namespace MathfinderBot
{
    public class ConfirmModal : IModal
    {
        
        public string Title => ">Confirm<";

        [InputLabel("Type 'CONFIRM' and submit")]
        [ModalTextInput("confirm_name", minLength: 1, maxLength: 30)]
        public string Confirm { get; set; }       
    }
}
