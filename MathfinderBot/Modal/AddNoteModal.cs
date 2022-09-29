using Discord;
using Discord.Interactions;

namespace MathfinderBot.Modal
{
    public class AddNoteModal : IModal
    {
        public string Title => "New-Note()";

        [ModalTextInput("subject",maxLength:100)]
        public string Subject { get; set; }

        [ModalTextInput("note", style: TextInputStyle.Paragraph)]
        public string Note { get; set; }
    }
}
